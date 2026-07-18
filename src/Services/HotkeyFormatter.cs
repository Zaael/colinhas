using Windows.System;

namespace Colinhas.Services;

/// <summary>Formats a (modifiers, virtual-key) pair into a label like "Ctrl+Alt+1".</summary>
public static class HotkeyFormatter
{
    public static string Format(int modifiers, int virtualKey)
    {
        if (virtualKey == 0) return string.Empty;

        var parts = new List<string>();
        if ((modifiers & (int)GlobalHotkeys.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & (int)GlobalHotkeys.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & (int)GlobalHotkeys.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & (int)GlobalHotkeys.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(KeyName((VirtualKey)virtualKey));

        return string.Join("+", parts);
    }

    private static string KeyName(VirtualKey key) => key switch
    {
        >= VirtualKey.Number0 and <= VirtualKey.Number9 => ((int)(key - VirtualKey.Number0)).ToString(),
        >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9 => "Num" + (int)(key - VirtualKey.NumberPad0),
        _ => key.ToString(),
    };
}
