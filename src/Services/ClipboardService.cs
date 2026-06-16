using Windows.ApplicationModel.DataTransfer;

namespace Colinhas.Services;

public sealed class ClipboardService : IDisposable
{
    private readonly Action<string> _onNewText;
    private string? _lastSetText;

    public ClipboardService(Action<string> onNewText)
    {
        _onNewText = onNewText;
        Clipboard.ContentChanged += OnContentChanged;
    }

    private async void OnContentChanged(object? sender, object e)
    {
        try
        {
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text)) return;

            var text = await content.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Ignore changes that we triggered ourselves (via SetText)
            if (text == _lastSetText)
            {
                _lastSetText = null;
                return;
            }

            _onNewText(text);
        }
        catch { }
    }

    public void SetText(string text)
    {
        _lastSetText = text;
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    public void Dispose() =>
        Clipboard.ContentChanged -= OnContentChanged;
}
