using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Tells the user-chosen keys of OpenAPI and JSON Schema maps, like the names under <c>properties</c>, apart from
/// keywords, so the sanitizers do not rewrite a schema or property that only happens to be named like a keyword.
/// </summary>
internal static class NamedMapEntries
{
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
}
