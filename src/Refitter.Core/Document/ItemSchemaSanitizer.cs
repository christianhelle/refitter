using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// OpenAPI 3.2 describes each item of a sequential media type, like <c>text/event-stream</c>, with
/// <c>itemSchema</c> on the media type object. NSwag only reads <c>schema</c>, so the item type is dropped and
/// streaming responses fall back to <c>object</c>. This rewrites <c>itemSchema</c> to a <c>schema</c> holding an
/// array of those items in a JSON document before NSwag parses it.
/// </summary>
internal static class ItemSchemaSanitizer
{
    // A cheap text scan that finds candidate objects, so that documents without them
    // are returned untouched instead of being parsed and re-serialized. A \u escape could spell
    // a property name the scan cannot see, so documents with one are always parsed.
    private static readonly Regex CandidateRegex = new(
        @"""itemSchema""\s*:|\\u",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static string Sanitize(string json)
    {
        if (!CandidateRegex.IsMatch(json))
            return json;

        var document = Parse(json);

        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
            converted |= TryConvert(property);

        return converted ? document.ToString(Formatting.None) : json;
    }

    private static bool TryConvert(JProperty property)
    {
        if (property.Name != "itemSchema" || !IsMediaTypeObject(property.Parent))
            return false;

        var mediaType = (JObject)property.Parent!;
        property.Remove();
        mediaType["schema"] = new JObject
        {
            ["type"] = "array",
            ["items"] = property.Value,
        };

        return true;
    }

    // A media type object is an entry of a "content" map, like "content": { "text/event-stream": { ... } }
    private static bool IsMediaTypeObject(JContainer? container) =>
        container is JObject &&
        container.Parent is JProperty &&
        container.Parent.Parent?.Parent is JProperty { Name: "content" };

    // Numbers are read as decimals so that re-serializing keeps them exact. Only a document with a number
    // outside the decimal range falls back to doubles, the way NumericBoundsSanitizer reads documents.
    private static JContainer Parse(string json)
    {
        try
        {
            return Parse(json, FloatParseHandling.Decimal);
        }
        catch (JsonReaderException)
        {
            return Parse(json, FloatParseHandling.Double);
        }
    }

    private static JContainer Parse(string json, FloatParseHandling floatParseHandling)
    {
        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = floatParseHandling,
        };

        // The candidate scan only matches a property name, so the root is an object or array
        return (JContainer)JToken.ReadFrom(reader);
    }
}
