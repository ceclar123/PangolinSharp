using Avalonia.Controls;
using ReactiveUI;
using System.Diagnostics.CodeAnalysis;

namespace Pangolin.Desktop.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        [NotNull]
        public Window ParentWindow { get; set; }
    }
}
