using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pangolin.Desktop.Models;
using Pangolin.Utility;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Json;

public class JsonViewUserControlViewModel : ViewModelBase
{
    public ObservableCollection<JsonViewNode>? RootNodes { get; set; }
    public string? JsonUrl { get; set; } = string.Empty;
    public string? JsonString { get; set; } = string.Empty;
    public string? LocalFilePath { get; private set; }
    public ReactiveCommand<Unit, Unit> CmdLoadJson { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdBrowseFilePath { get; protected set; }

    public JsonViewUserControlViewModel()
    {
        CmdLoadJson = ReactiveCommand.CreateFromTask(LoadJson);
        CmdBrowseFilePath = ReactiveCommand.CreateFromTask(BrowseFilePath);
    }

    private async Task BrowseFilePath()
    {
        if (!CommonUtil.IsValidHttpUrl(JsonUrl))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "JSON文件链接非法", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(ParentWindow);
        IStorageProvider? storageProvider = topLevel?.StorageProvider;
        if (storageProvider is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "文件框系统错误", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        // 启动异步操作以打开对话框。
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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

        if (file is null)
        {
            return;
        }

        LocalFilePath = file.Path.AbsolutePath;
        // 下载JSON
        await this.DownloadImageAsync();
    }


    private async Task LoadJson()
    {
        if (!CommonUtil.IsValidHttpUrl(JsonUrl))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "JSON文件链接非法", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                byte[] bytes = this.DownloadImageAsync(JsonUrl ?? string.Empty);
                JsonString = Encoding.UTF8.GetString(bytes);
                // 通知改变
                this.RaisePropertyChanged(nameof(JsonString));
                // 创建树
                JsonNode? node = JsonNode.Parse(JsonString);
                if (node != null)
                {
                    JsonViewNode? viewNode = CreateTree(node, node is JsonObject ? "Root" : "");
                    if (viewNode != null)
                    {
                        if (RootNodes is null)
                        {
                            RootNodes = new ObservableCollection<JsonViewNode>();
                        }

                        RootNodes.Clear();
                        RootNodes.Add(viewNode);
                        this.RaisePropertyChanged(nameof(RootNodes));
                    }
                }
            });
        }
        catch (Exception e)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
    }

    private byte[] DownloadImageAsync(string url)
    {
        var handler = new HttpClientHandler
        {
            // 同时支持 gzip 和 deflate 压缩格式
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        using var client = new HttpClient(handler);
        return client.GetByteArrayAsync(url).Result;
    }

    private async Task DownloadImageAsync()
    {
        if (!CommonUtil.IsValidHttpUrl(JsonUrl) || string.IsNullOrWhiteSpace(LocalFilePath))
        {
            return;
        }

        try
        {
            var handler = new HttpClientHandler
            {
                // 同时支持 gzip 和 deflate 压缩格式
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            using var client = new HttpClient(handler);
            using var response = await client.GetAsync(JsonUrl);
            response.EnsureSuccessStatusCode();
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            // 保存到本地文件
            await File.WriteAllBytesAsync(LocalFilePath, bytes);

            await MessageBoxManager.GetMessageBoxStandard("提示", "保存成功", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
        catch (Exception e)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
    }

    private JsonViewNode? CreateTree(JsonNode? jsonNode, string key)
    {
        if (jsonNode == null)
        {
            return null;
        }

        JsonViewNode node = new JsonViewNode() { Name = key };
        if (jsonNode is JsonObject jsonObject)
        {
            int index = 0;
            foreach (var property in jsonObject)
            {
                index++;
                JsonViewNode? tmpNode = CreateTree(property.Value, property.Key);
                if (tmpNode != null)
                {
                    if (node.SubNodes is null)
                    {
                        node.SubNodes = new ObservableCollection<JsonViewNode>();
                    }

                    node.SubNodes.Add(tmpNode);
                }
            }

            node.Value = " {" + index + "}";
        }
        else if (jsonNode is JsonArray jsonArray)
        {
            int index = 0;
            foreach (var item in jsonArray)
            {
                JsonViewNode? tmpNode = CreateTree(item, $"{index++}");
                if (tmpNode != null)
                {
                    if (node.SubNodes is null)
                    {
                        node.SubNodes = new ObservableCollection<JsonViewNode>();
                    }

                    node.SubNodes.Add(tmpNode);
                }
            }

            node.Value = " [" + index + "]";
        }
        else if (jsonNode is JsonValue jsonValue)
        {
            node.Value = " : " + jsonValue.ToString();
        }

        return node;
    }
}