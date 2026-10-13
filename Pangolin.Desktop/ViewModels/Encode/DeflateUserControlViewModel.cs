using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Notification = Avalonia.Controls.Notifications.Notification;

using Pangolin.Utility;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Pangolin.Desktop.ViewModels.Encode;

public class DeflateUserControlViewModel : ViewModelBase
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;

    public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdCompress { get; protected set; }
    public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdDecompress { get; protected set; }

    public DeflateUserControlViewModel()
    {
        CmdCompress = ReactiveCommand.CreateFromTask(CommandCompress);
        CmdDecompress = ReactiveCommand.CreateFromTask(CommandDecompress);
    }

    private async Task CommandCompress()
    {
        if (string.IsNullOrWhiteSpace(From))
        {
            MainNotificationManager?.Show(new Notification("提示", "参数为空", NotificationType.Information));
        }
        else
        {
            try
            {
                await Task.Run(() =>
                {
                    byte[] data = DeflateUtil.Compress(From.Trim());
                    To = Base64Util.Encode(data);
                    // 通知改变
                    this.RaisePropertyChanged(nameof(To));
                });
            }
            catch (Exception e)
            {
                MainNotificationManager?.Show(new Notification("错误", e.Message, NotificationType.Error));
            }
        }
    }

    private async Task CommandDecompress()
    {
        if (string.IsNullOrWhiteSpace(From))
        {
            MainNotificationManager?.Show(new Notification("提示", "参数为空", NotificationType.Information));
        }
        else
        {
            try
            {
                await Task.Run(() =>
                {
                    To = DeflateUtil.Decompress(Base64Util.Decode(From.Trim()));
                    // 通知改变
                    this.RaisePropertyChanged(nameof(To));
                });
            }
            catch (Exception e)
            {
                MainNotificationManager?.Show(new Notification("错误", e.Message, NotificationType.Error));
            }
        }
    }
}