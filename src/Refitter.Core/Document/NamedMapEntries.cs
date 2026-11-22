using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Tells the user-chosen keys of OpenAPI and JSON Schema maps, like the names under <c>properties</c>, apart from
/// keywords, so the sanitizers do not rewrite a schema or property that only happens to be named like a keyword.
/// </summary>
internal static class NamedMapEntries
{
    // Keywords whose values hold literal JSON, like example payloads, rather than OpenAPI objects. A link's
    // requestBody and parameters are literal values too, and nothing else under a links map is a media type.
    private static readonly HashSet<string> LiteralValueKeywords = new(StringComparer.Ordinal)
    {
        "example",
        "examples",
        "default",
        "const",
        "enum",
        "value",
        "dataValue",
        "serializedValue",
        "links",
    };

    // OpenAPI and JSON Schema maps whose keys are user-chosen names
    private static readonly HashSet<string> NamedMapKeywords = new(StringComparer.Ordinal)
    {
        "properties",
        "patternProperties",
        "dependentSchemas",
        "definitions",
        "$defs",
        "schemas",
        "responses",
        "parameters",
        "examples",
        "requestBodies",
        "headers",
        "securitySchemes",
        "securityDefinitions",
        "links",
        "callbacks",
        "pathItems",
        "webhooks",
        "content",
        "encoding",
        "variables",
    };

    // A property is an entry of a named map when its object is the value of a named map keyword. A keyword's
    // name alone is not enough: a schema property called "responses" is itself an entry of "properties".
    public static bool IsEntry(JProperty property) =>
        property.Parent!.Parent is JProperty map &&
        NamedMapKeywords.Contains(map.Name) &&
        !IsEntry(map);

    // Whether the token is inside an example, default, enum, const or x-* extension value, where objects are
    // only data. An entry of a named map, like a schema property called "default", is not such a value.
    public static bool IsInsideLiteralValue(JToken token) =>
        token
            .Ancestors()
            .OfType<JProperty>()
            .Any(ancestor =>
                (LiteralValueKeywords.Contains(ancestor.Name) || ancestor.Name.StartsWith("x-", StringComparison.Ordinal)) &&
                !IsEntry(ancestor));
}
