using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Pangolin.Desktop.Models;

public class HttpRequestSettingDto : ReactiveValidationObject
{
    public SelectItemDto? SelectedVersionMethod { get; set; }

    private int? _timeout;

    public int? Timeout
    {
        get => _timeout;
        set => this.RaiseAndSetIfChanged(ref _timeout, value);
    }

    public HttpRequestSettingDto()
    {
        this.ValidationRule(
            viewModel => viewModel.Timeout, val => val is > 0, "整数[1,200]");
    }
}