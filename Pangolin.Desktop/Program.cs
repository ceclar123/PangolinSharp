using Avalonia;
using ReactiveUI.Avalonia;
using System;
using System.Diagnostics;

namespace Pangolin.Desktop
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // 确保当前只有一个此应用实例在运行
            string processName = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcessesByName(processName).Length > 1)
            {
                Environment.Exit(0);
                return;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI(builder => { /* We don't need to configure anything for the samples */ })
                //.With(new X11PlatformOptions { EnableMultiTouch = true, UseDBusMenu = true })
                //.With(new Win32PlatformOptions { RenderingMode = new Win32RenderingMode[] { Win32RenderingMode.AngleEgl } })
        //.UseManagedSystemDialogs()
        ;
    }
}