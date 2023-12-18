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
        while (knownIdentifiers.Contains(string.IsNullOrEmpty(parent) ? $"{name}{counter}{suffix}" : $"{parent}.{name}{counter}{suffix}"))
            counter++;

        return $"{name}{counter}{suffix}";
    }

    /// <summary>
    /// Removes invalid character from an identifier string
    /// </summary>
    public static string Sanitize(string value)
    {
        return value.Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace(".", string.Empty);
    }
}