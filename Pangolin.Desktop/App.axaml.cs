using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Pangolin.Desktop.ViewModels;
using Pangolin.Desktop.Views;

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
                desktop.MainWindow.DataContext = new MainWindowViewModel() { ParentWindow = desktop.MainWindow };

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
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Close_OnClick(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}