using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Colinhas.Models;
using Colinhas.Services;
using Colinhas.ViewModels;

namespace Colinhas;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; } = new();
    public TemplatesViewModel TemplatesVM { get; } = new();

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

        // Wire template action buttons as templates come in, then load them.
        TemplatesVM.Templates.CollectionChanged += Templates_CollectionChanged;
        TemplatesVM.Load();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => ViewModel.Dispose();

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

    private async Task UseTemplateAsync(TextTemplate template)
    {
        // No placeholders? Copy straight away.
        if (!template.HasPlaceholders)
        {
            ViewModel.CopyToClipboard(template.Content);
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

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(titleBox);
        panel.Children.Add(contentBox);
        panel.Children.Add(hint);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Novo template" : "Editar template",
            Content = panel,
            PrimaryButtonText = "Salvar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var title = string.IsNullOrWhiteSpace(titleBox.Text) ? "Sem título" : titleBox.Text.Trim();

        if (existing is null)
        {
            TemplatesVM.Add(new TextTemplate { Title = title, Content = contentBox.Text });
        }
        else
        {
            existing.Title = title;
            existing.Content = contentBox.Text;
            TemplatesVM.Persist();
        }
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
            TemplatesVM.Delete(template);
    }
}
