using System.Runtime.InteropServices;

namespace Colinhas.Services;

/// <summary>
/// Registers a global hotkey (Ctrl+\) via the Win32 <c>RegisterHotKey</c> API and
/// raises a callback when it's pressed. Works system-wide, even when our window is
/// hidden in the background. Listens for <c>WM_HOTKEY</c> by subclassing the window.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint SubclassId = 2;

    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;

    // The "\" key sends a different virtual-key depending on the keyboard layout,
    // so we register Ctrl + each candidate. All map to the same toggle action.
    private static readonly (int Id, uint Vk, string Name)[] Candidates =
    [
        (0xC01, 0xDC, "VK_OEM_5"),    // "\|" above Enter — US / most layouts
        (0xC02, 0xE2, "VK_OEM_102"),  // "\|" between LShift and Z — ABNT2 (Brazilian)
    ];

    private readonly nint _hwnd;
    private readonly SubclassProcDelegate _subclassProc; // kept alive against GC
    private readonly Action _onHotkey;
    private bool _disposed;

    public HotkeyManager(nint hwnd, Action onHotkey)
    {
        _hwnd = hwnd;
        _onHotkey = onHotkey;
        _subclassProc = WndProc;

        SetWindowSubclass(_hwnd, _subclassProc, SubclassId, 0);

        foreach (var (id, vk, name) in Candidates)
        {
            if (RegisterHotKey(_hwnd, id, MOD_CONTROL | MOD_NOREPEAT, vk))
                Logger.Log($"HotkeyManager: Ctrl+\\ registered ({name})");
            else
                Logger.Log($"HotkeyManager: RegisterHotKey {name} FAILED (err {Marshal.GetLastWin32Error()})");
        }
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam, nint id, nint data)
    {
        if (msg == WM_HOTKEY && Candidates.Any(c => c.Id == (int)wParam))
        {
            Logger.Log($"WM_HOTKEY pressed (id 0x{(int)wParam:X})");
            _onHotkey();
        }

        return DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var (id, _, _) in Candidates)
            UnregisterHotKey(_hwnd, id);
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
}
