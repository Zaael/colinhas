using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Colinhas.Models;

public partial class ClipboardEntry : ObservableObject
{
    public required string Text { get; init; }
    public DateTime CopiedAt { get; init; } = DateTime.Now;

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    internal Action<ClipboardEntry>? OnCopy { private get; set; }
    internal Action<ClipboardEntry>? OnDelete { private get; set; }

    public string Preview => Text.Length > 150 ? Text[..150] + "…" : Text;

    public string TimeLabel => CopiedAt.Date == DateTime.Today
        ? CopiedAt.ToString("HH:mm")
        : CopiedAt.ToString("dd/MM HH:mm");

    [RelayCommand]
    private void Copy() => OnCopy?.Invoke(this);

    [RelayCommand]
    private void Delete() => OnDelete?.Invoke(this);

    [RelayCommand]
    private void TogglePin() => IsPinned = !IsPinned;
}
