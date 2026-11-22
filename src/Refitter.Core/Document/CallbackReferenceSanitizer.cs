using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// An operation callback can be a reference to <c>#/components/callbacks/...</c>, but NSwag only reads a callback
/// as an inline map of runtime expressions to path items, so a document using the reference fails to load.
/// Callbacks describe requests the API makes to the client, so they have no place in a Refit interface. This
/// replaces such a reference with an empty callback in a JSON document before NSwag parses it, which keeps
/// the name of the callback and drops nothing the generated client uses. References to other documents are left
/// alone, so the reference policy still decides what they may fetch.
/// </summary>
internal static class CallbackReferenceSanitizer
{
    private const string ComponentsPrefix = "#/components/callbacks/";

    // Finds candidate objects. A \u escape could spell a property name the scan cannot see,
    // so documents with one are always parsed.
    private static readonly Regex CandidateRegex = new(
        @"""callbacks""\s*:|\\u",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static bool IsCandidate(string json) => CandidateRegex.IsMatch(json);

    public static bool Rewrite(JContainer document) => SanitizerJson.RewriteProperties(document, TryConvert);

    private static bool TryConvert(JProperty property)
    {
        // Only entries of a callbacks map, not a schema property that happens to be called "callbacks"
        if (property.Parent!.Parent is not JProperty { Name: "callbacks" } ||
            !NamedMapEntries.IsEntry(property) ||
            LiteralValues.IsInside(property) ||
            property.Value is not JObject callback ||
            callback["$ref"] is not JValue { Type: JTokenType.String } reference ||
            !((string)reference!).StartsWith(ComponentsPrefix, StringComparison.Ordinal))
            return false;

        property.Value = new JObject();
        return true;
    }
}
