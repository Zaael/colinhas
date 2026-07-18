using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Colinhas.Services;
using Colinhas.ViewModels;

namespace Colinhas;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();

        // Initialize on Loaded, not in the constructor: at construction time the
        // MainWindow is still being built and App.Window (thus the HWND) is null.
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Logger.Log("MainPage.Loaded");
        ViewModel.Initialize(App.WindowHandle);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Dispose();
    }
}
