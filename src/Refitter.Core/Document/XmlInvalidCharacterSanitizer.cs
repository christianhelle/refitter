using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Descriptions, summaries and titles are written into XML documentation comments, and XML 1.0 rejects most
/// control characters (U+0000 to U+0008, U+000B, U+000C and U+000E to U+001F). A document carrying one of them
/// in such text makes NSwag fail while rendering its documentation templates. This strips those characters from
/// the text in a JSON document before NSwag parses it, which covers every place the text is later rendered.
/// </summary>
internal static class XmlInvalidCharacterSanitizer
{
    private static readonly HashSet<string> TextKeywords = new(StringComparer.Ordinal)
    {
        "description",
        "summary",
        "title",
    };

    // Finds a control character written as a \u escape or as itself. Escapes of other characters are
    // not candidates, so documents without such characters are returned untouched.
    private static readonly Regex CandidateRegex = new(
        @"\\u00(?:0[0-8bBcCeEfF]|1[0-9a-fA-F])|[\u0000-\u0008\u000B\u000C\u000E-\u001F]",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static bool IsCandidate(string json) => CandidateRegex.IsMatch(json);

    public static bool Rewrite(JContainer document) => SanitizerJson.RewriteProperties(document, TryStrip);

    private static bool TryStrip(JProperty property)
    {
        if (!TextKeywords.Contains(property.Name) ||
            property.Value is not JValue { Type: JTokenType.String, Value: string text })
            return false;

        if (!text.Any(IsInvalidXmlCharacter))
            return false;

        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (!IsInvalidXmlCharacter(c))
                builder.Append(c);
        }

        property.Value = new JValue(builder.ToString());
        return true;
    }

    private static bool IsInvalidXmlCharacter(char c) =>
        c < ' ' && c is not ('\t' or '\n' or '\r');
}
