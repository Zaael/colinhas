using Windows.Storage;

namespace Colinhas.Services;

/// <summary>Small persisted app settings, stored in the packaged app's LocalSettings.</summary>
public static class Settings
{
    private static ApplicationDataContainer Local => ApplicationData.Current.LocalSettings;

    /// <summary>When true, using an item auto-pastes it into the previously focused app.</summary>
    public static bool PasteDirectly
    {
        get { try { return Local.Values["PasteDirectly"] as bool? ?? true; } catch { return true; } }
        set { try { Local.Values["PasteDirectly"] = value; } catch { } }
    }

    /// <summary>
    /// True once the welcome tutorial has been completed (or dismissed) — it only
    /// opens by itself on the very first run.
    /// </summary>
    public static bool HasSeenWelcome
    {
        get { try { return Local.Values["HasSeenWelcome"] as bool? ?? false; } catch { return false; } }
        set { try { Local.Values["HasSeenWelcome"] = value; } catch { } }
    }
}
