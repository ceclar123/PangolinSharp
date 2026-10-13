using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Notification = Avalonia.Controls.Notifications.Notification;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace Pangolin.Desktop.ViewModels
{
    public class ViewModelBase : ReactiveValidationObject
    {
        public required Window ParentWindow { get; set; }

        public required WindowNotificationManager? MainNotificationManager { get; set; }
    }
}