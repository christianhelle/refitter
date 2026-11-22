using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Tells literal data, like an example payload or a schema default, apart from the OpenAPI constructs the
/// sanitizers target, so a sanitizer does not rewrite data that merely looks like such a construct.
/// </summary>
internal static class LiteralValues
{
    // Keywords whose values hold no OpenAPI constructs, like example payloads and other literal JSON. A link's
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

    // A token is inside a literal value when an ancestor is a literal keyword or an extension, unless that
    // ancestor is itself a user-chosen name in a named map, like a schema property called "default"
    public static bool IsInside(JToken token) =>
        token
            .Ancestors()
            .OfType<JProperty>()
            .Any(ancestor =>
                (LiteralValueKeywords.Contains(ancestor.Name) || ancestor.Name.StartsWith("x-", StringComparison.Ordinal)) &&
                !NamedMapEntries.IsEntry(ancestor));
}
