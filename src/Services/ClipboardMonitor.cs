using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace Colinhas.Services;

/// <summary>
/// Monitors the Windows clipboard system-wide using the Win32
/// <c>AddClipboardFormatListener</c> API. Unlike WinRT's
/// <c>Clipboard.ContentChanged</c>, this fires for clipboard changes from ANY
/// application, even when our window is not focused — which is exactly what a
/// Win+V style clipboard manager needs.
///
/// It works by subclassing our own window's message loop (WndProc) to receive
/// the <c>WM_CLIPBOARDUPDATE</c> message.
/// </summary>
public sealed class ClipboardMonitor : IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    private const uint SubclassId = 1;

    private readonly nint _hwnd;
    // Keep the delegate alive for the whole lifetime — if the GC collects it,
    // Windows calls into freed memory and the app crashes.
    private readonly SubclassProcDelegate _subclassProc;
    private string? _lastText;
    private bool _disposed;

    /// <summary>Raised on the UI thread whenever new text lands on the clipboard.</summary>
    public event Action<string>? TextCaptured;

    public ClipboardMonitor(nint hwnd)
    {
        _hwnd = hwnd;
        _subclassProc = WndProc;

        if (!SetWindowSubclass(_hwnd, _subclassProc, SubclassId, 0))
            Logger.Log("ClipboardMonitor: SetWindowSubclass FAILED");

        if (!AddClipboardFormatListener(_hwnd))
            Logger.Log($"ClipboardMonitor: AddClipboardFormatListener FAILED (err {Marshal.GetLastWin32Error()})");
        else
            Logger.Log($"ClipboardMonitor: listening on hwnd 0x{_hwnd:X}");
    }

    private nint WndProc(nint hWnd, uint uMsg, nint wParam, nint lParam, nint uIdSubclass, nint dwRefData)
    {
        if (uMsg == WM_CLIPBOARDUPDATE)
        {
            Logger.Log("WM_CLIPBOARDUPDATE received");
            _ = CaptureAsync();
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private async Task CaptureAsync()
    {
        try
        {
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text))
            {
                Logger.Log("Clipboard change ignored: not text");
                return;
            }

            var text = await content.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Ignore the change we caused ourselves via SetText.
            if (text == _lastText)
            {
                Logger.Log("Clipboard change ignored: same as last (self-copy)");
                return;
            }

            _lastText = text;
            Logger.Log($"Captured {text.Length} chars");
            TextCaptured?.Invoke(text);
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureAsync error: {ex.Message}");
        }
    }

    /// <summary>Puts text on the clipboard, remembering it so we don't re-capture it.</summary>
    public void SetText(string text)
    {
        _lastText = text;
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        RemoveClipboardFormatListener(_hwnd);
        RemoveWindowSubclass(_hwnd, _subclassProc, SubclassId);
    }

    private delegate nint SubclassProcDelegate(
        nint hWnd, uint uMsg, nint wParam, nint lParam, nint uIdSubclass, nint dwRefData);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(nint hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(nint hwnd);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(
        nint hWnd, SubclassProcDelegate pfnSubclass, uint uIdSubclass, nint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(
        nint hWnd, SubclassProcDelegate pfnSubclass, uint uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);
}
