using System.Runtime.InteropServices;
using Colinhas.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics;

namespace Colinhas;

/// <summary>
/// Tutorial de boas-vindas. Abre sozinho na primeira execução e fica disponível
/// no menu da bandeja ("Tutorial") para rever depois.
///
/// É uma janela separada de propósito: a janela principal é um popup estreito
/// colado no canto da tela, apertado demais para ensinar qualquer coisa.
/// </summary>
public sealed partial class WelcomeWindow : Window
{
    /// <summary>Instância única — reabrir com o tutorial aberto só traz ele para frente.</summary>
    private static WelcomeWindow? _instance;

    private readonly nint _hwnd;
    private readonly AppWindow _appWindow;
    private readonly StackPanel[] _steps;
    private readonly Ellipse[] _dots;
    private int _index;

    /// <summary>Evita que a sincronização inicial do ToggleSwitch dispare o Toggled.</summary>
    private bool _syncingStartup;

    public WelcomeWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _steps = [Step0, Step1, Step2, Step3];
        _dots = [Dot0, Dot1, Dot2, Dot3];

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.SetIcon("Assets/AppIcon.ico");

        SetupWindowStyle();
        ShowStep(0);

        _ = SyncStartupSwitch();

        // Fechar no X (sem chegar ao fim) também conta como "já viu": ninguém
        // quer o tutorial de volta a cada abertura.
        _appWindow.Closing += (_, _) =>
        {
            Settings.HasSeenWelcome = true;
            _instance = null;
        };
    }

    /// <summary>Abre o tutorial, ou traz para frente o que já está aberto.</summary>
    public static void ShowOrFocus()
    {
        if (_instance is not null)
        {
            _instance.Activate();
            SetForegroundWindow(_instance._hwnd);
            return;
        }

        _instance = new WelcomeWindow();
        _instance.Activate();
        Logger.Log("Tutorial de boas-vindas aberto");
    }

    /// <summary>Fecha o tutorial se ele estiver aberto (usado ao sair do app).</summary>
    public static void CloseIfOpen()
    {
        var open = _instance;
        _instance = null;
        if (open is null) return;

        try { open.Close(); }
        catch (Exception ex) { Logger.Log($"CloseIfOpen falhou — {ex.Message}"); }
    }

    // ---------- Janela ----------

    private void SetupWindowStyle()
    {
        var scale = GetDpiForWindow(_hwnd) / 96.0;
        var width = (int)(760 * scale);
        var height = (int)(600 * scale);
        _appWindow.Resize(new SizeInt32(width, height));

        // Tamanho fixo: o conteúdo é diagramado para essa caixa.
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        var display = DisplayArea.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd), DisplayAreaFallback.Primary);
        _appWindow.Move(new PointInt32(
            display.WorkArea.X + (display.WorkArea.Width - width) / 2,
            display.WorkArea.Y + (display.WorkArea.Height - height) / 2));
    }

    // ---------- Navegação entre passos ----------

    private void ShowStep(int index)
    {
        _index = Math.Clamp(index, 0, _steps.Length - 1);

        for (var i = 0; i < _steps.Length; i++)
            _steps[i].Visibility = i == _index ? Visibility.Visible : Visibility.Collapsed;

        var active = ThemeBrush("AccentFillColorDefaultBrush", Microsoft.UI.Colors.SlateGray);
        var idle = ThemeBrush("ControlStrongFillColorDisabledBrush", Microsoft.UI.Colors.Gray);
        for (var i = 0; i < _dots.Length; i++)
            _dots[i].Fill = i == _index ? active : idle;

        BackButton.IsEnabled = _index > 0;
        NextButton.Content = _index == _steps.Length - 1 ? "Começar a usar" : "Próximo";
    }

    /// <summary>
    /// Brush do tema pelo nome, com cor de reserva. Chave de recurso inexistente
    /// derruba o app em runtime (não na compilação), e uma bolinha do indicador
    /// não vale um crash na primeira execução.
    /// </summary>
    private static Brush ThemeBrush(string key, Windows.UI.Color fallback)
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush)
            return brush;

        Logger.Log($"WelcomeWindow: recurso '{key}' não encontrado — usando cor de reserva");
        return new SolidColorBrush(fallback);
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ShowStep(_index - 1);

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_index < _steps.Length - 1)
        {
            ShowStep(_index + 1);
            return;
        }

        Settings.HasSeenWelcome = true;
        _instance = null;
        Close();
    }

    // ---------- Iniciar com o Windows ----------

    private async Task SyncStartupSwitch()
    {
        var state = await StartupService.GetStateAsync();

        _syncingStartup = true;
        StartupSwitch.IsOn = state is Windows.ApplicationModel.StartupTaskState.Enabled
                                  or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
        _syncingStartup = false;

        if (state is Windows.ApplicationModel.StartupTaskState.DisabledByUser
                  or Windows.ApplicationModel.StartupTaskState.DisabledByPolicy)
        {
            StartupSwitch.IsEnabled = false;
            StartupHint.Text = "O Windows desativou o início automático do Colinhas. " +
                "Para religar: Gerenciador de Tarefas → Aplicativos de inicialização → Colinhas → Habilitar.";
        }
    }

    private async void StartupSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_syncingStartup) return;

        var wanted = StartupSwitch.IsOn;
        var state = await StartupService.SetEnabledAsync(wanted);
        var applied = state is Windows.ApplicationModel.StartupTaskState.Enabled
                           or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;

        if (wanted == applied) return;

        _syncingStartup = true;
        StartupSwitch.IsOn = applied;
        _syncingStartup = false;

        StartupSwitch.IsEnabled = false;
        StartupHint.Text = "O Windows não permitiu ligar o início automático. " +
            "Tente por Gerenciador de Tarefas → Aplicativos de inicialização → Colinhas → Habilitar.";
    }

    // ---------- Win32 ----------

    [DllImport("user32.dll")]
    private static extern int GetDpiForWindow(nint hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hwnd);
}
