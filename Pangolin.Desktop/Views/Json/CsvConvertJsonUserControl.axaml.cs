using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CsvHelper;
using CsvHelper.Configuration;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Pangolin.Desktop.Views.Json;

public partial class CsvConvertJsonUserControl : UserControlBase
{
    public CsvConvertJsonUserControl()
    {
        InitializeComponent();
    }

    private readonly DataTable _dataTable = new DataTable();
    private readonly List<Dictionary<string, string>> _records = new List<Dictionary<string, string>>();

    private async void BtnCheck_OnClick(object? sender, RoutedEventArgs e)
    {
        string csvString = this.TxtCsvString.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(csvString))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info,
                WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        try
        {
            string split = this.TxtSplit.Text ?? string.Empty;
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = string.IsNullOrWhiteSpace(split) ? "\t" : split.Trim(),
                NewLine = Environment.NewLine,
                HasHeaderRecord = true
            };
            _records.Clear();
            _dataTable.Clear();

            using (var reader = new StringReader(csvString))
            using (var csv = new CsvReader(reader, config))
            {
                // 获取列名
                await csv.ReadAsync();
                csv.ReadHeader();
                string[]? headers = csv.HeaderRecord;
                int len = headers?.Length ?? 0;
                // 列定义
                _dataTable.Columns.Clear();
                for (int i = 0; i < len; i++)
                {
                    _dataTable.Columns.Add(headers?[i] ?? string.Empty, typeof(string));
                }

                while (await csv.ReadAsync())
                {
                    DataRow row = _dataTable.NewRow();
                    var record = new Dictionary<string, string>();

                    for (int i = 0; i < len; i++)
                    {
                        var header = headers?[i] ?? string.Empty;
                        var value = csv.GetField(i)?.ToString() ?? string.Empty;
                        record.Add(header, value);
                        row[header] = value;
                    }

                    _records.Add(record);
                    _dataTable.Rows.Add(row);
                }
            }

            this.DataGridCsv.Columns.Clear();
            foreach (DataColumn column in _dataTable.Columns)
            {
                this.DataGridCsv.Columns.Add(new DataGridTextColumn
                {
                    Header = column.ColumnName,
                    Binding = new Binding($"Row.ItemArray[{column.Ordinal}]")
                });
            }

            this.DataGridCsv.ItemsSource = new DataView();
            this.DataGridCsv.ItemsSource = _dataTable.DefaultView;
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", ex.Message, ButtonEnum.Ok,
                    Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
    }

    private async void BtnExport_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this.ParentWindow);
            IStorageProvider? storageProvider = topLevel?.StorageProvider;
            IStorageFile? fileTask = storageProvider is null
                ? null
                : await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "JSON另存为",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new FilePickerFileType("JSON")
                        {
                            Patterns = new List<string> { "*.json" }
                        }
                    }
                });
            if (fileTask is null)
            {
                return;
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };
            string json = JsonSerializer.Serialize(_records, options);
            await File.WriteAllTextAsync(fileTask.Path.AbsolutePath, json, Encoding.UTF8);

            await MessageBoxManager.GetMessageBoxStandard("提示", "保存成功", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", ex.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
    }
}