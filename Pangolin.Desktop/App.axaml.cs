using Avalonia;
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
                    DataContext = new MainWindowViewModel(),
                    WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen
                };

                if (desktop.MainWindow.Screens.Primary != null)
                {
                    desktop.MainWindow.Width = desktop.MainWindow.Screens.Primary.Bounds.Width / 2;
                    desktop.MainWindow.Height = desktop.MainWindow.Screens.Primary.Bounds.Height / 2;
                }
                else
                {
                    desktop.MainWindow.Width = 800;
                    desktop.MainWindow.Height = 600;
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}