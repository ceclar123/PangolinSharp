using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Image;

public class ImageViewUserControlViewModel : ViewModelBase
{
    public String? ImageUrl { get; set; } = string.Empty;
    public Bitmap? ImageSource { get; private set; } = null;
    public string LocalFilePath { get; private set; }
    public ReactiveCommand<Unit, Unit> CmdLoadImage { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdBrowseFilePath { get; protected set; }


    public ImageViewUserControlViewModel()
    {
        CmdLoadImage = ReactiveCommand.CreateFromTask(LoadImage);
        CmdBrowseFilePath = ReactiveCommand.CreateFromTask(BrowseFilePath);
    }

    private async Task BrowseFilePath()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Image As",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "Image Files",
                    Extensions = { "jpg", "jpeg", "png", "bmp", "gif" }
                }
            }
        };

        var result = await dialog.ShowAsync(new Window());
        if (!string.IsNullOrWhiteSpace(result))
        {
            LocalFilePath = result;
            // 下载图片
            await this.DownloadImageAsync();
        }
    }


    private async Task LoadImage()
    {
        if (string.IsNullOrWhiteSpace(ImageUrl))
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
                byte[] imageBytes = this.DownloadImageAsync(ImageUrl);
                using var ms = new MemoryStream(imageBytes);
                ImageSource = new Bitmap(ms);
                // 通知改变
                this.RaisePropertyChanged(nameof(ImageSource));
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
        if (string.IsNullOrWhiteSpace(ImageUrl) || string.IsNullOrWhiteSpace(LocalFilePath))
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
}