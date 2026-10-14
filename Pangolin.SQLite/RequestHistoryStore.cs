using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Pangolin.SQLite.Models;

namespace Pangolin.SQLite;

public sealed class RequestHistoryStore
{
    public const int MaximumHistoryEntries = 1000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private Task? _initializationTask;

    public string DatabasePath => _databasePath;

    public RequestHistoryStore(string? databasePath = null)
    {
        _databasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PangolinSharp", "Postman", "request-history.db");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_initializeLock)
            _initializationTask ??= Task.Run(InitializeDatabase);
        return _initializationTask.WaitAsync(cancellationToken);
    }

    public async Task<long> SaveAsync(
        DateTimeOffset createdAtUtc,
        string method,
        string url,
        RequestHistorySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (string.IsNullOrWhiteSpace(method)) throw new ArgumentException("HTTP method is required.", nameof(method));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Request URL is required.", nameof(url));
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var insertHistory = connection.CreateCommand();
            insertHistory.Transaction = transaction;
            insertHistory.CommandText = """
                INSERT INTO request_history (created_at_utc, method, url)
                VALUES ($created_at_utc, $method, $url);
                SELECT last_insert_rowid();
                """;
            insertHistory.Parameters.AddWithValue("$created_at_utc", ToStorageTime(createdAtUtc));
            insertHistory.Parameters.AddWithValue("$method", method);
            insertHistory.Parameters.AddWithValue("$url", url);
            var id = Convert.ToInt64(insertHistory.ExecuteScalar(), CultureInfo.InvariantCulture);

            using var insertDetail = connection.CreateCommand();
            insertDetail.Transaction = transaction;
            insertDetail.CommandText = """
                INSERT INTO request_history_detail (
                    history_id, snapshot_version, headers_json, query_params_json,
                    body_mode, body_content_type, body_text, form_fields_json,
                    auth_type, auth_config_json, settings_json, secrets_redacted)
                VALUES (
                    $history_id, $version, $headers, $query_params,
                    $body_mode, $body_content_type, $body_text, $form_fields,
                    $auth_type, $auth, $settings, $secrets_redacted);
                """;
            insertDetail.Parameters.AddWithValue("$history_id", id);
            insertDetail.Parameters.AddWithValue("$version", RequestHistorySnapshot.CurrentVersion);
            insertDetail.Parameters.AddWithValue("$headers", JsonSerializer.Serialize(snapshot.Headers, JsonOptions));
            insertDetail.Parameters.AddWithValue("$query_params", JsonSerializer.Serialize(snapshot.QueryParameters, JsonOptions));
            insertDetail.Parameters.AddWithValue("$body_mode", snapshot.BodyMode);
            insertDetail.Parameters.AddWithValue("$body_content_type", snapshot.BodyContentType);
            insertDetail.Parameters.AddWithValue("$body_text", (object?)snapshot.BodyText ?? DBNull.Value);
            insertDetail.Parameters.AddWithValue("$form_fields", JsonSerializer.Serialize(snapshot.FormFields, JsonOptions));
            insertDetail.Parameters.AddWithValue("$auth_type", snapshot.AuthType);
            insertDetail.Parameters.AddWithValue("$auth", JsonSerializer.Serialize(snapshot.Auth, JsonOptions));
            insertDetail.Parameters.AddWithValue("$settings", JsonSerializer.Serialize(snapshot.Settings, JsonOptions));
            insertDetail.Parameters.AddWithValue("$secrets_redacted", snapshot.SecretsRedacted ? 1 : 0);
            insertDetail.ExecuteNonQuery();
            using var prune = connection.CreateCommand();
            prune.Transaction = transaction;
            prune.CommandText = """
                DELETE FROM request_history
                WHERE id NOT IN (
                    SELECT id FROM request_history ORDER BY created_at_utc DESC, id DESC LIMIT $limit);
                """;
            prune.Parameters.AddWithValue("$limit", MaximumHistoryEntries);
            prune.ExecuteNonQuery();
            transaction.Commit();
            return id;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RequestHistorySummary>> GetRecentAsync(
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000.");
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run<IReadOnlyList<RequestHistorySummary>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, created_at_utc, method, url
                FROM request_history
                ORDER BY created_at_utc DESC, id DESC
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$limit", limit);
            using var reader = command.ExecuteReader();
            var items = new List<RequestHistorySummary>();
            while (reader.Read())
            {
                items.Add(new RequestHistorySummary
                {
                    Id = reader.GetInt64(0),
                    CreatedAtUtc = ParseStorageTime(reader.GetString(1)),
                    Method = reader.GetString(2),
                    Url = reader.GetString(3)
                });
            }
            return items;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RequestHistorySnapshot?> GetDetailAsync(long historyId, CancellationToken cancellationToken = default)
    {
        if (historyId < 1) return null;
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT snapshot_version, headers_json, query_params_json, body_mode,
                       body_content_type, body_text, form_fields_json, auth_type,
                       auth_config_json, settings_json, secrets_redacted
                FROM request_history_detail
                WHERE history_id = $history_id;
                """;
            command.Parameters.AddWithValue("$history_id", historyId);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            var version = reader.GetInt32(0);
            if (version > RequestHistorySnapshot.CurrentVersion)
                throw new NotSupportedException($"History snapshot version {version} is newer than this application supports.");

            return new RequestHistorySnapshot
            {
                Version = version,
                Headers = Deserialize<List<RequestHistoryHeader>>(reader.GetString(1)),
                QueryParameters = Deserialize<List<RequestHistoryParameter>>(reader.GetString(2)),
                BodyMode = reader.GetString(3),
                BodyContentType = reader.GetString(4),
                BodyText = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                FormFields = Deserialize<List<RequestHistoryFormField>>(reader.GetString(6)),
                AuthType = reader.GetString(7),
                Auth = Deserialize<RequestHistoryAuthConfiguration>(reader.GetString(8)),
                Settings = Deserialize<RequestHistorySettings>(reader.GetString(9)),
                SecretsRedacted = reader.GetInt32(10) != 0
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(long historyId, CancellationToken cancellationToken = default)
    {
        if (historyId < 1) return false;
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM request_history WHERE id = $id;";
            command.Parameters.AddWithValue("$id", historyId);
            return command.ExecuteNonQuery() != 0;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM request_history;";
            return command.ExecuteNonQuery();
        }, cancellationToken).ConfigureAwait(false);
    }

    private void InitializeDatabase()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        using var connection = OpenConnection();
        using (var journal = connection.CreateCommand())
        {
            journal.CommandText = "PRAGMA journal_mode = WAL;";
            journal.ExecuteScalar();
        }

        using var transaction = connection.BeginTransaction();
        using var versionCommand = connection.CreateCommand();
        versionCommand.Transaction = transaction;
        versionCommand.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(versionCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (version > 1) throw new NotSupportedException($"Database schema version {version} is newer than this application supports.");
        if (version == 0)
        {
            using var schema = connection.CreateCommand();
            schema.Transaction = transaction;
            schema.CommandText = """
                CREATE TABLE IF NOT EXISTS request_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    created_at_utc TEXT NOT NULL,
                    method TEXT NOT NULL,
                    url TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_request_history_created
                    ON request_history(created_at_utc DESC);
                CREATE INDEX IF NOT EXISTS idx_request_history_method_created
                    ON request_history(method, created_at_utc DESC);

                CREATE TABLE IF NOT EXISTS request_history_detail (
                    history_id INTEGER PRIMARY KEY,
                    snapshot_version INTEGER NOT NULL,
                    headers_json TEXT NOT NULL,
                    query_params_json TEXT NOT NULL,
                    body_mode TEXT NOT NULL,
                    body_content_type TEXT NOT NULL,
                    body_text TEXT,
                    form_fields_json TEXT NOT NULL,
                    auth_type TEXT NOT NULL,
                    auth_config_json TEXT NOT NULL,
                    settings_json TEXT NOT NULL,
                    secrets_redacted INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (history_id) REFERENCES request_history(id) ON DELETE CASCADE
                );
                PRAGMA user_version = 1;
                """;
            schema.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            ForeignKeys = true,
            DefaultTimeout = 5
        }.ToString());
        connection.Open();
        return connection;
    }

    private static T Deserialize<T>(string json) where T : new()
        => JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();

    private static string ToStorageTime(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseStorageTime(string value)
        => DateTimeOffset.ParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
}
