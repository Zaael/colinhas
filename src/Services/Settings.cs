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
}
