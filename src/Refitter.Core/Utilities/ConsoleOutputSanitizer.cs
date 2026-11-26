using System.Text;

namespace Refitter.Core;

/// <summary>
/// Makes text taken from an OpenAPI document inert before it is written to console output.
/// CI systems (Azure Pipelines, GitHub Actions, MSBuild markers) parse logging commands from
/// stdout lines, so untrusted text must neither break a line nor contain a command prefix.
/// </summary>
internal static class ConsoleOutputSanitizer
{
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;
    private const string VsoToken = "vso[";

    /// <summary>
    /// Returns a single-line, inert version of <paramref name="text"/>. CR and LF, other control
    /// characters and U+2028/U+2029 are replaced with literal escape sequences, then command
    /// prefixes are neutralized. Returns the same instance when nothing needs changing.
    /// </summary>
    /// <param name="text">The text to sanitize.</param>
    /// <returns>Single-line text that is safe to print.</returns>
    public static string SanitizeLine(string text)
    {
        StringBuilder? builder = null;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            string? escape = c switch
            {
                '\r' => @"\r",
                '\n' => @"\n",
                '\t' => null,
                LineSeparator or ParagraphSeparator => EscapeUnicode(c),
                _ when char.IsControl(c) => EscapeUnicode(c),
                _ => null,
            };

            if (escape is null)
            {
                builder?.Append(c);
                continue;
            }

            builder = Start(builder, text, i);
            builder.Append(escape);
        }

        var escaped = builder?.ToString() ?? text;
        return NeutralizeCommands(escaped);
    }

    /// <summary>
    /// Makes logging commands inert in multi-line text while keeping its line breaks unchanged.
    /// Inserts a space after <c>##</c> when followed by <c>[</c> or <c>vso[</c> (case-insensitive),
    /// and splits a leading <c>::</c> at the start of a line. Returns the same instance when
    /// nothing needs changing.
    /// </summary>
    /// <param name="text">The text to neutralize.</param>
    /// <returns>Text that no CI system treats as a logging command.</returns>
    public static string NeutralizeCommands(string text)
    {
        StringBuilder? builder = null;
        var lineStart = true;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];

            if (lineStart && c == ':' && Peek(text, i + 1) == ':')
            {
                builder = Start(builder, text, i);
                builder.Append(": ");
                lineStart = false;
                i++;
                continue;
            }

            if (c == '#' && Peek(text, i + 1) == '#' && IsCommandAt(text, i + 2))
            {
                builder = Start(builder, text, i);
                builder.Append("## ");
                i += 2;
                lineStart = false;
                continue;
            }

            builder?.Append(c);
            if (c is '\r' or '\n')
                lineStart = true;
            else if (!char.IsWhiteSpace(c))
                lineStart = false;
            i++;
        }

        return builder?.ToString() ?? text;
    }

    private static StringBuilder Start(StringBuilder? builder, string text, int upTo) =>
        builder ?? new StringBuilder(text, 0, upTo, text.Length + 16);

    private static char Peek(string text, int index) =>
        index < text.Length ? text[index] : '\0';

    private static bool IsCommandAt(string text, int index)
    {
        if (index >= text.Length)
            return false;

        if (text[index] == '[')
            return true;

        return index + VsoToken.Length <= text.Length &&
               string.Compare(text, index, VsoToken, 0, VsoToken.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static string EscapeUnicode(char c) => "\\u" + ((int)c).ToString("x4");
}
