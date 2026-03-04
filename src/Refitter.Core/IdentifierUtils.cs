namespace Refitter.Core;

internal static class IdentifierUtils
{
    /// <summary>
    /// Returns <c>{value}{counter}{suffix}</c> if <c>{value}{name}</c> exists in <paramref name="knownIdentifiers"/>
    /// else returns <c>{value}{name}</c>.
    /// </summary>
    public static string Counted(ISet<string> knownIdentifiers, string name, string suffix = "", string parent = "")
    {
        if (!knownIdentifiers.Contains(string.IsNullOrEmpty(parent) ? $"{name}{suffix}" : $"{parent}.{name}{suffix}"))
        {
            return $"{name}{suffix}";
        }

        var counter = 2;
        while (knownIdentifiers.Contains(string.IsNullOrEmpty(parent)
                   ? $"{name}{counter}{suffix}"
                   : $"{parent}.{name}{counter}{suffix}"))
            counter++;

        return $"{name}{counter}{suffix}";
    }

    private static readonly char[] IllegalSymbols =
    [
        ' ', '-', '.',
        '!', '@',
        '"', '\'',
        '\n', '\t',
        '#', '$', '%', '^', '&', '*', '+',
        ',', ':', ';',
        '(', ')', '[', ']', '}', '{',
        '|', '/', '\\'
    ];

    /// <summary>
    /// Removes invalid character from an identifier string
    /// </summary>
    public static string Sanitize(this string value)
    {
        const char dash = '-';

        // @ can be used and still make valid methode names. but this should make most use cases safe
        if (
            (value.First() < 'A' || value.First() > 'Z') &&
            (value.First() < 'a' || value.First() > 'z') &&
            value.First() != '_'
            )
        {
            value = "_" + value;
        }
        return string.Join(string.Empty, value.Split(IllegalSymbols, StringSplitOptions.RemoveEmptyEntries))
                .Trim(dash);
    }

    /// <summary>
    /// Sanitizes and formats controller tags for identifier usage.
    /// </summary>
    /// <param name="tag">The tag to sanitize.</param>
    /// <returns>A sanitized, title-cased identifier string.</returns>
    public static string SanitizeControllerTag(this string tag)
    {
        return tag.Sanitize().CapitalizeFirstCharacter();
    }
}
