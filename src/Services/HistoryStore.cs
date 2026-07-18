using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Colinhas.Models;

namespace Colinhas.Services;

/// <summary>
/// Persists the clipboard history to disk, encrypted with Windows DPAPI
/// (tied to the current user). Clipboard content is often sensitive
/// (passwords, tokens), so the file is never stored in plaintext.
/// </summary>
public sealed class HistoryStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Colinhas", "history.dat");

    /// <summary>Serializable snapshot of a history entry.</summary>
    public sealed record Snapshot(string Text, DateTime CopiedAt, bool IsPinned, bool IsHidden, string Label);

    public List<Snapshot> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return [];

            var encrypted = File.ReadAllBytes(FilePath);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plain);

            return JsonSerializer.Deserialize<List<Snapshot>>(json) ?? [];
        }
        catch (Exception ex)
        {
            Logger.Log($"HistoryStore.Load error: {ex.Message}");
            return [];
        }
    }

    public void Save(IEnumerable<ClipboardEntry> items)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            var snapshots = items
                .Select(i => new Snapshot(i.Text, i.CopiedAt, i.IsPinned, i.IsHidden, i.Label))
                .ToList();

            var json = JsonSerializer.Serialize(snapshots);
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(FilePath, encrypted);
        }
        catch (Exception ex)
        {
            Logger.Log($"HistoryStore.Save error: {ex.Message}");
        }
    }
}
