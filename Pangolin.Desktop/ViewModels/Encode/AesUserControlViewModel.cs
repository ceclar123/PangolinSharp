using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Org.BouncyCastle.Crypto;
using Pangolin.Desktop.Models;
using Pangolin.Utility;
using Pangolin.Utility.Aes;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Encode;

public class AesUserControlViewModel : ViewModelBase
{
    public ObservableCollection<AesModeItem> ModeItems { get; } = new ObservableCollection<AesModeItem>(AesUtil.BlockCipherModeList);

    public AesModeItem SelectedMode { get; set; }

    public ObservableCollection<AesPaddingItem> PaddingItems { get; } = new ObservableCollection<AesPaddingItem>(AesUtil.BlockCipherPaddingList);

    public AesPaddingItem SelectedPadding { get; set; }

    public ObservableCollection<SelectItemDto> KeyItems { get; } = new ObservableCollection<SelectItemDto>() { new SelectItemDto(16, "16字节"), new SelectItemDto(24, "24字节"), new SelectItemDto(32, "32字节") };
    public ObservableCollection<SelectItemDto> IvItems { get; } = new ObservableCollection<SelectItemDto>() { new SelectItemDto(16, "16字节") };
    public string Plaintext { get; set; } = string.Empty;
    public string Ciphertext { get; set; } = string.Empty;

    public SelectItemDto SelectedKey { get; set; }
    public SelectItemDto SelectedIv { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Iv { get; set; } = string.Empty;

    public ReactiveCommand<Unit, Unit> CmdGenKey { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdGenIv { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdEncrypt { get; protected set; }
    public ReactiveCommand<Unit, Unit> CmdDecrypt { get; protected set; }

    public AesUserControlViewModel()
    {
        SelectedMode = ModeItems.First();
        SelectedPadding = PaddingItems.First();

        SelectedKey = KeyItems.First();
        SelectedIv = IvItems.First();

        CmdGenKey = ReactiveCommand.CreateFromTask(GenKey);
        CmdGenIv = ReactiveCommand.CreateFromTask(GenIv);
        CmdEncrypt = ReactiveCommand.CreateFromTask(CommandCompress);
        CmdDecrypt = ReactiveCommand.CreateFromTask(CommandDecompress);
    }

    private async Task GenKey()
    {
        await Task.Run(() =>
        {
            Key = CommonUtil.Generate(SelectedKey.Code);
            this.RaisePropertyChanged(nameof(Key));
        });
    }

    private async Task GenIv()
    {
        await Task.Run(() =>
        {
            Iv = CommonUtil.Generate(SelectedIv.Code);
            this.RaisePropertyChanged(nameof(Iv));
        });
    }

    private async Task CommandCompress()
    {
        if (string.IsNullOrWhiteSpace(Plaintext) || string.IsNullOrWhiteSpace(Key) || string.IsNullOrWhiteSpace(Iv))
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
                    ICipherParameters cipherParam = SelectedMode.HasIv ? AesUtil.GetCipherParam(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(Iv)) : AesUtil.GetCipherParam(Encoding.UTF8.GetBytes(Key));
                    byte[]? output = AesFactory.GetHandler(SelectedMode.CfgName)?.Encrypt(Encoding.UTF8.GetBytes(Plaintext.Trim()), SelectedPadding.Padding, cipherParam);
                    Ciphertext = Base64Util.Encode(output ?? []);
                    // 通知改变
                    this.RaisePropertyChanged(nameof(Ciphertext));
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
        if (string.IsNullOrWhiteSpace(Ciphertext) || string.IsNullOrWhiteSpace(Key) || string.IsNullOrWhiteSpace(Iv))
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
                    ICipherParameters cipherParam = SelectedMode.HasIv ? AesUtil.GetCipherParam(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(Iv)) : AesUtil.GetCipherParam(Encoding.UTF8.GetBytes(Key));
                    byte[]? output = AesFactory.GetHandler(SelectedMode.CfgName)?.Decrypt(Base64Util.Decode(Ciphertext.Trim()), SelectedPadding.Padding, cipherParam);
                    Plaintext = Encoding.UTF8.GetString(output ?? []);
                    // 通知改变
                    this.RaisePropertyChanged(nameof(Plaintext));
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