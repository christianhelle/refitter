using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// JSON Schema 2020-12, and so OpenAPI 3.1, allows <c>items</c> to be a boolean schema. <c>items: false</c>
/// next to <c>prefixItems</c> is the usual way to write a tuple with no further elements. NJsonSchema reads
/// <c>items</c> as a schema object or an array of them, so a boolean fails the whole document load. This drops a
/// boolean <c>items</c> from an array schema in a JSON document before NSwag parses it, which leaves an array
/// of unconstrained items, the same as <c>items: true</c>. The generated client types it as a list of objects.
/// </summary>
internal static class BooleanItemsSanitizer
{
    // Finds candidate objects. A \u escape could spell a property name the scan cannot see,
    // so documents with one are always parsed. The boolean is matched in any case because the YAML conversion
    // writes the YAML 1.2 spellings True and FALSE as they appear.
    private static readonly Regex CandidateRegex = new(
        @"""items""\s*:\s*""?(?i:true|false)|\\u",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static bool IsCandidate(string json) => CandidateRegex.IsMatch(json);

    public static bool Rewrite(JContainer document) => SanitizerJson.RewriteProperties(document, TryConvert);

    private static bool TryConvert(JProperty property)
    {
        // A YAML document is converted to JSON without types, so its booleans are written as strings, in whichever
        // YAML 1.2 spelling the document used ("true", "True", "TRUE"). None of them is a schema
        if (property.Name != "items" ||
            property.Value is not JValue value ||
            !IsBoolean(value.Value) ||
            property.Parent is not JObject schema)
            return false;

        // A schema property called "items" is an entry of "properties", and its boolean is the property's schema
        if (NamedMapEntries.IsEntry(property))
            return false;

        // Literal data, like a default or an example, can hold an object that only looks like an array schema
        if (NamedMapEntries.IsInsideLiteralValue(property))
            return false;

        // Only an array schema has items. Anything else is literal data, like an example.
        if (!schema.ContainsKey("prefixItems") && !IsArrayType(schema["type"]))
            return false;

        property.Remove();
        return true;
    }

    private static bool IsBoolean(object? value) =>
        value is bool ||
        value is string text &&
        (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text.Equals("false", StringComparison.OrdinalIgnoreCase));

    // OpenAPI 3.1 allows a list of types, so a nullable array is written as ["array", "null"]
    private static bool IsArrayType(JToken? type) => type switch
    {
        JValue { Value: "array" } => true,
        JArray types => types.Any(t => t is JValue { Value: "array" }),
        _ => false,
    };
}
