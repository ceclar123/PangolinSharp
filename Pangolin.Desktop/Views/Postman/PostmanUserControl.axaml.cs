using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Pangolin.Desktop.Models;
using Pangolin.Desktop.Models.Postman;
using Pangolin.Desktop.ViewModels.Postman;

namespace Pangolin.Desktop.Views.Postman
{
    public partial class PostmanUserControl : UserControl
    {
        private PostmanUserControlViewModel? _viewModel;
        private Bitmap? _responseBitmap;
        private byte[]? _imageBytes;
        private bool _imageDecodeFailed;
        private int _imageVersion;
        private string? _renderedText;
        private bool _renderedHighlight;
        private static readonly IBrush KeyBrush = Brush.Parse("#92278F");
        private static readonly IBrush StringBrush = Brush.Parse("#22863A");
        private static readonly IBrush ValueBrush = Brush.Parse("#1A6BA8");
        private static readonly Regex JsonTokens = new(
            "\\\"(?:\\\\.|[^\\\"\\\\])*\\\"(?=\\s*:)|\\\"(?:\\\\.|[^\\\"\\\\])*\\\"|\\b(?:true|false|null)\\b|-?\\b\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?\\b",
            RegexOptions.Compiled, TimeSpan.FromMilliseconds(20));

        public PostmanUserControl()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => BindViewModel();
            AttachedToVisualTree += (_, _) => BindViewModel();
            DetachedFromVisualTree += (_, _) =>
            {
                if (_viewModel != null) _viewModel.PropertyChanged -= ResponseChanged;
                _viewModel = null;
                ++_imageVersion;
                _imageBytes = null;
                _renderedText = null;
                ResponseImage.Source = null;
                _responseBitmap?.Dispose();
                _responseBitmap = null;
            };
        }

        private void BindViewModel()
        {
            if (_viewModel != null) _viewModel.PropertyChanged -= ResponseChanged;
            _viewModel = DataContext as PostmanUserControlViewModel;
            if (_viewModel != null) _viewModel.PropertyChanged += ResponseChanged;
            RenderResponse();
            RenderImage();
        }

        private void ResponseChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(PostmanUserControlViewModel.ResponseText)
                    || e.PropertyName == nameof(PostmanUserControlViewModel.IsFormatting)) && _viewModel?.IsFormatting != true)
                RenderResponse();
            if (e.PropertyName == nameof(PostmanUserControlViewModel.ResponseBytes)
                || (e.PropertyName == nameof(PostmanUserControlViewModel.ResponseBodyMode)
                    && _viewModel?.ResponseBodyMode == 0 && ResponseImage.Source == null))
                RenderImage();
        }

        private async void RenderImage()
        {
            if (ResponseImage == null) return;
            var vm = _viewModel;
            var bytes = vm?.ResponseBytes;
            if (ReferenceEquals(bytes, _imageBytes))
            {
                if (vm?.IsImagePreview == true && _imageDecodeFailed)
                    vm.ResponseBodyMode = 1;
                return;
            }
            var version = ++_imageVersion;
            _imageBytes = bytes;
            _imageDecodeFailed = false;
            ResponseImage.Source = null;
            _responseBitmap?.Dispose();
            _responseBitmap = null;
            if (vm == null || !vm.ResponseContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                var bitmap = await Task.Run(() =>
                {
                    using var stream = new MemoryStream(bytes!, writable: false);
                    return Bitmap.DecodeToWidth(stream, 1600);
                });
                if (version != _imageVersion || !ReferenceEquals(vm, _viewModel)) { bitmap.Dispose(); return; }
                _responseBitmap = bitmap;
                ResponseImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                if (version != _imageVersion || !ReferenceEquals(vm, _viewModel)) return;
                _imageDecodeFailed = true;
                vm.ResponseBodyMode = 1;
                // Wait for the hex view so its notice does not overwrite the decoding error.
                await vm.ResponseFormattingTask;
                if (version == _imageVersion)
                    vm.ResponseMessage = $"Image preview is unavailable: {ex.Message}. Use Save to download the original file.";
            }
        }

        private void CommitRequestEdits(object? sender, RoutedEventArgs e)
        {
            foreach (var grid in new[] { QueryGrid, HeadersGrid, FormGrid })
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        private void DeleteParameter_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button || _viewModel == null) return;
            CommitRequestEdits(sender, e);
            if (button.DataContext is FormDataField field && button.Tag as string == "Form")
                _viewModel.FormDataParams.Remove(field);
            else if (button.DataContext is KeyValueDto<string, string> row)
            {
                if (button.Tag as string == "Query") _viewModel.QueryParams.Remove(row);
                if (button.Tag as string == "Headers") _viewModel.Headers.Remove(row);
            }
        }

        private async void ChooseUploadFile_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: FormDataField field }) return;
            try
            {
                var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
                if (storage == null) return;
                var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Choose upload file", AllowMultiple = false });
                if (files.Count == 0) return;
                var path = files[0].TryGetLocalPath();
                if (path == null) throw new IOException("Choose a local file for upload.");
                field.Value = path;
                field.HistoryFileName = Path.GetFileName(path);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private async void DeleteHistoryDetail_Click(object? sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is not Button { Tag: long id } || _viewModel == null) return;
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;
            var confirmation = new ConfirmationDialog(
                "Remove history detail",
                "This removes the selected request detail from History. The request and its collection links will remain.");
            if (!await confirmation.ShowDialog<bool>(owner)) return;
            await _viewModel.DeleteHistoryDetailAsync(id);
        }

        private async void DeleteRequestHistory_Click(object? sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is not Button { Tag: long id } || _viewModel == null) return;
            if (TopLevel.GetTopLevel(this) is not Window owner) return;
            var confirmation = new ConfirmationDialog(
                "Delete request",
                "This deletes the request, its detail, and all collection links. This action cannot be undone.");
            if (await confirmation.ShowDialog<bool>(owner))
                await _viewModel.DeleteRequestHistoryAsync(id);
        }

        private async void HistoryList_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_viewModel == null || _viewModel.SelectedHistoryItem == null) return;
            e.Handled = true;
            await _viewModel.OpenSelectedHistoryAsync();
            if (string.IsNullOrEmpty(_viewModel.HistoryError))
                RequestEditorTabs.SelectedIndex = 2;
        }

        private async void CreateRootCollection_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null || TopLevel.GetTopLevel(this) is not Window owner) return;
            var dialog = new CollectionNameDialog("Create collection");
            var name = await dialog.ShowDialog<string?>(owner);
            if (!string.IsNullOrWhiteSpace(name)) await _viewModel.CreateCollectionAsync(name, null);
        }

        private void CollectionContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is not ContextMenu menu || menu.PlacementTarget?.DataContext is not RequestCollectionNode node || _viewModel == null) return;
            _viewModel.SelectedCollectionNode = node;
            ConfigureCollectionMenu(menu, node);
        }

        private void CollectionContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (sender is not Control { DataContext: RequestCollectionNode collection } control) return;
            _viewModel?.SelectedCollectionNode = collection;
            if (control.ContextMenu is ContextMenu menu) ConfigureCollectionMenu(menu, collection);
        }

        private static void ConfigureCollectionMenu(ContextMenu menu, RequestCollectionNode node)
        {
            if (menu.Items.Count > 0 && menu.Items[0] is MenuItem createChild)
                createChild.IsEnabled = node.Depth < 2;
            if (menu.Items.Count > 1 && menu.Items[1] is MenuItem rename)
                rename.IsEnabled = node.Id != 0;
            if (menu.Items.Count > 3 && menu.Items[3] is MenuItem delete)
                delete.IsEnabled = node.Id != 0;
        }

        private async void CreateChildCollection_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedCollectionNode is not { } parent || TopLevel.GetTopLevel(this) is not Window owner) return;
            var dialog = new CollectionNameDialog("Create child collection");
            var name = await dialog.ShowDialog<string?>(owner);
            if (!string.IsNullOrWhiteSpace(name)) await _viewModel.CreateCollectionAsync(name, parent.Id);
        }

        private async void RenameCollection_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedCollectionNode is not { Id: > 0 } collection || TopLevel.GetTopLevel(this) is not Window owner) return;
            var dialog = new CollectionNameDialog("Rename collection", collection.Name);
            var name = await dialog.ShowDialog<string?>(owner);
            if (!string.IsNullOrWhiteSpace(name)) await _viewModel.RenameCollectionAsync(collection.Id, name);
        }

        private async void DeleteCollection_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedCollectionNode is not { Id: > 0 } collection || TopLevel.GetTopLevel(this) is not Window owner) return;
            var confirmation = new ConfirmationDialog(
                "Delete collection",
                $"Delete '{collection.Path}'? Its requests will remain in history. Requests without another collection will return to Default, and child collections will move up one level.");
            if (await confirmation.ShowDialog<bool>(owner))
                await _viewModel.DeleteCollectionAsync(collection.Id);
        }

        private async void ManageCollectionMembership_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null || TopLevel.GetTopLevel(this) is not Window owner) return;
            var historyId = sender is Button { Tag: long id } ? id : _viewModel.SelectedHistoryItem?.Id;
            if (!historyId.HasValue) return;
            if (_viewModel.AllCollectionNodes.Count == 0)
            {
                _viewModel.ReportHistoryError("Create a collection before assigning requests to it.");
                return;
            }

            var selected = await _viewModel.GetHistoryCollectionIdsAsync(historyId.Value);
            var picker = new CollectionMembershipDialog(_viewModel.AllCollectionNodes, selected);
            if (await picker.ShowDialog<bool>(owner))
                await _viewModel.SetHistoryCollectionsAsync(historyId.Value, picker.SelectedCollectionIds);
        }

        private async void AddHistoryToCollection_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedCollectionNode is not { Id: > 0 } collection || TopLevel.GetTopLevel(this) is not Window owner) return;
            var history = await _viewModel.GetCollectionPickerHistoryAsync();
            var selected = await _viewModel.GetCollectionHistoryAsync(collection.Id);
            var picker = new HistoryPickerDialog(collection.Path, history, selected.Select(item => item.Id));
            if (await picker.ShowDialog<bool>(owner))
                await _viewModel.SetCollectionHistoryMembersAsync(collection.Id, picker.SelectedHistoryIds);
        }

        private void RenderResponse()
        {
            if (ResponseEditor == null || ResponseLineNumbers == null) return;
            var text = _viewModel?.ResponseText ?? string.Empty;
            var highlight = _viewModel?.ResponseBodyMode == 0 && _viewModel.ResponseFormat == "JSON" && text.Length <= 12_000;
            if (text == _renderedText && highlight == _renderedHighlight) return;
            _renderedText = text;
            _renderedHighlight = highlight;
            ResponseScroll.Offset = default;
            ResponseLineNumbers.Text = string.Join("\n", Enumerable.Range(1, text.Count(c => c == '\n') + 1));
            ResponseEditor.Inlines?.Clear();
            ResponseEditor.Text = string.Empty;
            if (!highlight)
            {
                ResponseEditor.Text = text;
                return;
            }
            try
            {
                var matches = JsonTokens.Matches(text);
                // Thousands of Run objects cause costly text layout even with a small JSON body.
                if (matches.Count > 200) { ResponseEditor.Text = text; return; }
                var inlines = new List<Inline>();
                var position = 0;
                foreach (Match match in matches)
                {
                    if (match.Index > position) inlines.Add(new Run(text[position..match.Index]));
                    var token = match.Value;
                    var next = match.Index + match.Length;
                    while (next < text.Length && char.IsWhiteSpace(text[next])) next++;
                    var isKey = token.StartsWith('"') && next < text.Length && text[next] == ':';
                    var brush = token.StartsWith('"') ? (isKey ? KeyBrush : StringBrush) : ValueBrush;
                    inlines.Add(new Run(token) { Foreground = brush });
                    position = match.Index + match.Length;
                }
                if (position < text.Length) inlines.Add(new Run(text[position..]));
                ResponseEditor.Inlines!.AddRange(inlines);
            }
            catch (RegexMatchTimeoutException) { ResponseEditor.Text = text; }
        }

        private void WrapResponse_Changed(object? sender, RoutedEventArgs e)
        {
            if (ResponseEditor == null || ResponseScroll == null) return;
            var wrap = WrapResponse.IsChecked == true;
            ResponseEditor.TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ResponseScroll.HorizontalScrollBarVisibility = wrap
                ? Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
                : Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
            // Wrapped visual lines have variable heights; hide the source line gutter in this mode.
            ResponseLineNumbers.IsVisible = !wrap;
        }

        private async void CopyResponse_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null && _viewModel != null)
                    await clipboard.SetTextAsync(_viewModel.ResponseText);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private async void SaveResponse_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            var body = _viewModel.ResponseBytes;
            var extension = _viewModel.ResponseContentType.ToLowerInvariant() switch
            {
                "image/png" => "png", "image/jpeg" => "jpg", "image/gif" => "gif",
                "image/webp" => "webp", "image/svg+xml" => "svg", "application/pdf" => "pdf",
                _ => _viewModel.ResponseFormat switch { "JSON" => "json", "XML" => "xml", "HTML" => "html", "Binary" => "bin", _ => "txt" }
            };
            try
            {
                var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
                if (storage == null) return;
                var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save response body",
                    SuggestedFileName = $"response.{extension}",
                    DefaultExtension = extension,
                    FileTypeChoices = new[] { new FilePickerFileType("Response body") { Patterns = new[] { $"*.{extension}" } }, FilePickerFileTypes.All }
                });
                if (file == null) return;
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                await stream.WriteAsync(body);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void ShowError(Exception ex)
        {
            if (_viewModel != null) _viewModel.ResponseMessage = ex.Message;
            _viewModel?.MainNotificationManager?.Show(new Avalonia.Controls.Notifications.Notification(
                "Response", ex.Message, Avalonia.Controls.Notifications.NotificationType.Error));
        }
    }
}
