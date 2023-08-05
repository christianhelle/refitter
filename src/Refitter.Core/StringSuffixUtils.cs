using System.Text;

namespace Refitter.Core;

internal static class StringSuffixUtils
{
    public static string InterfaceNameWithCounter(StringBuilder stringBuilder, string name)
    {
        var body = stringBuilder.ToString().AsSpan();
        if (!ContainsBounded(body, $"interface I{name}".AsSpan()))
        {
            return name;
        }

        var counter = 2;
        while (ContainsBounded(body,$"interface I{name}{counter}".AsSpan()))
            counter++;

        return $"{name}{counter}";
    }
    
    private static bool ContainsBounded(ReadOnlySpan<char> haystack, ReadOnlySpan<char> needle)
    {
        var idx = haystack.IndexOf(needle, StringComparison.Ordinal);
        if (idx == -1)
        {
            return false;
        }

        return haystack.IsEmpty || !IsIdentifierChar(haystack[0]);
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}