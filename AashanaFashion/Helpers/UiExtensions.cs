using System.Text.RegularExpressions;

namespace AashanaFashion.Helpers;

/// <summary>Presentation helpers shared by Razor views.</summary>
public static partial class UiExtensions
{
    private static readonly HashSet<string> SmallWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "at", "by", "for", "in", "of", "on", "or", "the", "to"
    };

    /// <summary>Turns an enum member into a readable label, e.g. ReadyToDispatch → "Ready to Dispatch".</summary>
    public static string Humanize(this Enum value) => Humanize(value.ToString());

    /// <summary>Splits PascalCase / snake_case into words, e.g. "QCPassed" → "QC Passed".</summary>
    public static string Humanize(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var words = WordBoundary().Split(value.Replace('_', ' ').Trim())
            .Where(w => w.Length > 0)
            .ToArray();

        for (var i = 1; i < words.Length; i++)
        {
            if (SmallWords.Contains(words[i])) words[i] = words[i].ToLowerInvariant();
        }

        return string.Join(' ', words);
    }

    [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|\s+")]
    private static partial Regex WordBoundary();
}
