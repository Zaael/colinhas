using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Colinhas.Models;

public partial class ClipboardEntry : ObservableObject
{
    public required string Text { get; init; }
    public DateTime CopiedAt { get; init; } = DateTime.Now;

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool IsHidden { get; set; }

    internal Action<ClipboardEntry>? OnCopy { private get; set; }
    internal Action<ClipboardEntry>? OnDelete { private get; set; }

    private string Preview => Text.Length > 150 ? Text[..150] + "…" : Text;

    /// <summary>Text shown in the UI — masked when the entry is hidden (privacy).</summary>
    public string Display => IsHidden ? "•••••••••••••" : Preview;

    public string TimeLabel => CopiedAt.Date == DateTime.Today
        ? CopiedAt.ToString("HH:mm")
        : CopiedAt.ToString("dd/MM HH:mm");

    partial void OnIsHiddenChanged(bool value) => OnPropertyChanged(nameof(Display));

    [RelayCommand]
    private void Copy() => OnCopy?.Invoke(this);

    [RelayCommand]
    private void Delete() => OnDelete?.Invoke(this);
}
