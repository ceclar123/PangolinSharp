using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using DynamicData;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pangolin.Desktop.Models;
using Pangolin.Utility;
using Pangolin.Utility.Aes;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Encode;

public class AesUserControlViewModel : ViewModelBase
{
    public ObservableCollection<AesCipherModeItem> CipherModeItems { get; } = new ObservableCollection<AesCipherModeItem>(AesUtil.GetAesCipherModeItemList());

    private AesCipherModeItem _selectedCipherMode;

    public AesCipherModeItem SelectedCipherMode
    {
        get => _selectedCipherMode;
        set
        {
            if (value != null && value != _selectedCipherMode)
            {
                _selectedCipherMode = value;
                // 刷新PaddingMode
                PaddingModeItems.Clear();
                PaddingModeItems.AddRange(AesUtil.GetAesPaddingModeItemList(value.Mode));
                // 默认选中PaddingMode
                SelectedPaddingMode = PaddingModeItems.First();
                this.RaisePropertyChanged(nameof(SelectedPaddingMode));
            }
        }
    }

    public ObservableCollection<AesPaddingModeItem> PaddingModeItems { get; } = new ObservableCollection<AesPaddingModeItem>(AesUtil.GetAesPaddingModeItemList(AesUtil.GetAesCipherModeItemList()[0].Mode));

    public AesPaddingModeItem SelectedPaddingMode { get; set; }

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
        _selectedCipherMode = CipherModeItems.First();
        SelectedPaddingMode = PaddingModeItems.First();

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
            await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
        else
        {
            try
            {
                await Task.Run(() =>
                {
                    Ciphertext = AesUtil.Encrypt(Plaintext.Trim(), Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(Iv), SelectedCipherMode.Mode, SelectedPaddingMode.Padding);
                    // 通知改变
                    this.RaisePropertyChanged(nameof(Ciphertext));
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
        if (string.IsNullOrWhiteSpace(Ciphertext) || string.IsNullOrWhiteSpace(Key) || string.IsNullOrWhiteSpace(Iv))
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
                    Plaintext = AesUtil.Decrypt(Ciphertext.Trim(), Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(Iv), SelectedCipherMode.Mode, SelectedPaddingMode.Padding);
                    // 通知改变
                    this.RaisePropertyChanged(nameof(Plaintext));
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