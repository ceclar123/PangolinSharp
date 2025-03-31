using Avalonia.Controls;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        public required Window ParentWindow { get; set; }
    }
}