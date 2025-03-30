using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
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
        TopLevel? topLevel = TopLevel.GetTopLevel(ParentWindow);
        IStorageProvider? storageProvider = topLevel?.StorageProvider;
        if (storageProvider == null)
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "文件框系统错误", ButtonEnum.Ok, Icon.Info,
                WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
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

        if (ObjectUtil.IsNull(file))
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "文件系统错误", ButtonEnum.Ok, Icon.Info,
                WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        LocalFilePath = file.Path.AbsolutePath;
        // 下载JSON
        await this.DownloadImageAsync();
    }


    private async Task LoadJson()
    {
        if (string.IsNullOrWhiteSpace(JsonUrl))
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info,
                WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                byte[] bytes = this.DownloadImageAsync(JsonUrl);
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
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
        }
    }

    private byte[] DownloadImageAsync(string url)
    {
        using var client = new HttpClient();
        return client.GetByteArrayAsync(url).Result;
    }

    private async Task DownloadImageAsync()
    {
        if (string.IsNullOrWhiteSpace(JsonUrl) || string.IsNullOrWhiteSpace(LocalFilePath))
        {
            return;
        }

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(JsonUrl);
            response.EnsureSuccessStatusCode();
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            // 保存到本地文件
            await File.WriteAllBytesAsync(LocalFilePath, bytes);

            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "保存成功", ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
        }
        catch (Exception e)
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.ParentWindow);
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