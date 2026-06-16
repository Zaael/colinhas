using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Colinhas.Models;
using Colinhas.Services;

namespace Colinhas.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    private const int MaxUnpinned = 50;

    private ClipboardService? _clipboardService;

    public ObservableCollection<ClipboardEntry> Items { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    public void Initialize()
    {
        _clipboardService = new ClipboardService(AddFromClipboard);
    }

    private void AddFromClipboard(string text)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            // Remove unpinned duplicate (move to top instead)
            var existing = Items.FirstOrDefault(i => i.Text == text && !i.IsPinned);
            if (existing != null) Items.Remove(existing);

            // Already at top?
            if (Items.Count > 0 && Items[0].Text == text) return;

            Items.Insert(0, CreateEntry(text));

            // Trim old unpinned entries beyond limit
            var unpinned = Items.Where(i => !i.IsPinned).ToList();
            while (unpinned.Count > MaxUnpinned)
            {
                Items.Remove(unpinned[^1]);
                unpinned.RemoveAt(unpinned.Count - 1);
            }
        });
    }

    private ClipboardEntry CreateEntry(string text) => new()
    {
        Text = text,
        OnCopy = e =>
        {
            _clipboardService?.SetText(e.Text);
            // Bump to top if not already there
            if (Items.IndexOf(e) != 0)
            {
                Items.Remove(e);
                Items.Insert(0, e);
            }
        },
        OnDelete = e => Items.Remove(e),
    };

    [RelayCommand]
    private void ClearHistory()
    {
        var toRemove = Items.Where(i => !i.IsPinned).ToList();
        foreach (var item in toRemove)
            Items.Remove(item);
    }

    public void Dispose() => _clipboardService?.Dispose();
}
