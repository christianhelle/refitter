using System.Text.RegularExpressions;
using Parlot.Fluent;

namespace Refitter.Core;

internal static class IdentifierUtils
{
    /// <summary>
    /// Returns <c>{value}{counter}{suffix}</c> if <c>{value}{name}</c> exists in <paramref name="knownIdentifiers"/>
    /// else returns <c>{value}{name}</c>.
    /// </summary>
    public static string Counted(ISet<string> knownIdentifiers, string name, string suffix = "", string parent = "")
    {
        var hasParent = !string.IsNullOrEmpty(parent);

        if (hasParent)
        {
            name = Regex.Replace(name, @"\d+$", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(50));
        }

        if (!knownIdentifiers.Contains(string.IsNullOrEmpty(parent) ? $"{name}{suffix}" : $"{parent}.{name}{suffix}"))
        {
            return $"{name}{suffix}";
        }

        var counter = 2;
        while (knownIdentifiers.Contains(!hasParent
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
}
