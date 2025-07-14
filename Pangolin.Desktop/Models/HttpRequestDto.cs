using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using DynamicData;
using Pangolin.Utility;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Pangolin.Desktop.Models;

[Serializable]
public class HttpRequestDto : ReactiveValidationObject
{
    private readonly object _paramSyncLock = new object();

    private string? _requestUrl = string.Empty;

    public string? RequestUrl
    {
        get => _requestUrl;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                string? rtn = this.RaiseAndSetIfChanged(ref _requestUrl, value);
                this.TryUpdateParamsByRequestUrl(rtn);
            }
        }
    }

    public ObservableCollection<KeyValueDto<string, string>> Params { get; set; }
    public ObservableCollection<KeyValueDto<string, string>> Headers { get; set; }


    public HttpRequestDto()
    {
        RequestUrl = string.Empty;
        Params = [AddParams(string.Empty, string.Empty)];
        // Params.CollectionChanged += Params_NotifyCollectionChangedEventHandler;
        Headers = [AddHeaders(string.Empty, string.Empty)];

        this.ValidationRule(
            viewModel => viewModel.RequestUrl, it => string.IsNullOrWhiteSpace(it) || HttpUtil.IsValidHttpUrl(it), "非法请求地址");
    }

    public static HttpRequestDto Empty()
    {
        return new HttpRequestDto();
    }


    #region 请求地址变更,更新Params数据

    private void TryUpdateParamsByRequestUrl(string? url)
    {
        if (Monitor.TryEnter(_paramSyncLock))
        {
            try
            {
                UpdateParamsByRequestUrl(url);
            }
            catch (Exception ex)
            {
                // 忽略异常
            }
            finally
            {
                Monitor.Exit(_paramSyncLock);
            }
        }
    }


    /// <summary>
    ///  输入:http://a/b/c/123.jpg?a=1&b=2&c=3
    /// </summary>
    /// <param name="url"></param>
    private void UpdateParamsByRequestUrl(string? url)
    {
        if (url is null || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        int index = url.IndexOf("?", StringComparison.Ordinal);
        if (index <= 0 || index >= url.Length - 1)
        {
            return;
        }

        string[] array = url.Substring(index + 1).Split('&');
        var list = array
            .Where(it => !string.IsNullOrWhiteSpace(it) && it.Contains('='))
            .Select(it =>
            {
                string[] tmpArray = it.Split('=');
                string key = tmpArray.Length >= 1 ? StringUtil.Trim(tmpArray[0]) : string.Empty;
                string value = tmpArray.Length >= 2 ? StringUtil.Trim(tmpArray[1]) : string.Empty;
                return AddParams(key, value);
            }).Where(it => !string.IsNullOrWhiteSpace(it.Key))
            .ToList();
        if (CollectionUtil.IsNotEmpty(list))
        {
            list.Add(AddParams(string.Empty, string.Empty));
            Params.Clear();
            Params.AddRange(list);
        }
    }

    #endregion

    #region Params变更更新请求地址

    public void Params_NotifyCollectionChangedEventHandler(object? sender, NotifyCollectionChangedEventArgs e)
    {
        TryUpdateRequestUrlByParams();
    }

    private void TryUpdateRequestUrlByParams()
    {
        if (Monitor.TryEnter(_paramSyncLock))
        {
            try
            {
                UpdateRequestUrlByParams();
            }
            catch (Exception ex)
            {
                // 忽略异常
            }
            finally
            {
                Monitor.Exit(_paramSyncLock);
            }
        }
    }

    private void UpdateRequestUrlByParams()
    {
        if (RequestUrl is null || string.IsNullOrWhiteSpace(RequestUrl) || !HttpUtil.IsValidHttpUrl(RequestUrl))
        {
            return;
        }

        int i = 0;
        StringBuilder builder = new StringBuilder();
        foreach (var item in Params)
        {
            if (!string.IsNullOrWhiteSpace(item.Key) || !string.IsNullOrWhiteSpace(item.Value))
            {
                if (i > 0)
                {
                    builder.Append("&");
                }

                builder.Append(item.Key is null ? string.Empty : item.Key.Trim())
                    .Append("=")
                    .Append(item.Value is null ? string.Empty : item.Value.Trim());
            }

            i++;
        }

        if (i <= 0)
        {
            return;
        }

        int index = RequestUrl.IndexOf("?", StringComparison.Ordinal);
        if (index < 0 || index >= RequestUrl.Length - 1)
        {
            RequestUrl = RequestUrl + "?" + builder.ToString();
        }
        else if (index == 0)
        {
            RequestUrl += builder.ToString();
        }
        else
        {
            string tmpUrl = RequestUrl.Substring(0, index) + "?" + builder.ToString();
            if (string.CompareOrdinal(tmpUrl, RequestUrl) != 0)
            {
                RequestUrl = tmpUrl;
            }
        }
    }

    #endregion

    #region Params数据项变更,更新请求地址

    private KeyValueDto<string, string> AddParams(string key, string value)
    {
        KeyValueDto<string, string> item = new KeyValueDto<string, string>()
        {
            Key = key,
            Value = value
        };
        item.PropertyChanged += Params_PropertyChangedEventHandler;

        return item;
    }

    public void Params_PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
    {
        TryUpdateRequestUrlByParams();
    }

    #endregion

    #region headers数据项变更,更新请求地址

    private KeyValueDto<string, string> AddHeaders(string key, string value)
    {
        KeyValueDto<string, string> item = new KeyValueDto<string, string>()
        {
            Key = key,
            Value = value
        };
        item.PropertyChanged += Headers_PropertyChangedEventHandler;

        return item;
    }

    public void Headers_PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
    {
        if ("Key".Equals(e.PropertyName))
        {
            bool find = Headers.Any(it => string.IsNullOrWhiteSpace(it.Key));
            if (!find)
            {
                Headers.Add(AddHeaders(string.Empty, string.Empty));
            }
        }
    }

    #endregion
}