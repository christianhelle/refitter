using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// NSwag cannot read path items that are references to OpenAPI 3.1 <c>components/pathItems</c>,
/// so this replaces such references in <c>paths</c> with the referenced path item (#1274).
/// </summary>
internal static class PathItemReferenceInliner
{
    private const string ComponentsPathItemsPrefix = "#/components/pathItems/";

    public static string Inline(string json)
    {
        if (json.IndexOf(ComponentsPathItemsPrefix, StringComparison.Ordinal) < 0)
            return json;

        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
        };

        if (JToken.ReadFrom(reader) is not JObject document ||
            document["paths"] is not JObject paths ||
            document["components"]?["pathItems"] is not JObject pathItems)
        {
            return json;
        }

        var inlined = false;
        foreach (var path in paths.Properties().ToList())
        {
            if (path.Value is JObject pathItem &&
                TryResolve(pathItem, pathItems, new HashSet<string>(StringComparer.Ordinal), out var resolved))
            {
                path.Value = resolved;
                inlined = true;
            }
        }

        return inlined ? document.ToString(Formatting.None) : json;
    }

    private static bool TryResolve(
        JObject pathItem,
        JObject pathItems,
        HashSet<string> visited,
        out JObject resolved)
    {
        resolved = pathItem;
        var reference = (pathItem["$ref"] as JValue)?.Value as string;
        if (reference is null || !reference.StartsWith(ComponentsPathItemsPrefix, StringComparison.Ordinal))
            return false;

        // JSON pointer escaping: ~1 is '/' and ~0 is '~'
        var name = reference
            .Substring(ComponentsPathItemsPrefix.Length)
            .Replace("~1", "/")
            .Replace("~0", "~");

        if (!visited.Add(name) || pathItems[name] is not JObject target)
            return false;

        var targetResolved = target;
        if (target["$ref"] is not null && !TryResolve(target, pathItems, visited, out targetResolved))
            return false;

        // Fields next to the $ref (e.g. summary, description) override the referenced ones
        resolved = (JObject)targetResolved.DeepClone();
        foreach (var sibling in pathItem.Properties().Where(p => p.Name != "$ref"))
        {
            resolved[sibling.Name] = sibling.Value.DeepClone();
        }

        return true;
    }
}
