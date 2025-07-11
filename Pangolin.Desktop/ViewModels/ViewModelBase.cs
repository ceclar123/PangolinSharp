using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace Pangolin.Desktop.ViewModels
{
    public class ViewModelBase : ReactiveValidationObject
    {
        public required Window ParentWindow { get; set; }
    }
}