using System.Text.RegularExpressions;

namespace Colinhas.Services;

/// <summary>
/// Parses and fills text templates. Placeholders use the <c>{name}</c> syntax,
/// e.g. "Olá {nome}, sua consulta é dia {data}".
/// </summary>
public static partial class TemplateEngine
{
    [GeneratedRegex(@"\{([^{}]+)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    /// <summary>Returns the distinct placeholder names found in the content (order preserved).</summary>
    public static IReadOnlyList<string> ExtractPlaceholders(string content)
    {
        if (string.IsNullOrEmpty(content)) return [];

        return PlaceholderRegex()
            .Matches(content)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Replaces each <c>{name}</c> with the matching value. Unmatched placeholders are left as-is.</summary>
    public static string Fill(string content, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(content)) return content;

        return PlaceholderRegex().Replace(content, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return values.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
