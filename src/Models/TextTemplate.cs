using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Colinhas.Services;

namespace Colinhas.Models;

/// <summary>A reusable text snippet, optionally containing {placeholders}.</summary>
public partial class TextTemplate : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    /// <summary>Global hotkey modifiers (MOD_* bit flags). 0 = no hotkey.</summary>
    [ObservableProperty]
    public partial int HotkeyModifiers { get; set; }

    /// <summary>Global hotkey virtual-key. 0 = no hotkey.</summary>
    [ObservableProperty]
    public partial int HotkeyKey { get; set; }

    // --- UI helpers (not persisted) ---

    [JsonIgnore]
    public bool HasHotkey => HotkeyKey != 0;

    [JsonIgnore]
    public string HotkeyLabel => HotkeyFormatter.Format(HotkeyModifiers, HotkeyKey);

    [JsonIgnore]
    public string Preview => Content.Length > 120 ? Content[..120] + "…" : Content;

    [JsonIgnore]
    public IReadOnlyList<string> Placeholders => TemplateEngine.ExtractPlaceholders(Content);

    [JsonIgnore]
    public bool HasPlaceholders => Placeholders.Count > 0;

    [JsonIgnore]
    public string PlaceholderLabel =>
        HasPlaceholders ? string.Join(" · ", Placeholders) : string.Empty;

    // Callbacks wired by the view so the item buttons can trigger UI actions.
    [JsonIgnore] internal Action<TextTemplate>? OnUse { private get; set; }
    [JsonIgnore] internal Action<TextTemplate>? OnEdit { private get; set; }
    [JsonIgnore] internal Action<TextTemplate>? OnDelete { private get; set; }

    partial void OnContentChanged(string value)
    {
        OnPropertyChanged(nameof(Preview));
        OnPropertyChanged(nameof(Placeholders));
        OnPropertyChanged(nameof(HasPlaceholders));
        OnPropertyChanged(nameof(PlaceholderLabel));
    }

    partial void OnHotkeyModifiersChanged(int value) => OnPropertyChanged(nameof(HotkeyLabel));

    partial void OnHotkeyKeyChanged(int value)
    {
        OnPropertyChanged(nameof(HasHotkey));
        OnPropertyChanged(nameof(HotkeyLabel));
    }

    [RelayCommand]
    private void Use() => OnUse?.Invoke(this);

    [RelayCommand]
    private void Edit() => OnEdit?.Invoke(this);

    [RelayCommand]
    private void Delete() => OnDelete?.Invoke(this);
}
