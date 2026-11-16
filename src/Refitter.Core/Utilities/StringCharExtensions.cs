namespace Refitter.Core;

/// <summary>
/// Polyfills for the char overloads of StartsWith and EndsWith, which netstandard2.0 lacks.
/// </summary>
internal static class StringCharExtensions
{
    public static bool StartsWith(this string str, char value) =>
        str.Length > 0 && str[0] == value;

    public static bool EndsWith(this string str, char value) =>
        str.Length > 0 && str[str.Length - 1] == value;
}
