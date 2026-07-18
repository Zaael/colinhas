using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Colinhas.Services;
using Windows.Graphics;

namespace Colinhas;

public sealed partial class MainWindow : Window
{
    private const uint VK_OEM_5 = 0xDC;   // "\|" above Enter (US layouts)
    private const uint VK_OEM_102 = 0xE2; // "\|" between LShift and Z (ABNT2)

    private readonly nint _hwnd;
    private readonly AppWindow _appWindow;
    private GlobalHotkeys? _hotkeys;
    private TaskbarIcon? _trayIcon;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");
        RootFrame.Navigate(typeof(MainPage));

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Tray-only app: no taskbar / Alt-Tab entry — it lives in the background.
        _appWindow.IsShownInSwitchers = false;

        SetupWindowStyle();
        SetupTray();

        // Global hotkeys: expose the registry app-wide and register Ctrl+\ (both
        // key codes so it works on US and ABNT2 layouts) to toggle the window.
        _hotkeys = new GlobalHotkeys(_hwnd);
        App.Hotkeys = _hotkeys;
        _hotkeys.Register(0xC01, GlobalHotkeys.MOD_CONTROL, VK_OEM_5, ToggleVisibility);
        _hotkeys.Register(0xC02, GlobalHotkeys.MOD_CONTROL, VK_OEM_102, ToggleVisibility);

        _appWindow.Closing += OnClosing;
    }

    // ---------- Window sizing / positioning ----------

    private void SetupWindowStyle()
    {
        var scale = GetDpiForWindow(_hwnd) / 96.0;
        _appWindow.Resize(new SizeInt32((int)(420 * scale), (int)(580 * scale)));
        PositionBottomRight(scale);
    }

    private void PositionBottomRight(double scale)
    {
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
        var display = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var x = display.WorkArea.X + display.WorkArea.Width - (int)(440 * scale);
        var y = display.WorkArea.Y + display.WorkArea.Height - (int)(600 * scale);
        _appWindow.Move(new PointInt32(x, y));
    }

    // ---------- System tray ----------

    private void SetupTray()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Colinhas — Ctrl+\\ para abrir",
            IconSource = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.ico")),
            LeftClickCommand = new RelayCommand(ShowAndFocus),
            NoLeftClickDelay = true,
        };

        var menu = new MenuFlyout();

        var open = new MenuFlyoutItem { Text = "Abrir" };
        open.Click += (_, _) => ShowAndFocus();

        var exit = new MenuFlyoutItem { Text = "Sair" };
        exit.Click += (_, _) => ExitApp();

        menu.Items.Add(open);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exit);
        _trayIcon.ContextFlyout = menu;

        _trayIcon.ForceCreate();
        Logger.Log("Tray icon created");
    }

    // ---------- Show / hide ----------

    private void ToggleVisibility()
    {
        if (_appWindow.IsVisible && GetForegroundWindow() == _hwnd)
            HideToTray();
        else
            ShowAndFocus();
    }

    /// <summary>Brings the window to the foreground (used by hotkeys/tray).</summary>
    public void ShowAndFocus()
    {
        PositionBottomRight(GetDpiForWindow(_hwnd) / 96.0);
        _appWindow.Show();
        SetForegroundWindow(_hwnd);
        Activate();
    }

    private void HideToTray() => _appWindow.Hide();

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExiting) return;

        // Clicking the window's X hides to tray instead of quitting.
        args.Cancel = true;
        HideToTray();
    }

    private void ExitApp()
    {
        _isExiting = true;
        _hotkeys?.Dispose();
        _trayIcon?.Dispose();
        Application.Current.Exit();
    }

    // ---------- Win32 ----------

    [DllImport("user32.dll")]
    private static extern int GetDpiForWindow(nint hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hwnd);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}
