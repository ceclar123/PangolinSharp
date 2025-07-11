using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;
using Pangolin.Desktop.Models;
using Pangolin.Utility;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using RestSharp;

namespace Pangolin.Desktop.ViewModels.Network;

public class HttpRequestUserControlViewModel : ViewModelBase
{
    public ObservableCollection<SelectItemDto> HttpMethodItems { get; } = GetHttpMethodItems();
    public SelectItemDto SelectedHttpMethod { get; set; }

    public ObservableCollection<SelectItemDto> HttpVersionItems { get; } = GetHttpVersionItems();
    public HttpRequestSettingDto Setting { get; set; }


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

    public ReactiveCommand<Unit, Unit> CmdSend { get; protected set; }

    public HttpRequestUserControlViewModel()
    {
        SelectedHttpMethod = HttpMethodItems.First();
        Setting = new HttpRequestSettingDto
        {
            SelectedVersionMethod = HttpVersionItems.First(),
            Timeout = 3
        };

        CmdSend = ReactiveCommand.CreateFromTask(Send);

        this.ValidationRule(
            viewModel => viewModel.RequestUrl, it => string.IsNullOrWhiteSpace(it) || HttpUtil.IsValidHttpUrl(it), "非法请求地址");
    }


    private static Dictionary<int, Method> _httpMethodDic = new Dictionary<int, Method>();
    private static readonly HashSet<Method> UnSupportHttpMethod = [Method.Merge, Method.Copy, Method.Search];

    private static readonly Dictionary<int, Version> HttpVersionDic = new Dictionary<int, Version>();

    private static ObservableCollection<SelectItemDto> GetHttpMethodItems()
    {
        _httpMethodDic = Enum.GetValues<Method>()
            .Where(it => !UnSupportHttpMethod.Contains(it))
            .ToDictionary(it => (int)it, it => it);

        List<SelectItemDto> list = _httpMethodDic
            .Select(it =>
            {
                SelectItemDto item = new SelectItemDto
                {
                    Name = it.Value.ToString(),
                    Code = it.Key
                };
                return item;
            }).ToList();

        return new ObservableCollection<SelectItemDto>(list);
    }

    private static ObservableCollection<SelectItemDto> GetHttpVersionItems()
    {
        HttpVersionDic.Add(0, HttpVersion.Version11);
        HttpVersionDic.Add(1, HttpVersion.Version20);
        HttpVersionDic.Add(2, HttpVersion.Version30);

        List<SelectItemDto> list = HttpVersionDic
            .Select(it =>
            {
                string minor = it.Value.Minor == 0 ? string.Empty : "." + it.Value.Minor;
                SelectItemDto item = new SelectItemDto
                {
                    Name = $"HTTP/{it.Value.Major}{minor}",
                    Code = it.Key
                };
                return item;
            }).ToList();

        return new ObservableCollection<SelectItemDto>(list);
    }


    private async Task Send()
    {
        await Task.Run(() => { });
    }
}