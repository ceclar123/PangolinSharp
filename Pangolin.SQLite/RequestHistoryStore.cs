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
        _databasePath = databasePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PangolinSharp", "Postman", "request-history.db");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_initializeLock)
            _initializationTask ??= Task.Run(InitializeDatabase);
        return _initializationTask.WaitAsync(cancellationToken);
    }

    public async Task<long> SaveAsync(DateTimeOffset createdAtUtc, string method, string url, RequestHistorySnapshot snapshot, CancellationToken cancellationToken = default)
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
                VALUES ($created_at_utc, $method, $url)
                ON CONFLICT(method, url) DO UPDATE SET created_at_utc = excluded.created_at_utc;
                """;
            insertHistory.Parameters.AddWithValue("$created_at_utc", ToStorageTime(createdAtUtc));
            insertHistory.Parameters.AddWithValue("$method", method);
            insertHistory.Parameters.AddWithValue("$url", url);
            insertHistory.ExecuteNonQuery();
            using var selectHistoryId = connection.CreateCommand();
            selectHistoryId.Transaction = transaction;
            selectHistoryId.CommandText = "SELECT id FROM request_history WHERE method = $method AND url = $url;";
            selectHistoryId.Parameters.AddWithValue("$method", method);
            selectHistoryId.Parameters.AddWithValue("$url", url);
            var id = Convert.ToInt64(selectHistoryId.ExecuteScalar(), CultureInfo.InvariantCulture);

            using var replaceDetail = connection.CreateCommand();
            replaceDetail.Transaction = transaction;
            replaceDetail.CommandText = "DELETE FROM request_history_detail WHERE history_id = $history_id;";
            replaceDetail.Parameters.AddWithValue("$history_id", id);
            replaceDetail.ExecuteNonQuery();

            using var insertDetail = connection.CreateCommand();
            insertDetail.Transaction = transaction;
            insertDetail.CommandText = """
                INSERT INTO request_history_detail (
                    history_id, created_at_utc, snapshot_version, headers_json, query_params_json,
                    body_mode, body_content_type, body_text, form_fields_json,
                    auth_type, auth_config_json, settings_json, secrets_redacted)
                VALUES (
                    $history_id, $created_at_utc, $snapshot_version, $headers, $query_params,
                    $body_mode, $body_content_type, $body_text, $form_fields,
                    $auth_type, $auth, $settings, $secrets_redacted);
                """;
            insertDetail.Parameters.AddWithValue("$history_id", id);
            insertDetail.Parameters.AddWithValue("$created_at_utc", ToStorageTime(createdAtUtc));
            insertDetail.Parameters.AddWithValue("$snapshot_version", RequestHistorySnapshot.CurrentVersion);
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
            using var defaultMembership = connection.CreateCommand();
            defaultMembership.Transaction = transaction;
            defaultMembership.CommandText = """
                INSERT INTO collection_history (collection_id, history_id, created_at_utc)
                SELECT 0, $history_id, $created_at_utc
                WHERE NOT EXISTS (
                    SELECT 1 FROM collection_history WHERE history_id = $history_id);
                """;
            defaultMembership.Parameters.AddWithValue("$history_id", id);
            defaultMembership.Parameters.AddWithValue("$created_at_utc", ToStorageTime(createdAtUtc));
            defaultMembership.ExecuteNonQuery();
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

    public async Task<IReadOnlyList<RequestHistorySummary>> GetRecentAsync(int limit = 200, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000.");
        }

        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run<IReadOnlyList<RequestHistorySummary>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT h.id, d.created_at_utc, h.method, h.url
                FROM request_history AS h
                JOIN request_history_detail AS d ON d.history_id = h.id
                ORDER BY d.created_at_utc DESC, d.id DESC
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
            var snapshotVersion = reader.GetInt32(0);
            if (snapshotVersion > RequestHistorySnapshot.CurrentVersion)
                throw new NotSupportedException($"Request snapshot version {snapshotVersion} is newer than this application supports.");
            return new RequestHistorySnapshot
            {
                Version = snapshotVersion,
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

    public async Task<bool> DeleteDetailAsync(long historyId, CancellationToken cancellationToken = default)
    {
        if (historyId < 1) return false;
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM request_history_detail WHERE history_id = $history_id;";
            command.Parameters.AddWithValue("$history_id", historyId);
            return command.ExecuteNonQuery() != 0;
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

    public async Task<IReadOnlyList<RequestCollection>> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run<IReadOnlyList<RequestCollection>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT c.id, c.name, c.parent_id, COUNT(ch.history_id)
                FROM request_collection AS c
                LEFT JOIN collection_history AS ch ON ch.collection_id = c.id
                GROUP BY c.id, c.name, c.parent_id
                ORDER BY CASE WHEN c.id = 0 THEN 0 ELSE 1 END,
                         COALESCE(c.parent_id, 0), c.name COLLATE NOCASE, c.id;
                """;
            using var reader = command.ExecuteReader();
            var items = new List<RequestCollection>();
            while (reader.Read())
            {
                items.Add(new RequestCollection
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    ParentId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                    RequestCount = reader.GetInt32(3)
                });
            }
            return items;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<long> CreateCollectionAsync(string name, long? parentId = null, CancellationToken cancellationToken = default)
    {
        name = ValidateCollectionName(name);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO request_collection (name, parent_id, created_at_utc)
                VALUES ($name, $parent_id, $created_at_utc);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$parent_id", (object?)parentId ?? DBNull.Value);
            command.Parameters.AddWithValue("$created_at_utc", ToStorageTime(DateTimeOffset.UtcNow));
            try { return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture); }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("A collection with this name already exists at this level, or the parent collection no longer exists.", ex);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task RenameCollectionAsync(long collectionId, string name, CancellationToken cancellationToken = default)
    {
        if (collectionId < 1) throw new ArgumentOutOfRangeException(nameof(collectionId));
        name = ValidateCollectionName(name);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE request_collection SET name = $name WHERE id = $id;";
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$id", collectionId);
            try
            {
                if (command.ExecuteNonQuery() == 0) throw new KeyNotFoundException("The collection no longer exists.");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("A collection with this name already exists at this level.", ex);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveCollectionAsync(long collectionId, long? newParentId, CancellationToken cancellationToken = default)
    {
        if (collectionId < 1) throw new ArgumentOutOfRangeException(nameof(collectionId));
        if (newParentId == collectionId) throw new InvalidOperationException("A collection cannot be its own parent.");
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            if (newParentId.HasValue)
            {
                using var cycleCheck = connection.CreateCommand();
                cycleCheck.Transaction = transaction;
                cycleCheck.CommandText = """
                    WITH RECURSIVE descendants(id) AS (
                        SELECT id FROM request_collection WHERE parent_id = $id
                        UNION ALL
                        SELECT child.id FROM request_collection AS child
                        JOIN descendants AS parent ON child.parent_id = parent.id
                    )
                    SELECT EXISTS (SELECT 1 FROM descendants WHERE id = $parent_id);
                    """;
                cycleCheck.Parameters.AddWithValue("$id", collectionId);
                cycleCheck.Parameters.AddWithValue("$parent_id", newParentId.Value);
                if (Convert.ToInt32(cycleCheck.ExecuteScalar(), CultureInfo.InvariantCulture) != 0)
                    throw new InvalidOperationException("A collection cannot be moved below one of its descendants.");
            }

            using var move = connection.CreateCommand();
            move.Transaction = transaction;
            move.CommandText = "UPDATE request_collection SET parent_id = $parent_id WHERE id = $id;";
            move.Parameters.AddWithValue("$parent_id", (object?)newParentId ?? DBNull.Value);
            move.Parameters.AddWithValue("$id", collectionId);
            try
            {
                if (move.ExecuteNonQuery() == 0) throw new KeyNotFoundException("The collection no longer exists.");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("The destination collection no longer exists, or this level already has the same name.", ex);
            }
            transaction.Commit();
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCollectionAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        if (collectionId <= 0) throw new ArgumentOutOfRangeException(nameof(collectionId), "The default collection cannot be deleted.");
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            long? parentId;
            using (var getParent = connection.CreateCommand())
            {
                getParent.Transaction = transaction;
                getParent.CommandText = "SELECT parent_id FROM request_collection WHERE id = $id;";
                getParent.Parameters.AddWithValue("$id", collectionId);
                var result = getParent.ExecuteScalar();
                if (result == null) throw new KeyNotFoundException("The collection no longer exists.");
                parentId = result is DBNull ? null : Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }

            // Preserve child collections by promoting them one level when their parent is deleted.
            using (var moveChildren = connection.CreateCommand())
            {
                moveChildren.Transaction = transaction;
                moveChildren.CommandText = "UPDATE request_collection SET parent_id = $parent_id WHERE parent_id = $id;";
                moveChildren.Parameters.AddWithValue("$parent_id", (object?)parentId ?? DBNull.Value);
                moveChildren.Parameters.AddWithValue("$id", collectionId);
                moveChildren.ExecuteNonQuery();
            }

            long[] affectedHistoryIds;
            using (var getLinks = connection.CreateCommand())
            {
                getLinks.Transaction = transaction;
                getLinks.CommandText = "SELECT history_id FROM collection_history WHERE collection_id = $id;";
                getLinks.Parameters.AddWithValue("$id", collectionId);
                using var reader = getLinks.ExecuteReader();
                var ids = new List<long>();
                while (reader.Read()) ids.Add(reader.GetInt64(0));
                affectedHistoryIds = ids.ToArray();
            }

            using (var deleteLinks = connection.CreateCommand())
            {
                deleteLinks.Transaction = transaction;
                deleteLinks.CommandText = "DELETE FROM collection_history WHERE collection_id = $id;";
                deleteLinks.Parameters.AddWithValue("$id", collectionId);
                deleteLinks.ExecuteNonQuery();
            }
            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM request_collection WHERE id = $id;";
                delete.Parameters.AddWithValue("$id", collectionId);
                delete.ExecuteNonQuery();
            }
            foreach (var historyId in affectedHistoryIds)
            {
                using var restoreDefault = connection.CreateCommand();
                restoreDefault.Transaction = transaction;
                restoreDefault.CommandText = """
                    INSERT INTO collection_history (collection_id, history_id, created_at_utc)
                    SELECT 0, $history_id, $created_at_utc
                    WHERE NOT EXISTS (
                        SELECT 1 FROM collection_history WHERE history_id = $history_id);
                    """;
                restoreDefault.Parameters.AddWithValue("$history_id", historyId);
                restoreDefault.Parameters.AddWithValue("$created_at_utc", ToStorageTime(DateTimeOffset.UtcNow));
                restoreDefault.ExecuteNonQuery();
            }
            transaction.Commit();
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RequestHistorySummary>> GetCollectionHistoryAsync(long collectionId, CancellationToken cancellationToken = default)
    {
        if (collectionId < 0) return Array.Empty<RequestHistorySummary>();
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run<IReadOnlyList<RequestHistorySummary>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT h.id, h.created_at_utc, h.method, h.url
                FROM request_history AS h
                JOIN collection_history AS ch ON ch.history_id = h.id
                WHERE ch.collection_id = $collection_id
                ORDER BY h.created_at_utc DESC, h.id DESC;
                """;
            command.Parameters.AddWithValue("$collection_id", collectionId);
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

    public async Task<IReadOnlyList<long>> GetHistoryCollectionIdsAsync(long historyId, CancellationToken cancellationToken = default)
    {
        if (historyId < 1) return Array.Empty<long>();
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run<IReadOnlyList<long>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT collection_id FROM collection_history WHERE history_id = $history_id ORDER BY collection_id;";
            command.Parameters.AddWithValue("$history_id", historyId);
            using var reader = command.ExecuteReader();
            var ids = new List<long>();
            while (reader.Read()) ids.Add(reader.GetInt64(0));
            return ids;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetHistoryCollectionsAsync(long historyId, IEnumerable<long> collectionIds, CancellationToken cancellationToken = default)
    {
        if (historyId < 1) throw new ArgumentOutOfRangeException(nameof(historyId));
        var ids = collectionIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0) ids = new long[] { 0 };
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM collection_history WHERE history_id = $history_id;";
                delete.Parameters.AddWithValue("$history_id", historyId);
                delete.ExecuteNonQuery();
            }
            foreach (var collectionId in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO collection_history (collection_id, history_id, created_at_utc) VALUES ($collection_id, $history_id, $created_at_utc);";
                insert.Parameters.AddWithValue("$collection_id", collectionId);
                insert.Parameters.AddWithValue("$history_id", historyId);
                insert.Parameters.AddWithValue("$created_at_utc", ToStorageTime(DateTimeOffset.UtcNow));
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetCollectionHistoryMembersAsync(long collectionId, IEnumerable<long> historyIds, CancellationToken cancellationToken = default)
    {
        if (collectionId <= 0) throw new ArgumentOutOfRangeException(nameof(collectionId), "The default collection is managed automatically.");
        var selectedIds = historyIds.Where(id => id > 0).Distinct().ToHashSet();
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var previousIds = new List<long>();
            using (var validateCollection = connection.CreateCommand())
            {
                validateCollection.Transaction = transaction;
                validateCollection.CommandText = "SELECT EXISTS (SELECT 1 FROM request_collection WHERE id = $collection_id);";
                validateCollection.Parameters.AddWithValue("$collection_id", collectionId);
                if (Convert.ToInt32(validateCollection.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                    throw new KeyNotFoundException("The collection no longer exists.");
            }
            using (var getMembers = connection.CreateCommand())
            {
                getMembers.Transaction = transaction;
                getMembers.CommandText = "SELECT history_id FROM collection_history WHERE collection_id = $collection_id;";
                getMembers.Parameters.AddWithValue("$collection_id", collectionId);
                using var reader = getMembers.ExecuteReader();
                while (reader.Read()) previousIds.Add(reader.GetInt64(0));
            }

            using (var deleteCurrent = connection.CreateCommand())
            {
                deleteCurrent.Transaction = transaction;
                deleteCurrent.CommandText = "DELETE FROM collection_history WHERE collection_id = $collection_id;";
                deleteCurrent.Parameters.AddWithValue("$collection_id", collectionId);
                deleteCurrent.ExecuteNonQuery();
            }

            foreach (var historyId in previousIds.Where(id => !selectedIds.Contains(id)))
            {
                using var restoreDefault = connection.CreateCommand();
                restoreDefault.Transaction = transaction;
                restoreDefault.CommandText = """
                    INSERT INTO collection_history (collection_id, history_id, created_at_utc)
                    SELECT 0, $history_id, $created_at_utc
                    WHERE NOT EXISTS (
                        SELECT 1 FROM collection_history WHERE history_id = $history_id);
                    """;
                restoreDefault.Parameters.AddWithValue("$history_id", historyId);
                restoreDefault.Parameters.AddWithValue("$created_at_utc", ToStorageTime(DateTimeOffset.UtcNow));
                restoreDefault.ExecuteNonQuery();
            }

            foreach (var historyId in selectedIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var replaceDefault = connection.CreateCommand();
                replaceDefault.Transaction = transaction;
                replaceDefault.CommandText = "DELETE FROM collection_history WHERE collection_id = 0 AND history_id = $history_id;";
                replaceDefault.Parameters.AddWithValue("$history_id", historyId);
                replaceDefault.ExecuteNonQuery();

                using var addMember = connection.CreateCommand();
                addMember.Transaction = transaction;
                addMember.CommandText = "INSERT OR IGNORE INTO collection_history (collection_id, history_id, created_at_utc) VALUES ($collection_id, $history_id, $created_at_utc);";
                addMember.Parameters.AddWithValue("$collection_id", collectionId);
                addMember.Parameters.AddWithValue("$history_id", historyId);
                addMember.Parameters.AddWithValue("$created_at_utc", ToStorageTime(DateTimeOffset.UtcNow));
                addMember.ExecuteNonQuery();
            }
            transaction.Commit();
        }, cancellationToken).ConfigureAwait(false);
    }

    private static string ValidateCollectionName(string name)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 100) throw new ArgumentException("Collection names must contain between 1 and 100 characters.", nameof(name));
        return name;
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
            CREATE UNIQUE INDEX IF NOT EXISTS uk_method_url
                ON request_history(method, url);

            CREATE TABLE IF NOT EXISTS request_history_detail (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                history_id INTEGER NOT NULL UNIQUE,
                created_at_utc TEXT NOT NULL,
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

            CREATE TABLE IF NOT EXISTS request_collection (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL CHECK (length(trim(name)) BETWEEN 1 AND 100),
                parent_id INTEGER NULL,
                created_at_utc TEXT NOT NULL,
                FOREIGN KEY (parent_id) REFERENCES request_collection(id) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS idx_request_collection_parent_name
                ON request_collection(parent_id, name COLLATE NOCASE);

            CREATE TABLE IF NOT EXISTS collection_history (
                collection_id INTEGER NOT NULL,
                history_id INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                PRIMARY KEY (collection_id, history_id),
                FOREIGN KEY (collection_id) REFERENCES request_collection(id) ON DELETE CASCADE,
                FOREIGN KEY (history_id) REFERENCES request_history(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS idx_collection_history_history
                ON collection_history(history_id);
            """;
        schema.ExecuteNonQuery();
        using (var ensureDefault = connection.CreateCommand())
        {
            ensureDefault.Transaction = transaction;
            ensureDefault.CommandText = """
                INSERT OR IGNORE INTO request_collection (id, name, parent_id, created_at_utc)
                VALUES (0, 'Default', NULL, $created_at_utc);
                """;
            ensureDefault.Parameters.AddWithValue("$created_at_utc", ToStorageTime(DateTimeOffset.UtcNow));
            ensureDefault.ExecuteNonQuery();
        }
        using (var placeUnassignedRequests = connection.CreateCommand())
        {
            placeUnassignedRequests.Transaction = transaction;
            placeUnassignedRequests.CommandText = """
                INSERT OR IGNORE INTO collection_history (collection_id, history_id, created_at_utc)
                SELECT 0, h.id, h.created_at_utc
                FROM request_history AS h
                WHERE NOT EXISTS (
                    SELECT 1 FROM collection_history AS ch WHERE ch.history_id = h.id);
                """;
            placeUnassignedRequests.ExecuteNonQuery();
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
