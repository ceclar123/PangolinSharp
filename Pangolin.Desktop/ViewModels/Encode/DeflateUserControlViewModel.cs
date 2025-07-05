using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pangolin.Utility;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Encode;

public class DeflateUserControlViewModel : ViewModelBase
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;

    public ReactiveCommand<Unit, Unit> CmdCompress { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdDecompress { get; protected set; }

    public DeflateUserControlViewModel()
    {
        CmdCompress = ReactiveCommand.CreateFromTask(CommandCompress);
        CmdDecompress = ReactiveCommand.CreateFromTask(CommandDecompress);
    }

    private async Task CommandCompress()
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
                    byte[] data = DeflateUtil.Compress(From.Trim());
                    To = Base64Util.Encode(data);
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

    private async Task CommandDecompress()
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
                    To = DeflateUtil.Decompress(Base64Util.Decode(From.Trim()));
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