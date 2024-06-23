using Avalonia;
using Avalonia.Platform;
using Pangolin.Desktop.Models;
using Pangolin.Desktop.Properties;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Tmds.DBus.Protocol;

namespace Pangolin.Desktop.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";


        public MainWindowViewModel()
        {

        }
    }
}
