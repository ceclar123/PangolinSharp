using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using DynamicData;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Json;

public class JsonConvertCsvUserControlViewModel : ViewModelBase
{
    public string? JsonString { get; set; } = string.Empty;
    public ObservableCollection<Dictionary<string, object>> Rows { get; set; } = new ObservableCollection<Dictionary<string, object>>();
    public ObservableCollection<DataGridColumn> Columns { get; } = new ObservableCollection<DataGridColumn>();

    public ReactiveCommand<Unit, Unit> CmdCheck { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdExport { get; protected set; }

    public string? SplitChar { get; set; }

    public JsonConvertCsvUserControlViewModel()
    {
        CmdCheck = ReactiveCommand.CreateFromTask(CheckJson);
        CmdExport = ReactiveCommand.CreateFromTask(ExportCsv);
    }

    private async Task ExportCsv()
    {
    }


    private async Task CheckJson()
    {
        if (string.IsNullOrWhiteSpace(JsonString))
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info,
                WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        try
        {
            List<Dictionary<string, object>> tmpList = new List<Dictionary<string, object>>();
            if (JsonString.Trim().StartsWith("{"))
            {
                Dictionary<string, object>? dic = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonString);
                if (dic is not null)
                {
                    tmpList.Add(dic);
                }
            }
            else
            {
                List<Dictionary<string, object>>? dicList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(JsonString);
                if (dicList is not null)
                {
                    tmpList.AddRange(dicList);
                }
            }

            JsonString = JsonSerializer.Serialize(tmpList, new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
            this.RaisePropertyChanged(nameof(JsonString));
            //this.RaisePropertyChanged(nameof(Rows));

            List<string> cols = tmpList.SelectMany(it => it.Keys).Where(it => !string.IsNullOrWhiteSpace(it)).Distinct().ToList();
            Columns.Clear();
            foreach (string col in cols)
            {
                Columns.Add(new DataGridTextColumn() { Header = col, Binding = new Binding() { Path = col, Mode = BindingMode.Default } });
            }

            Rows.Clear();
            Rows.AddRange(tmpList);

            //this.RaisePropertyChanged(nameof(Columns));
        }
        catch (Exception e)
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
        }
    }
}