using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Pangolin.Utility;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Encode
{
    public class GzipUserControlViewModel : ViewModelBase
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;

        public ReactiveCommand<Unit, Unit> CmdCompress { get; protected set; }
        public ReactiveCommand<Unit, Unit> CmdDecompress { get; protected set; }

        public GzipUserControlViewModel()
        {
            CmdCompress = ReactiveCommand.CreateFromTask(CommandCompress);
            CmdDecompress = ReactiveCommand.CreateFromTask(CommandDecompress);
        }

        private async Task CommandCompress()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner);
                await box.ShowWindowDialogAsync(this.ParentWindow);
            }
            else
            {
                try
                {
                    await Task.Run(() =>
                    {
                        To = GzipUtil.CompressAndBase64Encode(From.Trim());
                        // 通知改变
                        this.RaisePropertyChanged(nameof(To));
                    });
                }
                catch (Exception e)
                {
                    IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                    await box.ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }

        private async Task CommandDecompress()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner);
                await box.ShowWindowDialogAsync(this.ParentWindow);
            }
            else
            {
                try
                {
                    await Task.Run(() =>
                    {
                        To = GzipUtil.DecompressBase64Decode(From.Trim());
                        // 通知改变
                        this.RaisePropertyChanged(nameof(To));
                    });
                }
                catch (Exception e)
                {
                    IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                    await box.ShowWindowDialogAsync(this.ParentWindow);
                }
            }
        }
    }
}