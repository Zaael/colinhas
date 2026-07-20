using System.Collections.Specialized;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Colinhas.Models;
using Colinhas.Services;
using Colinhas.ViewModels;
using Windows.System;
using Windows.UI.Core;

namespace Colinhas;

public sealed partial class MainPage : Page
{
    private const int TemplateHotkeyBaseId = 0x1000;

    public MainPageViewModel ViewModel { get; } = new();
    public TemplatesViewModel TemplatesVM { get; } = new();

    private readonly List<int> _templateHotkeyIds = [];

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

        // Wire the "edit label" action on history entries as they arrive
        // (subscribe before Initialize so Win+V history items get wired too).
        ViewModel.Items.CollectionChanged += HistoryItems_CollectionChanged;
        ViewModel.Initialize(App.WindowHandle);

        // Wire template action buttons as templates come in, then load them.
        TemplatesVM.Templates.CollectionChanged += Templates_CollectionChanged;
        TemplatesVM.Load();

        // Register the global hotkeys for templates that have one.
        SyncTemplateHotkeys();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => ViewModel.Dispose();

    // ---------- History ----------

    private void HistoryItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null) return;
        foreach (ClipboardEntry entry in e.NewItems)
            entry.OnEditLabel = c => _ = EditLabelAsync(c);
    }

    private void HistoryList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipboardEntry entry)
        {
            ViewModel.CopyEntry(entry);
            TryPasteIntoPrevious();
        }
    }

    /// <summary>If "paste directly" is on, sends the copied text into the previous app.</summary>
    private void TryPasteIntoPrevious()
    {
        if (Settings.PasteDirectly)
            (App.Window as MainWindow)?.PasteIntoPrevious();
    }

    private async Task EditLabelAsync(ClipboardEntry entry)
    {
        var box = new TextBox
        {
            Header = "Descrição",
            Text = entry.Label,
            PlaceholderText = "Ex: API token access",
            AcceptsReturn = false,
        };

        var dialog = new ContentDialog
        {
            Title = "Descrição do item",
            Content = box,
            PrimaryButtonText = "Salvar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            entry.Label = box.Text.Trim();
    }

    // ---------- Tab switching ----------

    private void ViewSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var showTemplates = sender.SelectedItem == TabTemplates;
        HistoryPanel.Visibility = showTemplates ? Visibility.Collapsed : Visibility.Visible;
        TemplatesPanel.Visibility = showTemplates ? Visibility.Visible : Visibility.Collapsed;
        HistoryFooter.Visibility = showTemplates ? Visibility.Collapsed : Visibility.Visible;
    }

    // ---------- Template wiring ----------

    private void Templates_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null) return;
        foreach (TextTemplate template in e.NewItems)
            Wire(template);
    }

    private void Wire(TextTemplate template)
    {
        template.OnUse = t => _ = UseTemplateAsync(t);
        template.OnEdit = t => _ = EditTemplateAsync(t);
        template.OnDelete = t => _ = DeleteTemplateAsync(t);
    }

    // ---------- Template actions ----------

    private void AddTemplate_Click(object sender, RoutedEventArgs e) => _ = EditTemplateAsync(null);

    private async void BuyMeCoffee_Click(object sender, RoutedEventArgs e)
        => await Windows.System.Launcher.LaunchUriAsync(new Uri("https://buymeacoffee.com/zaael"));

    private async Task UseTemplateAsync(TextTemplate template)
    {
        // No placeholders? Copy straight away.
        if (!template.HasPlaceholders)
        {
            ViewModel.CopyToClipboard(template.Content);
            TryPasteIntoPrevious();
            return;
        }

        var boxes = new Dictionary<string, TextBox>();
        var panel = new StackPanel { Spacing = 10 };
        foreach (var name in template.Placeholders)
        {
            var box = new TextBox { Header = name, PlaceholderText = $"Valor para {{{name}}}" };
            boxes[name] = box;
            panel.Children.Add(box);
        }

        var dialog = new ContentDialog
        {
            Title = "Preencher campos",
            Content = new ScrollViewer { Content = panel },
            PrimaryButtonText = "Copiar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var values = boxes.ToDictionary(kv => kv.Key, kv => kv.Value.Text);
        var filled = TemplateEngine.Fill(template.Content, values);
        ViewModel.CopyToClipboard(filled);
        TryPasteIntoPrevious();
    }

    private async Task EditTemplateAsync(TextTemplate? existing)
    {
        var titleBox = new TextBox
        {
            Header = "Título",
            Text = existing?.Title ?? string.Empty,
            PlaceholderText = "Ex: Saudação",
        };
        var contentBox = new TextBox
        {
            Header = "Conteúdo",
            Text = existing?.Content ?? string.Empty,
            PlaceholderText = "Use {campos} para valores variáveis",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 140,
        };
        var hint = new TextBlock
        {
            Text = "Dica: escreva {nome}, {data}, etc. — serão pedidos ao usar o template.",
            Opacity = 0.6,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        };

        // Global hotkey capture (optional). Local state, updated by the key handler.
        var hotkeyMods = existing?.HotkeyModifiers ?? 0;
        var hotkeyKey = existing?.HotkeyKey ?? 0;

        var hotkeyBox = new TextBox
        {
            Header = "Atalho global (opcional)",
            Text = hotkeyKey != 0 ? HotkeyFormatter.Format(hotkeyMods, hotkeyKey) : string.Empty,
            PlaceholderText = "Clique aqui e pressione as teclas (ex: Ctrl+Alt+1)",
            IsReadOnly = true,
        };
        hotkeyBox.KeyDown += (_, e) =>
        {
            e.Handled = true;
            var vk = e.Key;
            if (vk is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu
                or VirtualKey.LeftWindows or VirtualKey.RightWindows)
                return; // wait for a non-modifier key

            var mods = 0;
            if (IsDown(VirtualKey.Control)) mods |= (int)GlobalHotkeys.MOD_CONTROL;
            if (IsDown(VirtualKey.Menu)) mods |= (int)GlobalHotkeys.MOD_ALT;
            if (IsDown(VirtualKey.Shift)) mods |= (int)GlobalHotkeys.MOD_SHIFT;
            if (mods == 0) return; // require at least one modifier

            hotkeyMods = mods;
            hotkeyKey = (int)vk;
            hotkeyBox.Text = HotkeyFormatter.Format(mods, (int)vk);
        };

        var clearHotkey = new HyperlinkButton { Content = "Limpar atalho" };
        clearHotkey.Click += (_, _) =>
        {
            hotkeyMods = 0;
            hotkeyKey = 0;
            hotkeyBox.Text = string.Empty;
        };

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(titleBox);
        panel.Children.Add(contentBox);
        panel.Children.Add(hint);
        panel.Children.Add(hotkeyBox);
        panel.Children.Add(clearHotkey);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Novo template" : "Editar template",
            Content = new ScrollViewer { Content = panel },
            PrimaryButtonText = "Salvar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var title = string.IsNullOrWhiteSpace(titleBox.Text) ? "Sem título" : titleBox.Text.Trim();

        if (existing is null)
        {
            TemplatesVM.Add(new TextTemplate
            {
                Title = title,
                Content = contentBox.Text,
                HotkeyModifiers = hotkeyMods,
                HotkeyKey = hotkeyKey,
            });
        }
        else
        {
            existing.Title = title;
            existing.Content = contentBox.Text;
            existing.HotkeyModifiers = hotkeyMods;
            existing.HotkeyKey = hotkeyKey;
            TemplatesVM.Persist();
        }

        SyncTemplateHotkeys();
    }

    private async Task DeleteTemplateAsync(TextTemplate template)
    {
        var dialog = new ContentDialog
        {
            Title = "Excluir template",
            Content = $"Excluir \"{template.Title}\"?",
            PrimaryButtonText = "Excluir",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            TemplatesVM.Delete(template);
            SyncTemplateHotkeys();
        }
    }

    // ---------- Template global hotkeys ----------

    /// <summary>Re-registers all template hotkeys to match the current templates.</summary>
    private void SyncTemplateHotkeys()
    {
        if (App.Hotkeys is null) return;

        foreach (var id in _templateHotkeyIds)
            App.Hotkeys.Unregister(id);
        _templateHotkeyIds.Clear();

        var id2 = TemplateHotkeyBaseId;
        foreach (var template in TemplatesVM.Templates)
        {
            if (!template.HasHotkey) continue;

            var captured = template;
            var thisId = id2++;
            var ok = App.Hotkeys.Register(
                thisId,
                (uint)captured.HotkeyModifiers,
                (uint)captured.HotkeyKey,
                () => App.DispatcherQueue.TryEnqueue(() => OnTemplateHotkey(captured)));

            if (ok)
                _templateHotkeyIds.Add(thisId);
            else
                Logger.Log($"Template hotkey conflito: '{captured.Title}' {captured.HotkeyLabel}");
        }
    }

    private void OnTemplateHotkey(TextTemplate template)
    {
        // Placeholders need the fill dialog, so bring the window forward first.
        if (template.HasPlaceholders)
            (App.Window as MainWindow)?.ShowAndFocus();

        _ = UseTemplateAsync(template);
    }

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}
