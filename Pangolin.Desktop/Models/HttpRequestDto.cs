using System;
using System.Collections.ObjectModel;
using Pangolin.Utility;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Pangolin.Desktop.Models;

[Serializable]
public class HttpRequestDto : ReactiveValidationObject
{
    private string? _requestUrl = string.Empty;

    public string? RequestUrl
    {
        get => _requestUrl;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                this.RaiseAndSetIfChanged(ref _requestUrl, value);
            }
        }
    }

    public ObservableCollection<KeyValueDto<string, string>> Params { get; set; }
    public ObservableCollection<KeyValueDto<string, string>> Headers { get; set; }


    public HttpRequestDto()
    {
        RequestUrl = string.Empty;
        Params = new ObservableCollection<KeyValueDto<string, string>>();
        Headers = new ObservableCollection<KeyValueDto<string, string>>();
        this.ValidationRule(
            viewModel => viewModel.RequestUrl, it => string.IsNullOrWhiteSpace(it) || HttpUtil.IsValidHttpUrl(it), "非法请求地址");
    }

    public static HttpRequestDto Empty()
    {
        HttpRequestDto request = new HttpRequestDto();
        request.Params.Add(new KeyValueDto<string, string>());
        request.Headers.Add(new KeyValueDto<string, string>());

        return request;
    }
}