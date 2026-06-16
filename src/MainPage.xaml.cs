using Microsoft.UI.Xaml.Controls;
using Colinhas.ViewModels;

namespace Colinhas;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        ViewModel.Initialize();
    }
}
