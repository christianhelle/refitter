using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Swagger 2.0 allows <c>csv</c>, <c>ssv</c>, <c>tsv</c>, <c>pipes</c> and <c>multi</c> as the
/// <c>collectionFormat</c> of an array parameter or its items. NSwag reads the value into an enum, so any other
/// value fails the whole document load. This drops an unrecognised value in a JSON document before NSwag parses
/// it, which leaves the format at its default, <c>csv</c>.
/// </summary>
internal static class CollectionFormatSanitizer
{
    private static readonly HashSet<string> KnownFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "csv",
        "ssv",
        "tsv",
        "pipes",
        "multi",
    };

    // Finds candidate objects. A \u escape could spell a property name the scan cannot see,
    // so documents with one are always parsed.
    private static readonly Regex CandidateRegex = new(
        @"""collectionFormat""\s*:|\\u",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static bool IsCandidate(string json) => CandidateRegex.IsMatch(json);

    public static bool Rewrite(JContainer document) => SanitizerJson.RewriteProperties(document, TryConvert);

    private static bool TryConvert(JProperty property)
    {
        if (property.Name != "collectionFormat" ||
            property.Value is not JValue { Type: JTokenType.String } format ||
            KnownFormats.Contains((string)format!))
            return false;

        // The keyword belongs to array parameters and items. An object without a type of array is a schema
        // describing data, or literal data in an example.
        if (property.Parent is not JObject owner || owner["type"] is not JValue { Value: "array" })
            return false;

        // Literal data, like an example or default value, is left as written
        if (NamedMapEntries.IsInsideLiteralValue(property))
            return false;

        property.Remove();
        return true;
    }
}
