using System;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Notification = Avalonia.Controls.Notifications.Notification;

using Pangolin.Utility;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Pangolin.Desktop.ViewModels.Encode
{
    public class Base64UserControlViewModel : ViewModelBase
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;

        public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdEncode { get; protected set; }
        public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdDecode { get; protected set; }

        public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdUrlEncode { get; protected set; }
        public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdUrlDecode { get; protected set; }

        public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdMimeEncode { get; protected set; }
        public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdMimeDecode { get; protected set; }

        public Base64UserControlViewModel()
        {
            CmdEncode = ReactiveCommand.CreateFromTask(CommandEncode);
            CmdDecode = ReactiveCommand.CreateFromTask(CommandDecode);

            CmdUrlEncode = ReactiveCommand.CreateFromTask(CommandUrlEncode);
            CmdUrlDecode = ReactiveCommand.CreateFromTask(CommandUrlDecode);

            CmdMimeEncode = ReactiveCommand.CreateFromTask(CommandMimeEncode);
            CmdMimeDecode = ReactiveCommand.CreateFromTask(CommandMimeDecode);
        }

        private async Task CommandEncode()
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
                        To = Base64Util.Encode(Encoding.UTF8.GetBytes(From.Trim()));
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

        private async Task CommandDecode()
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
                        byte[] data = Base64Util.Decode(From.Trim());
                        To = Encoding.UTF8.GetString(data);
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

        private async Task CommandUrlEncode()
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
                        To = Base64Util.UrlSafeEncode(Encoding.UTF8.GetBytes(From.Trim()));
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

        private async Task CommandUrlDecode()
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
                        byte[] data = Base64Util.UrlSafeDecode(From.Trim());
                        To = Encoding.UTF8.GetString(data);
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

        private async Task CommandMimeEncode()
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
                        To = Base64Util.MimeEncode(Encoding.UTF8.GetBytes(From.Trim()));
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

        private async Task CommandMimeDecode()
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
                        byte[] data = Base64Util.MimeDecode(From.Trim());
                        To = Encoding.UTF8.GetString(data);
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
}