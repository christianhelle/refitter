using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// OpenAPI 3.2 describes each item of a sequential media type, like <c>text/event-stream</c>, with
/// <c>itemSchema</c> on the media type object. NSwag only reads <c>schema</c>, so the item type is dropped and
/// streaming responses fall back to <c>object</c>. This rewrites <c>itemSchema</c> to a <c>schema</c> holding an
/// array of those items in a JSON document before NSwag parses it. Documents written as OpenAPI 3.1 carry the
/// same schema in the <c>x-oai-itemSchema</c> extension.
/// <para>
/// The item schema of <c>text/event-stream</c> describes a whole event, with <c>data</c>, <c>event</c> and
/// <c>id</c> fields, but Refit deserializes only the <c>data</c> of each event. Its <c>data</c> schema is the
/// item type for those responses.
/// </para>
/// <para>
/// An item schema that references a component missing from the document is left alone, so the document still
/// loads with an untyped stream instead of failing on the reference.
/// </para>
/// </summary>
internal static class ItemSchemaSanitizer
{
    private static readonly HashSet<string> ItemSchemaKeywords = new(StringComparer.Ordinal)
    {
        "itemSchema",
        "x-oai-itemSchema",
    };

    // A cheap text scan that finds candidate objects, so that documents without them
    // are returned untouched instead of being parsed and re-serialized. A \u escape could spell
    // a property name the scan cannot see, so documents with one are always parsed.
    private static readonly Regex CandidateRegex = new(
        @"""(?:x-oai-)?itemSchema""\s*:|\\u",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static string Sanitize(string json)
    {
        if (!CandidateRegex.IsMatch(json))
            return json;

        var document = SanitizerJson.Parse(json);

        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
            converted |= TryConvert(property);

        return converted ? document.ToString(Formatting.None) : json;
    }

    private static bool TryConvert(JProperty property)
    {
        if (!ItemSchemaKeywords.Contains(property.Name) || !IsMediaTypeObject(property.Parent))
            return false;

        // A schema given alongside itemSchema already describes the whole response, so it is kept
        var mediaType = (JObject)property.Parent!;
        if (mediaType.ContainsKey("schema"))
            return false;

        var mediaTypeName = ((JProperty)mediaType.Parent!).Name;
        var itemSchema = IsServerSentEvents(mediaTypeName) && property.Value["properties"]?["data"] is JObject data
            ? data
            : property.Value;

        if (HasUnresolvedLocalReference(itemSchema))
            return false;

        property.Remove();
        mediaType["schema"] = new JObject
        {
            ["type"] = "array",
            ["items"] = itemSchema,
        };

        return true;
    }

    private static bool HasUnresolvedLocalReference(JToken schema) =>
        schema is JContainer container &&
        container
            .Descendants()
            .OfType<JProperty>()
            .Any(property =>
                property.Name == "$ref" &&
                property.Value.Type == JTokenType.String &&
                ((string)property.Value!).StartsWith("#/", StringComparison.Ordinal) &&
                ResolvePointer(schema.Root, ((string)property.Value!).Substring(1)) is null);

    // Resolves a JSON pointer like /components/schemas/Pet against the document
    private static JToken? ResolvePointer(JToken root, string pointer)
    {
        JToken? current = root;
        foreach (var segment in pointer.Split('/').Skip(1))
        {
            var name = Uri.UnescapeDataString(segment).Replace("~1", "/").Replace("~0", "~");
            current = current switch
            {
                JObject obj => obj[name],
                JArray array when int.TryParse(name, out var index) && index >= 0 && index < array.Count => array[index],
                _ => null,
            };

            if (current is null)
                return null;
        }

        return current;
    }

    private static bool IsServerSentEvents(string mediaTypeName)
    {
        var separatorIndex = mediaTypeName.IndexOf(';');
        var mediaType = separatorIndex >= 0
            ? mediaTypeName.Substring(0, separatorIndex)
            : mediaTypeName;

        return string.Equals(mediaType.Trim(), "text/event-stream", StringComparison.OrdinalIgnoreCase);
    }

    // A media type object is an entry of a "content" map, like "content": { "text/event-stream": { ... } }.
    // A schema property called "content" is itself an entry of "properties", so its value is a schema instead.
    private static bool IsMediaTypeObject(JContainer? container) =>
        container is JObject &&
        container.Parent is JProperty &&
        container.Parent.Parent?.Parent is JProperty { Name: "content" } content &&
        !NamedMapEntries.IsEntry(content);
}
