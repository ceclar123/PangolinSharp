using Avalonia.Controls;
using Avalonia.Dialogs;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Pangolin.Utility;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Pangolin.Desktop.ViewModels.Encode
{
    public class Base64UserControlViewModel : ViewModelBase
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;

        public ReactiveCommand<Unit, Unit> CmdEncode { get; protected set; }
        public ReactiveCommand<Unit, Unit> CmdDecode { get; protected set; }

        public Base64UserControlViewModel()
        {
            CmdEncode = ReactiveCommand.CreateFromTask(CommandEncode);
            CmdDecode = ReactiveCommand.CreateFromTask(CommandDecode);
        }

        private async Task CommandEncode()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner);
                await box.ShowWindowDialogAsync(this.ParentWindow);
            }
            else
            {
                await Task.Run(() =>
                {
                    To = Base64Util.Encode(System.Text.Encoding.UTF8.GetBytes(From.Trim()));
                    // 通知改变
                    this.RaisePropertyChanged(nameof(To));
                });
            }
        }

        private async Task CommandDecode()
        {
            if (string.IsNullOrWhiteSpace(From))
            {
                IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner);
                await box.ShowWindowDialogAsync(this.ParentWindow);
            }
            else
            {
                await Task.Run(() =>
                {
                    byte[] data = Base64Util.Decode(From.Trim());
                    To = System.Text.Encoding.UTF8.GetString(data);
                    // 通知改变
                    this.RaisePropertyChanged(nameof(To));
                });
            }
        }
    }
}
