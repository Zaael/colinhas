using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Colinhas.Models;
using Colinhas.Services;
using Windows.ApplicationModel.DataTransfer;

namespace Colinhas.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    private const int MaxUnpinned = 50;

    private readonly HistoryStore _historyStore = new();
    private ClipboardMonitor? _clipboardMonitor;
    private bool _suppressSave;

    /// <summary>The full history. UI binds to <see cref="FilteredItems"/> instead.</summary>
    public ObservableCollection<ClipboardEntry> Items { get; } = [];

    /// <summary>The history after applying <see cref="SearchText"/> — what the list shows.</summary>
    public ObservableCollection<ClipboardEntry> FilteredItems { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>True when nothing has been copied yet.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>True when there are items but the search matched none of them.</summary>
    public bool NoResults => Items.Count > 0 && FilteredItems.Count == 0;

    public MainPageViewModel()
    {
        Items.CollectionChanged += OnItemsChanged;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;

        FilteredItems.Clear();
        foreach (var item in Items)
        {
            if (query.Length == 0
                || item.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Label.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(item);
            }
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(NoResults));
    }

    public void Initialize(nint windowHandle)
    {
        Logger.Log("ViewModel.Initialize");
        _clipboardMonitor = new ClipboardMonitor(windowHandle);
        _clipboardMonitor.TextCaptured += AddFromClipboard;

        // Load our own saved history first, then merge the Windows clipboard
        // history (Win+V). Saving is suppressed until the merge finishes.
        _suppressSave = true;
        LoadPersistedHistory();
        _ = LoadWindowsHistoryAsync();
    }

    private void LoadPersistedHistory()
    {
        foreach (var s in _historyStore.Load())
        {
            var entry = CreateEntry(s.Text, s.CopiedAt);
            entry.IsPinned = s.IsPinned;
            entry.IsHidden = s.IsHidden;
            entry.Label = s.Label;
            Items.Add(entry);
        }
        Logger.Log($"Persisted history loaded: {Items.Count} itens");
    }

    private void SaveHistory()
    {
        if (_suppressSave) return;
        _historyStore.Save(Items);
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
        ApplyFilter();
        SaveHistory();
    }

    /// <summary>Number of pinned items — pinned always occupy the top slots.</summary>
    private int PinnedCount => Items.Count(i => i.IsPinned);

    private async Task LoadWindowsHistoryAsync()
    {
        try
        {
            if (!Clipboard.IsHistoryEnabled())
            {
                Logger.Log("Windows clipboard history is OFF (Settings > Clipboard)");
                return;
            }

            var result = await Clipboard.GetHistoryItemsAsync();
            Logger.Log($"Win+V history status={result.Status} count={result.Items.Count}");
            if (result.Status != ClipboardHistoryItemsResultStatus.Success)
                return;

            // Collect Win+V texts not already in our (persisted) history.
            var newOnes = new List<(string Text, DateTime When)>();
            foreach (var item in result.Items)
            {
                if (!item.Content.Contains(StandardDataFormats.Text)) continue;

                string text;
                try { text = await item.Content.GetTextAsync(); }
                catch { continue; }

                if (string.IsNullOrWhiteSpace(text)) continue;
                if (Items.Any(i => i.Text == text)) continue;
                if (newOnes.Any(n => n.Text == text)) continue;

                newOnes.Add((text, item.Timestamp.LocalDateTime));
            }

            // result.Items is newest-first; insert oldest-first at the top of the
            // unpinned block so the most recent ends up highest.
            for (var k = newOnes.Count - 1; k >= 0; k--)
                Items.Insert(PinnedCount, CreateEntry(newOnes[k].Text, newOnes[k].When));

            TrimUnpinned();
        }
        catch (Exception ex)
        {
            Logger.Log($"LoadWindowsHistory error: {ex.Message}");
        }
        finally
        {
            // Merge done — enable persistence and write the current state once.
            _suppressSave = false;
            SaveHistory();
        }
    }

    private void AddFromClipboard(string text)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            // If it's already pinned, leave the pinned copy as-is.
            if (Items.Any(i => i.Text == text && i.IsPinned)) return;

            // Replace an existing unpinned duplicate (so it bumps to the top).
            var existing = Items.FirstOrDefault(i => i.Text == text && !i.IsPinned);
            if (existing != null) RemoveEntry(existing);

            // Insert at the top of the unpinned block (right below the pinned ones).
            Items.Insert(PinnedCount, CreateEntry(text));

            TrimUnpinned();
        });
    }

    private ClipboardEntry CreateEntry(string text, DateTime? copiedAt = null)
    {
        var entry = new ClipboardEntry
        {
            Text = text,
            CopiedAt = copiedAt ?? DateTime.Now,
            OnCopy = Bump,
            OnDelete = RemoveEntry,
        };
        entry.PropertyChanged += OnEntryPropertyChanged;
        return entry;
    }

    private void Bump(ClipboardEntry entry)
    {
        _clipboardMonitor?.SetText(entry.Text);
        Reposition(entry);
    }

    /// <summary>Puts arbitrary text on the clipboard (used by templates).</summary>
    public void CopyToClipboard(string text) => _clipboardMonitor?.SetText(text);

    /// <summary>Copies an entry to the clipboard and moves it to the top (used by item click).</summary>
    public void CopyEntry(ClipboardEntry entry) => Bump(entry);

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ClipboardEntry entry) return;

        if (e.PropertyName == nameof(ClipboardEntry.IsPinned))
            Reposition(entry);

        if (e.PropertyName is nameof(ClipboardEntry.IsPinned)
            or nameof(ClipboardEntry.IsHidden)
            or nameof(ClipboardEntry.Label))
        {
            SaveHistory();
        }
    }

    /// <summary>
    /// Moves an entry to its correct slot: pinned items go to the very top,
    /// unpinned items go just below the pinned block.
    /// </summary>
    private void Reposition(ClipboardEntry entry)
    {
        var current = Items.IndexOf(entry);
        if (current < 0) return;

        var target = entry.IsPinned ? 0 : Items.Count(i => i.IsPinned && i != entry);
        if (target != current)
            Items.Move(current, target);
    }

    private void TrimUnpinned()
    {
        var unpinned = Items.Where(i => !i.IsPinned).ToList();
        while (unpinned.Count > MaxUnpinned)
        {
            RemoveEntry(unpinned[^1]);
            unpinned.RemoveAt(unpinned.Count - 1);
        }
    }

    private void RemoveEntry(ClipboardEntry entry)
    {
        entry.PropertyChanged -= OnEntryPropertyChanged;
        Items.Remove(entry);
    }

    [RelayCommand]
    private void ClearHistory()
    {
        foreach (var item in Items.Where(i => !i.IsPinned).ToList())
            RemoveEntry(item);
    }

    public void Dispose() => _clipboardMonitor?.Dispose();
}
