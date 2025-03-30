using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Pangolin.Desktop.ViewModels.Json;
using ReactiveUI;

namespace Pangolin.Desktop.Views.Json;

public partial class JsonConvertCsvUserControl : ReactiveUserControl<JsonConvertCsvUserControlViewModel>
{
    public JsonConvertCsvUserControl()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Columns)
                .WhereNotNull()
                .Subscribe(cols =>
                {
                    DataGrid? jsonGrid = this.FindControl<DataGrid>("JsonGrid");
                    jsonGrid?.Columns.Clear();
                    foreach (DataGridColumn col in cols)
                    {
                        jsonGrid?.Columns.Add(col);
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private void GenerateColumns(DataGrid jsonGrid, Collection<DataGridColumn> cols)
    {
        jsonGrid.Columns.Clear();
        foreach (var col in cols)
        {
            jsonGrid.Columns.Add(col);
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        //this.ViewModel.JsonGrid = this.FindControl<DataGrid>("JsonGrid");
    }
}