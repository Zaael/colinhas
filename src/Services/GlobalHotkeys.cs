using System.Runtime.InteropServices;

namespace Colinhas.Services;

/// <summary>
/// Central registry for global hotkeys via the Win32 <c>RegisterHotKey</c> API.
/// Subclasses the window once and dispatches <c>WM_HOTKEY</c> messages to the
/// callback registered for each hotkey id. Works system-wide, even in background.
/// </summary>
public sealed class GlobalHotkeys : IDisposable
{
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int WM_HOTKEY = 0x0312;
    private const uint SubclassId = 2;

    private readonly nint _hwnd;
    private readonly SubclassProcDelegate _subclassProc; // kept alive against GC
    private readonly Dictionary<int, Action> _handlers = [];
    private bool _disposed;

    public GlobalHotkeys(nint hwnd)
    {
        _hwnd = hwnd;
        _subclassProc = WndProc;
        SetWindowSubclass(_hwnd, _subclassProc, SubclassId, 0);
    }

    /// <summary>
    /// Registers (or re-registers) a hotkey. Returns false if the combo is already
    /// taken by another app or is otherwise rejected by Windows.
    /// </summary>
    public bool Register(int id, uint modifiers, uint virtualKey, Action callback)
    {
        // Drop any previous registration for this id first.
        Unregister(id);

        if (!RegisterHotKey(_hwnd, id, modifiers | MOD_NOREPEAT, virtualKey))
        {
            Logger.Log($"GlobalHotkeys: register id {id} FAILED (err {Marshal.GetLastWin32Error()})");
            return false;
        }

        _handlers[id] = callback;
        return true;
    }

    public void Unregister(int id)
    {
        if (_handlers.Remove(id))
            UnregisterHotKey(_hwnd, id);
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam, nint id, nint data)
    {
        if (msg == WM_HOTKEY && _handlers.TryGetValue((int)wParam, out var callback))
        {
            Logger.Log($"WM_HOTKEY id 0x{(int)wParam:X}");

            // Remember the app that was focused when the hotkey fired, so "paste
            // directly" can send the text back into it.
            var foreground = GetForegroundWindow();
            if (foreground != 0 && foreground != _hwnd)
                App.PreviousForeground = foreground;

            callback();
        }

        return DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var id in _handlers.Keys)
            UnregisterHotKey(_hwnd, id);
        _handlers.Clear();

        RemoveWindowSubclass(_hwnd, _subclassProc, SubclassId);
    }

    private delegate nint SubclassProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam, nint id, nint data);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(nint hWnd, SubclassProcDelegate pfnSubclass, uint uIdSubclass, nint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(nint hWnd, SubclassProcDelegate pfnSubclass, uint uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}
