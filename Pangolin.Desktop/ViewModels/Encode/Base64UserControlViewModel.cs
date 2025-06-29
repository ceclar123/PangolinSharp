using System;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pangolin.Utility;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Encode
{
    public class Base64UserControlViewModel : ViewModelBase
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;

        public ReactiveCommand<Unit, Unit> CmdEncode { get; protected set; }
        public ReactiveCommand<Unit, Unit> CmdDecode { get; protected set; }

        public ReactiveCommand<Unit, Unit> CmdUrlEncode { get; protected set; }
        public ReactiveCommand<Unit, Unit> CmdUrlDecode { get; protected set; }

        public ReactiveCommand<Unit, Unit> CmdMimeEncode { get; protected set; }
        public ReactiveCommand<Unit, Unit> CmdMimeDecode { get; protected set; }

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
                await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(this.ParentWindow);
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
                    await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                        .ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }

        private async Task CommandDecode()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(this.ParentWindow);
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
                    await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                        .ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }

        private async Task CommandUrlEncode()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(this.ParentWindow);
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
                    await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                        .ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }

        private async Task CommandUrlDecode()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(this.ParentWindow);
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
                    await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                        .ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }

        private async Task CommandMimeEncode()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(this.ParentWindow);
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
                    await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                        .ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }

        private async Task CommandMimeDecode()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(this.ParentWindow);
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
                    await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                        .ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }
    }
}