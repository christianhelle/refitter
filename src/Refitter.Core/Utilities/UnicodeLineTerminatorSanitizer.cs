namespace Refitter.Core;

/// <summary>
/// Replaces the Unicode line terminators that C# recognizes besides CR and LF
/// (U+0085, U+2028 and U+2029) with spaces inside single-line comments of generated code.
/// Text copied from the OpenAPI document into a <c>///</c> comment would otherwise end the comment
/// and turn the rest of the text into code. String and character literals are left unchanged.
/// </summary>
internal static class UnicodeLineTerminatorSanitizer
{
    private const char NextLine = (char)0x0085;
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;
    private const char CarriageReturn = (char)0x000D;
    private const char LineFeed = (char)0x000A;
    private const char Backslash = (char)0x005C;
    private const char Quote = '"';
    private const char Apostrophe = (char)0x0027;

    public static string Sanitize(string code)
    {
        if (code.IndexOfAny([NextLine, LineSeparator, ParagraphSeparator]) < 0)
            return code;

        var chars = code.ToCharArray();
        var i = 0;
        while (i < chars.Length)
        {
            i = chars[i] switch
            {
                '/' when Peek(chars, i + 1) == '/' => SanitizeLineComment(chars, i + 2),
                '/' when Peek(chars, i + 1) == '*' => SkipBlockComment(chars, i + 2),
                Quote => SkipStringLiteral(chars, i),
                Apostrophe => SkipQuotedLine(chars, i + 1, Apostrophe),
                _ => i + 1,
            };
        }

        return new string(chars);
    }

    private static char Peek(char[] chars, int index) =>
        index >= 0 && index < chars.Length ? chars[index] : default;

    private static bool IsCrOrLf(char c) => c is CarriageReturn or LineFeed;

    private static int SanitizeLineComment(char[] chars, int i)
    {
        for (; i < chars.Length && !IsCrOrLf(chars[i]); i++)
        {
            if (chars[i] is NextLine or LineSeparator or ParagraphSeparator)
                chars[i] = ' ';
        }

        return i;
    }

    private static int SkipBlockComment(char[] chars, int i)
    {
        for (; i < chars.Length; i++)
        {
            if (chars[i] == '*' && Peek(chars, i + 1) == '/')
                return i + 2;
        }

        return i;
    }

    private static int SkipStringLiteral(char[] chars, int start)
    {
        var quotes = CountQuotes(chars, start);
        if (quotes >= 3)
            return SkipRawString(chars, start + quotes, quotes);

        var isVerbatim = Peek(chars, start - 1) == '@' ||
                         (Peek(chars, start - 1) == '$' && Peek(chars, start - 2) == '@');
        return isVerbatim
            ? SkipVerbatimString(chars, start + 1)
            : SkipQuotedLine(chars, start + 1, Quote);
    }

    private static int CountQuotes(char[] chars, int start)
    {
        var end = start;
        while (end < chars.Length && chars[end] == Quote)
            end++;
        return end - start;
    }

    private static int SkipRawString(char[] chars, int i, int quotes)
    {
        while (i < chars.Length)
        {
            var run = CountQuotes(chars, i);
            if (run >= quotes)
                return i + run;
            i += Math.Max(run, 1);
        }

        return i;
    }

    private static int SkipVerbatimString(char[] chars, int i)
    {
        while (i < chars.Length)
        {
            if (chars[i] != Quote)
            {
                i++;
                continue;
            }

            if (Peek(chars, i + 1) != Quote)
                return i + 1;

            i += 2;
        }

        return i;
    }

    private static int SkipQuotedLine(char[] chars, int i, char terminator)
    {
        while (i < chars.Length && !IsCrOrLf(chars[i]))
        {
            if (chars[i] == terminator)
                return i + 1;

            i += chars[i] == Backslash ? 2 : 1;
        }

        return i;
    }
}
