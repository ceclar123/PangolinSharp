using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Notification = Avalonia.Controls.Notifications.Notification;
using DynamicData;

using Pangolin.Desktop.Models;
using Pangolin.Utility;
using Pangolin.Utility.Aes;
using ReactiveUI;
using ReactiveUI.Primitives;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

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

    public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdGenKey { get; protected set; }
    public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdGenIv { get; protected set; }
    public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdEncrypt { get; protected set; }
    public ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid> CmdDecrypt { get; protected set; }

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
            MainNotificationManager?.Show(new Avalonia.Controls.Notifications.Notification("提示", "参数为空", NotificationType.Information));
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
                MainNotificationManager?.Show(new Notification("错误", e.Message, NotificationType.Error));
            }
        }
    }

    private async Task CommandDecompress()
    {
        if (string.IsNullOrWhiteSpace(Ciphertext) || string.IsNullOrWhiteSpace(Key) || string.IsNullOrWhiteSpace(Iv))
        {
            MainNotificationManager?.Show(new Notification("提示", "参数为空", NotificationType.Information));
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
                MainNotificationManager?.Show(new Notification("错误", e.Message, NotificationType.Error));
            }
        }
    }
}