using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CsvHelper;
using CsvHelper.Configuration;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;

namespace Pangolin.Desktop.Views.Json;

public partial class JsonConvertCsvUserControl : UserControlBase
{
    public JsonConvertCsvUserControl()
    {
        InitializeComponent();
    }

    private readonly DataTable _dataTable = new DataTable();

    private void BtnCheck_OnClick(object? sender, RoutedEventArgs e)
    {
        string jsonString = this.TxtJsonString.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info,
                WindowStartupLocation.CenterOwner);
            box.ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        try
        {
            List<Dictionary<string, object?>> tmpList = this.GetDataGridList(jsonString.Trim());
            // 文本框赋值
            this.TxtJsonString.Text = JsonSerializer.Serialize(tmpList, new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });

            // DataGird赋值
            List<string> cols = tmpList.SelectMany(it => it.Keys).Where(it => !string.IsNullOrWhiteSpace(it)).Distinct().ToList();
            this.ConvertToDataTable(tmpList, cols);

            this.JsonDataGrid.Columns.Clear();
            foreach (DataColumn column in _dataTable.Columns)
            {
                this.JsonDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = column.ColumnName,
                    Binding = new Binding($"Row.ItemArray[{column.Ordinal}]")
                });
            }

            this.JsonDataGrid.ItemsSource = new DataView();
            this.JsonDataGrid.ItemsSource = _dataTable.DefaultView;
        }
        catch (Exception ex)
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("错误", ex.Message, ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner);
            box.ShowWindowDialogAsync(this.ParentWindow);
        }
    }

    private List<Dictionary<string, object?>> GetDataGridList(string jsonString)
    {
        if (jsonString.Trim().StartsWith("{"))
        {
            Dictionary<string, object?>? dic = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString);
            return dic is null ? new List<Dictionary<string, object?>>() : new List<Dictionary<string, object?>>() { dic };
        }
        else
        {
            List<Dictionary<string, object?>>? dicList = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(jsonString);
            return dicList ?? new List<Dictionary<string, object?>>();
        }
    }

    private void ConvertToDataTable(List<Dictionary<string, object?>> dicList, List<string> cols)
    {
        // 列定义
        _dataTable.Columns.Clear();
        foreach (var col in cols)
        {
            _dataTable.Columns.Add(col, typeof(string));
        }

        // 数据行
        _dataTable.Rows.Clear();
        foreach (var dic in dicList)
        {
            DataRow row = _dataTable.NewRow();
            foreach (var col in cols)
            {
                row[col] = dic.TryGetValue(col, out var value) ? value?.ToString() ?? String.Empty : string.Empty;
            }

            _dataTable.Rows.Add(row);
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
                    Title = "CSV另存为",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new FilePickerFileType("CSV")
                        {
                            Patterns = new List<string> { "*.csv" }
                        }
                    }
                });
            if (fileTask is null)
            {
                return;
            }

            string split = this.TxtSplit.Text ?? string.Empty;
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = string.IsNullOrWhiteSpace(split) ? "\t" : split.Trim()
            };

            await using var writer = new StreamWriter(fileTask.Path.AbsolutePath);
            await using var csv = new CsvWriter(writer, config);

            // 写入表头
            foreach (DataColumn column in _dataTable.Columns)
            {
                csv.WriteField(column.ColumnName);
            }

            await csv.NextRecordAsync();

            // 写入数据行
            foreach (DataRow row in _dataTable.Rows)
            {
                foreach (DataColumn column in _dataTable.Columns)
                {
                    csv.WriteField(row[column]);
                }

                await csv.NextRecordAsync();
            }

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