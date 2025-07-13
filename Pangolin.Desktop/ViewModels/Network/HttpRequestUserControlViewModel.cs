using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pangolin.Desktop.Models;
using Pangolin.Utility;
using ReactiveUI;
using RestSharp;

namespace Pangolin.Desktop.ViewModels.Network;

public class HttpRequestUserControlViewModel : ViewModelBase
{
    public ObservableCollection<SelectItemDto> HttpMethodItems { get; } = GetHttpMethodItems();
    public SelectItemDto SelectedHttpMethod { get; set; }

    public ObservableCollection<SelectItemDto> HttpVersionItems { get; } = GetHttpVersionItems();
    public HttpRequestSettingDto Setting { get; set; }
    public HttpRequestDto HttpReq { get; set; }
    public HttpResponseDto HttpResp { get; set; }


    public ReactiveCommand<Unit, Unit> CmdSend { get; protected set; }

    public HttpRequestUserControlViewModel()
    {
        SelectedHttpMethod = HttpMethodItems.First();
        Setting = new HttpRequestSettingDto
        {
            SelectedVersionMethod = HttpVersionItems.First(),
            Timeout = 3
        };
        HttpReq = HttpRequestDto.Empty();
        HttpResp = HttpResponseDto.Empty();

        CmdSend = ReactiveCommand.CreateFromTask(Send);
    }


    private static Dictionary<int, Method> _httpMethodDic = new Dictionary<int, Method>();
    private static readonly HashSet<Method> UnSupportHttpMethod = [Method.Merge, Method.Copy, Method.Search];

    private static readonly Dictionary<int, Version> HttpVersionDic = new Dictionary<int, Version>();

    private static readonly JsonSerializerOptions DefaultOption = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

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
        if (this.HasErrors)
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "参数错误", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        if (string.IsNullOrWhiteSpace(HttpReq.RequestUrl))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "请求链接不能为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        if (!HttpUtil.IsValidHttpUrl(HttpReq.RequestUrl))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "请求链接非法", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                RestRequest request = new RestRequest(HttpReq.RequestUrl);
                request.Method = _httpMethodDic.GetValueOrDefault(SelectedHttpMethod.Code, Method.Get);
                request.Version = Setting.SelectedVersionMethod is null ? HttpVersion.Version11 : HttpVersionDic.GetValueOrDefault(Setting.SelectedVersionMethod.Code, HttpVersion.Version11);
                request.Timeout = TimeSpan.FromSeconds(Setting.Timeout ?? 3);
                RestResponse<string> response = HttpUtil.Do(request);

                stopwatch.Stop();

                // Status: 200 OK Times: 115 ms  Size:123 kb
                HttpResponseDto resp = HttpResponseDto.Empty();
                string size = response.RawBytes is null ? string.Empty : (response.RawBytes.Length / 1024).ToString();
                resp.Status = $"Method: {request.Method}\tStatus: {(int)(response.StatusCode)} {response.StatusCode}\tTimes: {stopwatch.ElapsedMilliseconds} ms\tSize: {size} kb";
                Process(response, resp);
                HttpResp = resp;

                // 通知改变
                this.RaisePropertyChanged(nameof(HttpResp));
            });
        }
        catch (Exception e)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
    }

    private void Process(RestResponse<string> response, HttpResponseDto resp)
    {
        resp.RawBody = response.Content;
        resp.PrettyBody = response.Content;
        bool isJson = response.ContentType is not null && response.ContentType.StartsWith(ContentType.Json.Value);
        if (isJson)
        {
            JsonNode? node = response.Content is null || string.IsNullOrWhiteSpace(response.Content) ? null : JsonNode.Parse(response.Content);
            resp.PrettyBody = node?.ToJsonString(DefaultOption);
        }

        if (response.ContentHeaders != null)
        {
            resp.Headers?.AddRange(response.ContentHeaders
                .Where(ObjectUtil.IsNotNull)
                .Where(it => !string.IsNullOrWhiteSpace(it.Name))
                .Select(it =>
                {
                    HttpHeader header = new HttpHeader
                    {
                        Name = it.Name,
                        Value = it.Value
                    };
                    return header;
                }).ToList()
            );
        }

        if (response.Headers != null)
            resp.Headers?.AddRange(response.Headers
                .Where(ObjectUtil.IsNotNull)
                .Where(it => !string.IsNullOrWhiteSpace(it.Name))
                .Select(it =>
                {
                    HttpHeader header = new HttpHeader
                    {
                        Name = it.Name,
                        Value = it.Value
                    };
                    return header;
                }).ToList()
            );

        if (response.Cookies != null)
        {
            resp.Cookies = response.Cookies
                .Where(ObjectUtil.IsNotNull)
                .Where(it => !string.IsNullOrWhiteSpace(it.Name))
                .Select(it =>
                {
                    HttpCookie cookie = new HttpCookie
                    {
                        Name = it.Name,
                        Value = it.Value,
                        Domain = it.Domain,
                        Path = it.Path,
                        Expires = it.Expires,
                        Expired = it.Expired,
                        HttpOnly = it.HttpOnly,
                        Secure = it.Secure
                    };
                    return cookie;
                }).ToList();
        }

        // 排序
        resp.Headers?.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
        resp.Cookies?.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
    }
}