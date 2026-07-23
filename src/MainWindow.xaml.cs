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
    private System.Threading.Timer? _exitFailsafe;

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

        var pasteToggle = new ToggleMenuFlyoutItem { Text = "Colar direto", IsChecked = Settings.PasteDirectly };
        pasteToggle.Click += (s, _) => Settings.PasteDirectly = ((ToggleMenuFlyoutItem)s).IsChecked;

        // O estado real do startup mora no Windows (não em Settings), então o
        // ToggleMenuFlyoutItem nasce desmarcado e é sincronizado de forma assíncrona.
        var startupToggle = new ToggleMenuFlyoutItem { Text = "Iniciar com o Windows" };
        startupToggle.Click += async (s, _) => await OnStartupToggled((ToggleMenuFlyoutItem)s);
        _ = SyncStartupToggle(startupToggle);

        var tutorial = new MenuFlyoutItem { Text = "Tutorial" };
        tutorial.Click += (_, _) => WelcomeWindow.ShowOrFocus();

        var exit = new MenuFlyoutItem { Text = "Sair" };
        exit.Click += (_, _) =>
        {
            // Log no próprio clique: se esta linha não sair no log, o problema é o
            // clique não chegar até aqui — não o encerramento em si.
            Logger.Log("Tray: clique em Sair");
            ExitApp();
        };

        menu.Items.Add(open);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(pasteToggle);
        menu.Items.Add(startupToggle);
        menu.Items.Add(tutorial);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exit);
        // O usuário pode ligar/desligar o startup pelo Gerenciador de Tarefas a
        // qualquer momento, então revalidamos toda vez que o menu abre.
        menu.Opening += (_, _) => _ = SyncStartupToggle(startupToggle);

        _trayIcon.ContextFlyout = menu;

        _trayIcon.ForceCreate();
        Logger.Log("Tray icon created");
    }

    // ---------- Iniciar com o Windows ----------

    private static async Task SyncStartupToggle(ToggleMenuFlyoutItem item)
    {
        var state = await StartupService.GetStateAsync();
        item.IsChecked = state is Windows.ApplicationModel.StartupTaskState.Enabled
                              or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;

        // Quando o Windows tira o controle do app, o menu vira apenas informativo.
        var locked = state is Windows.ApplicationModel.StartupTaskState.DisabledByUser
                          or Windows.ApplicationModel.StartupTaskState.DisabledByPolicy
                          or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
        item.IsEnabled = !locked;
    }

    private async Task OnStartupToggled(ToggleMenuFlyoutItem item)
    {
        var wanted = item.IsChecked;
        var state = await StartupService.SetEnabledAsync(wanted);

        var applied = state is Windows.ApplicationModel.StartupTaskState.Enabled
                           or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
        item.IsChecked = applied;

        if (wanted && !applied)
        {
            item.IsEnabled = false;
            await ShowMessage(
                "Não foi possível ligar",
                "O início automático do Colinhas foi desativado pelo Windows. " +
                "Para religar, abra o Gerenciador de Tarefas → Aplicativos de inicialização, " +
                "encontre \"Colinhas\" e escolha Habilitar.");
        }
    }

    /// <summary>Mostra um aviso simples na janela principal (trazendo-a para frente).</summary>
    private async Task ShowMessage(string title, string message)
    {
        ShowAndFocus();

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Entendi",
            XamlRoot = RootFrame.XamlRoot,
        };

        try { await dialog.ShowAsync(); }
        catch (Exception ex) { Logger.Log($"ShowMessage falhou — {ex.Message}"); }
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

    /// <summary>
    /// Sobe direto para a bandeja, sem aparecer. Ativamos e escondemos em seguida
    /// (em vez de nunca ativar) para o XAML já ficar carregado — assim o primeiro
    /// Ctrl+\ abre instantâneo, sem o atraso da primeira renderização.
    /// </summary>
    public void StartHidden()
    {
        Activate();
        _appWindow.Hide();
    }

    /// <summary>Hides Colinhas and pastes (Ctrl+V) into the previously focused app.</summary>
    public void PasteIntoPrevious()
    {
        var target = App.PreviousForeground;
        App.PreviousForeground = 0; // consume it so a stale window isn't reused later
        HideToTray();
        if (target == 0) return;

        SetForegroundWindow(target);

        // Give focus a moment to settle before sending the keystroke.
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(90);
        timer.IsRepeating = false;
        timer.Tick += (s, _) =>
        {
            s.Stop();
            InputSender.SendCtrlV();
        };
        timer.Start();
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExiting) return;

        // Clicking the window's X hides to tray instead of quitting.
        args.Cancel = true;
        HideToTray();
    }

    private void ExitApp()
    {
        if (_isExiting)
        {
            Logger.Log("Sair: ignorado — já estava encerrando");
            return;
        }

        _isExiting = true;
        Logger.Log("Sair: encerrando o app");

        // Soltar os ganchos Win32 antes de sair. Eles ficam registrados no
        // Windows enquanto a janela existir: subclass do WndProc (hotkeys) e o
        // listener de clipboard. O ViewModel só era liberado no Unloaded da
        // página, que nunca dispara aqui — a janela é escondida, não fechada.
        _hotkeys?.Dispose();
        (RootFrame.Content as MainPage)?.ViewModel.Dispose();

        // O ícone precisa sumir da bandeja agora; senão fica o "fantasma" que só
        // some quando o usuário passa o mouse por cima.
        _trayIcon?.Dispose();

        // Uma segunda janela aberta (o tutorial) segura o app vivo.
        WelcomeWindow.CloseIfOpen();

        Application.Current.Exit();

        // Rede de segurança: com a janela principal escondida, o Application.Exit()
        // do WinUI nem sempre derruba o processo — o loop de mensagens continua e o
        // app fica só na lista de processos, sem ícone e sem janela. Se meio segundo
        // depois ainda estivermos vivos, sai à força. Não há nada pendente para
        // salvar: o histórico é gravado a cada alteração.
        //
        // O timer precisa ser de thread (System.Threading.Timer). A primeira versão
        // usava DispatcherQueueTimer e nunca disparava: o Application.Exit() derruba
        // justamente a fila de mensagens da UI de que esse timer depende — a rede de
        // segurança morria junto com o que deveria salvar. O campo mantém a
        // referência viva; um Timer sem referência pode ser coletado antes de tocar.
        _exitFailsafe = new System.Threading.Timer(
            _ =>
            {
                Logger.Log("Sair: Application.Exit() não encerrou — saindo à força");
                Environment.Exit(0);
            },
            null, TimeSpan.FromMilliseconds(500), System.Threading.Timeout.InfiniteTimeSpan);
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
