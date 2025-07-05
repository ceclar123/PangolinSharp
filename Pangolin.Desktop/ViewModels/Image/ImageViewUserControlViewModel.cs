using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pangolin.Utility;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Image;

public class ImageViewUserControlViewModel : ViewModelBase
{
    public String? ImageUrl { get; set; } = string.Empty;
    public Bitmap? ImageSource { get; private set; } = null;
    public string? LocalFilePath { get; private set; }
    public ReactiveCommand<Unit, Unit> CmdLoadImage { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdBrowseFilePath { get; protected set; }

    public ImageViewUserControlViewModel()
    {
        CmdLoadImage = ReactiveCommand.CreateFromTask(LoadImage);
        CmdBrowseFilePath = ReactiveCommand.CreateFromTask(BrowseFilePath);
    }

    private async Task BrowseFilePath()
    {
        if (!CommonUtil.IsValidHttpUrl(ImageUrl))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "图片链接非法", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
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
            Title = "图片另存为",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("图片")
                {
                    Patterns = new List<string> { "*.jpg", "*.png" }
                }
            }
        });

        if (file is null)
        {
            return;
        }

        LocalFilePath = file.Path.AbsolutePath;
        // 下载图片
        await this.DownloadImageAsync();
    }


    private async Task LoadImage()
    {
        if (!CommonUtil.IsValidHttpUrl(ImageUrl))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "图片链接非法", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                byte[] imageBytes = this.DownloadImageAsync(ImageUrl ?? string.Empty);
                using var ms = new MemoryStream(imageBytes);
                ImageSource = new Bitmap(ms);
                // 通知改变
                this.RaisePropertyChanged(nameof(ImageSource));
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
        using var client = new HttpClient();
        return client.GetByteArrayAsync(url).Result;
    }

    private async Task DownloadImageAsync()
    {
        if (!CommonUtil.IsValidHttpUrl(ImageUrl) || string.IsNullOrWhiteSpace(LocalFilePath))
        {
            return;
        }

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(ImageUrl);
            response.EnsureSuccessStatusCode();
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            // 保存到本地文件
            await File.WriteAllBytesAsync(LocalFilePath, imageBytes);

            await MessageBoxManager.GetMessageBoxStandard("提示", "保存成功", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
        catch (Exception e)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
    }
}