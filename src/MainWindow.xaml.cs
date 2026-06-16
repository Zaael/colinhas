using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Colinhas;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        RootFrame.Navigate(typeof(MainPage));

        SetupWindowStyle();
    }

    private void SetupWindowStyle()
    {
        // Popup-style size: compact and tall like Win+V
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Get DPI scale factor
        var dpi = GetDpiForWindow(hwnd);
        double scale = dpi / 96.0;

        appWindow.Resize(new SizeInt32((int)(420 * scale), (int)(580 * scale)));

        // Position: bottom-right of the primary display work area
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        int x = displayArea.WorkArea.Width - (int)(440 * scale);
        int y = displayArea.WorkArea.Height - (int)(600 * scale);
        appWindow.Move(new PointInt32(x, y));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetDpiForWindow(nint hwnd);
}
