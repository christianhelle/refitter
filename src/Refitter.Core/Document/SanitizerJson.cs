using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Reads JSON documents for the sanitizers that rewrite them before NSwag parses them.
/// </summary>
internal static class SanitizerJson
{
    // Numbers are read as decimals so that re-serializing keeps them exact. Only a document with a number
    // outside the decimal range falls back to doubles, the way NumericBoundsSanitizer reads documents.
    public static JContainer Parse(string json)
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

        // The sanitizers' candidate scans only match a property name, so the root is an object or array
        return (JContainer)JToken.ReadFrom(reader);
    }
}
