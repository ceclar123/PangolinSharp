using Avalonia.Controls;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using Pangolin.Utility;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

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

                        byte[] data = GzipUtil.Compress(System.Text.Encoding.UTF8.GetBytes(From.Trim()));
                        To = Base64Util.Encode(data);
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

                        byte[] data = Base64Util.Decode(From.Trim());
                        To = Encoding.UTF8.GetString(GzipUtil.Decompress(data));
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
