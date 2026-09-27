using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Refitter.Core;

/// <summary>
/// String conversions used to derive C# identifiers, literals and XML docs from OpenAPI names and text.
/// See THIRD-PARTY-NOTICES.md.
/// </summary>
internal static class ConversionUtilities
{
    private enum CamelCaseMode
    {
        None,
        FirstLower,
        FirstUpper,
    }

    private static readonly char[] CamelCaseCleanupChars = [' ', '/'];
    private static readonly char[] WhiteSpaceChars = ['\n', '\r', '\t', ' '];
    private static readonly char[] CSharpDocLineBreakChars = ['\r', '\n'];
    private static readonly Regex CSharpDocLineBreakRegex = new("^( *)/// ", RegexOptions.Multiline | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    public static string ConvertToLowerCamelCase(string? input, bool firstCharacterMustBeAlpha) =>
        ConvertToCamelCase(input, firstCharacterMustBeAlpha, CamelCaseMode.FirstLower);

    public static string ConvertToUpperCamelCase(string? input, bool firstCharacterMustBeAlpha) =>
        ConvertToCamelCase(input, firstCharacterMustBeAlpha, CamelCaseMode.FirstUpper);

    private static string ConvertToCamelCase(string? input, bool firstCharacterMustBeAlpha, CamelCaseMode mode)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (input!.IndexOfAny(CamelCaseCleanupChars) != -1)
            input = input.Replace(' ', '_').Replace('/', '_');

        if (input.IndexOf('-') == -1)
        {
            var first = input[0];
            if (char.IsNumber(first))
                return firstCharacterMustBeAlpha ? "_" + input : input;

            var converted = mode switch
            {
                CamelCaseMode.FirstUpper => char.ToUpperInvariant(first),
                CamelCaseMode.FirstLower => char.ToLowerInvariant(first),
                _ => first,
            };

            return converted != first ? converted + input.Substring(1) : input;
        }

        var builder = new StringBuilder(input.Length + 1);
        var capitalizeNext = false;
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '-')
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                builder.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
                continue;
            }

            if (i == 0)
            {
                if (firstCharacterMustBeAlpha && char.IsNumber(c))
                {
                    builder.Append('_');
                }
                else
                {
                    c = mode switch
                    {
                        CamelCaseMode.FirstUpper => char.ToUpperInvariant(c),
                        CamelCaseMode.FirstLower => char.ToLowerInvariant(c),
                        _ => c,
                    };
                }
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    public static string ConvertToStringLiteral(string input, string? prefix = null, string? postfix = null)
    {
        var builder = new StringBuilder(input.Length + (prefix?.Length ?? 0) + (postfix?.Length ?? 0));
        if (prefix != null)
            builder.Append(prefix);

        foreach (var c in input)
        {
            switch (c)
            {
                case '\'':
                    builder.Append("\\'");
                    continue;
                case '"':
                    builder.Append("\\\"");
                    continue;
                case '\\':
                    builder.Append("\\\\");
                    continue;
                case '\0':
                    builder.Append("\\0");
                    continue;
                case '\a':
                    builder.Append("\\a");
                    continue;
                case '\b':
                    builder.Append("\\b");
                    continue;
                case '\f':
                    builder.Append("\\f");
                    continue;
                case '\n':
                    builder.Append("\\n");
                    continue;
                case '\r':
                    builder.Append("\\r");
                    continue;
                case '\t':
                    builder.Append("\\t");
                    continue;
                case '\v':
                    builder.Append("\\v");
                    continue;
            }

            if (c >= ' ' && c <= '~')
            {
                builder.Append(c);
                continue;
            }

            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
        }

        if (postfix != null)
            builder.Append(postfix);

        return builder.ToString();
    }

    public static string TrimWhiteSpaces(string? text) => text?.Trim(WhiteSpaceChars) ?? string.Empty;

    /// <summary>Indents every non-empty line after the first by the given number of tabs (4 spaces each).</summary>
    public static string Tab(string? input, int tabCount)
    {
        if (input == null)
            return string.Empty;

        var tab = CreateTabString(tabCount);
        if (tab.Length == 0)
            return input;

        var builder = new StringBuilder(input.Length);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            builder.Append(c);
            if (c != '\n')
                continue;

            var hasContent = false;
            for (var j = i + 1; j < input.Length; j++)
            {
                var next = input[j];
                if (next == '\n')
                    break;

                if (!char.IsWhiteSpace(next))
                {
                    hasContent = true;
                    break;
                }
            }

            if (hasContent)
                builder.Append(tab);
        }

        return builder.ToString();
    }

    public static string ConvertCSharpDocs(string? input, int tabCount)
    {
        input ??= string.Empty;
        if (input.IndexOfAny(CSharpDocLineBreakChars) != -1)
        {
            input = input.Replace("\r", string.Empty).Replace("\n", "\n" + CreateTabString(tabCount) + "/// ");
        }

        var escaped = new XText(input).ToString();
        return CSharpDocLineBreakRegex.Replace(escaped, m => m.Groups[1] + "/// <br/>");
    }

    private static string CreateTabString(int tabCount) =>
        tabCount switch
        {
            0 => string.Empty,
            1 => "    ",
            2 => "        ",
            _ => new string(' ', 4 * tabCount),
        };
}
