using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Pangolin.Desktop.ViewModels;
using Pangolin.Desktop.Views;
using System;

namespace Pangolin.Desktop
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                desktop.MainWindow.DataContext = new MainWindowViewModel();

                if (desktop.MainWindow.Screens.Primary != null)
                {
                    desktop.MainWindow.Width = desktop.MainWindow.Screens.Primary.Bounds.Width / 2.0;
                    desktop.MainWindow.Height = desktop.MainWindow.Screens.Primary.Bounds.Height / 2.0;
                }
                else
                {
                    desktop.MainWindow.Width = 800;
                    desktop.MainWindow.Height = 600;
                }

                // 关闭窗口不要自动退出程序
                // desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            }
            base.OnFrameworkInitializationCompleted();
        }

        public void TrayIcon_OnClicked(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.Show();
                if (desktop.MainWindow?.WindowState == WindowState.Minimized)
                {
                    desktop.MainWindow?.WindowState = WindowState.Normal;
                }
                desktop.MainWindow?.Activate();
            }
        }

        public void Close_OnClick(object? sender, EventArgs e)
        {
            // Environment.Exit(0);
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktopLifetime:
                    desktopLifetime.TryShutdown();
                    break;
                case IControlledApplicationLifetime controlledLifetime:
                    controlledLifetime.Shutdown();
                    break;
            }
        }
    }
}