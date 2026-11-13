namespace Refitter.Core;

/// <summary>
/// Replaces the Unicode line terminators that C# recognizes besides CR and LF
/// (U+0085, U+2028 and U+2029) with spaces in generated code. Text copied from the OpenAPI document
/// into a <c>///</c> comment would otherwise end the comment and turn the rest of the text into code.
/// </summary>
internal static class UnicodeLineTerminatorSanitizer
{
    private const char NextLine = (char)0x0085;
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;

    public static string Sanitize(string code)
    {
        if (code.IndexOfAny([NextLine, LineSeparator, ParagraphSeparator]) < 0)
            return code;

        return code
            .Replace(NextLine, ' ')
            .Replace(LineSeparator, ' ')
            .Replace(ParagraphSeparator, ' ');
    }
}
