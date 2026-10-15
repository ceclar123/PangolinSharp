using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Pangolin.SQLite.Models;
using Pangolin.Desktop.ViewModels.Postman;

namespace Pangolin.Desktop.Views.Postman;

internal sealed class CollectionNameDialog : Window
{
    private readonly TextBox _nameInput;

    public CollectionNameDialog(string title, string initialName = "")
    {
        Title = title;
        Width = 380;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        _nameInput = new TextBox { Text = initialName, PlaceholderText = "Collection name", MinWidth = 300 };

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = "Cancel", Classes = { "clear" } };
        cancel.Click += (_, _) => Close(null);
        var save = new Button { Content = "Save", MinWidth = 72 };
        save.Click += (_, _) => Close(_nameInput.Text?.Trim());
        actions.Children.Add(cancel);
        actions.Children.Add(save);

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children = { new TextBlock { Text = "Name" }, _nameInput, actions }
        };
        Opened += (_, _) => _nameInput.Focus();
    }
}

internal sealed class ConfirmationDialog : Window
{
    public ConfirmationDialog(string title, string message, string confirmLabel = "Delete")
    {
        Title = title;
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = "Cancel", Classes = { "clear" } };
        cancel.Click += (_, _) => Close(false);
        var confirm = new Button { Content = confirmLabel, MinWidth = 72 };
        confirm.Click += (_, _) => Close(true);
        actions.Children.Add(cancel);
        actions.Children.Add(confirm);

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 18,
            Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 380 }, actions }
        };
    }
}

internal sealed class CollectionMembershipDialog : Window
{
    private readonly Dictionary<long, CheckBox> _choices = new();
    public IReadOnlyList<long> SelectedCollectionIds => _choices.Where(pair => pair.Value.IsChecked == true).Select(pair => pair.Key).ToList();

    public CollectionMembershipDialog(IEnumerable<RequestCollectionNode> collections, IEnumerable<long> selectedIds)
    {
        Title = "Add request to collections";
        Width = 460;
        Height = 560;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var selected = selectedIds.ToHashSet();
        var choices = new StackPanel { Spacing = 5, Margin = new Thickness(8) };
        foreach (var collection in collections.OrderBy(item => item.Path, System.StringComparer.OrdinalIgnoreCase))
        {
            var option = new CheckBox
            {
                Content = collection.Path,
                IsChecked = selected.Contains(collection.Id),
                Tag = collection.Id
            };
            _choices[collection.Id] = option;
            choices.Children.Add(option);
        }

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(8) };
        var cancel = new Button { Content = "Cancel", Classes = { "clear" } };
        cancel.Click += (_, _) => Close(false);
        var save = new Button { Content = "Save", MinWidth = 72 };
        save.Click += (_, _) => Close(true);
        actions.Children.Add(cancel);
        actions.Children.Add(save);

        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Children =
            {
                new ScrollViewer { Content = choices, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto },
                actions
            }
        };
        Grid.SetRow(actions, 1);
    }
}

internal sealed class HistoryPickerOption
{
    public required RequestHistorySummary History { get; init; }
    public bool IsSelected { get; set; }
}

internal sealed class HistoryPickerDialog : Window
{
    private readonly List<HistoryPickerOption> _options;
    public IReadOnlyList<long> SelectedHistoryIds => _options.Where(option => option.IsSelected).Select(option => option.History.Id).ToList();

    public HistoryPickerDialog(string collectionPath, IEnumerable<RequestHistorySummary> history, IEnumerable<long> collectionMemberIds)
    {
        Title = $"Add requests to {collectionPath}";
        Width = 520;
        Height = 620;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var selected = collectionMemberIds.ToHashSet();
        _options = history.Select(item => new HistoryPickerOption { History = item, IsSelected = selected.Contains(item.Id) }).ToList();
        var list = new ListBox
        {
            ItemsSource = _options,
            ItemTemplate = new FuncDataTemplate<HistoryPickerOption>((option, _) =>
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowDefinitions = new RowDefinitions("Auto,Auto"), Margin = new Thickness(4, 5) };
                row.Children.Add(new TextBlock { Text = option.History.Method, VerticalAlignment = VerticalAlignment.Center, Foreground = Avalonia.Media.Brushes.SeaGreen, Margin = new Thickness(0, 0, 8, 0) });
                var detail = new StackPanel { Spacing = 2 };
                detail.Children.Add(new TextBlock { Text = option.History.Url, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis });
                detail.Children.Add(new TextBlock { Text = option.History.DisplayTime, FontSize = 10, Foreground = Avalonia.Media.Brushes.Gray });
                Grid.SetColumn(detail, 1);
                row.Children.Add(detail);
                var check = new CheckBox { Content = row, IsChecked = option.IsSelected };
                check.IsCheckedChanged += (_, _) => option.IsSelected = check.IsChecked == true;
                return check;
            }, supportsRecycling: false)
        };

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(8) };
        var cancel = new Button { Content = "Cancel", Classes = { "clear" } };
        cancel.Click += (_, _) => Close(false);
        var save = new Button { Content = "Save", MinWidth = 72 };
        save.Click += (_, _) => Close(true);
        actions.Children.Add(cancel);
        actions.Children.Add(save);
        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Children = { list, actions }
        };
        Grid.SetRow(actions, 1);
    }
}
