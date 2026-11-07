using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Reads and rewrites JSON documents for <see cref="OpenApiJsonNormalizer"/>.
/// </summary>
internal static class SanitizerJson
{
    /// <summary>
    /// Offers every property of <paramref name="document"/> to <paramref name="tryConvert"/>, which rewrites it in
    /// place, and returns whether any property was rewritten.
    /// </summary>
    public static bool RewriteProperties(JContainer document, Func<JProperty, bool> tryConvert)
    {
        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
            converted |= tryConvert(property);

        return converted;
    }

    /// <summary>
    /// Parses a JSON document. Floating-point numbers keep the exact text they were written with, so
    /// re-serializing the document never rounds them, whether or not a decimal or double can hold them.
    /// </summary>
    public static JToken Parse(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
        };

        var document = JToken.ReadFrom(reader, new JsonLoadSettings { LineInfoHandling = LineInfoHandling.Load });
        if (document is not JContainer container)
            return document;

        var lineStarts = GetLineStarts(json);
        foreach (var value in container.Descendants().OfType<JValue>().Where(v => v.Type == JTokenType.Float).ToList())
            value.Replace(new JRaw(GetLiteral(json, lineStarts, value)));

        return document;
    }

    // The offsets in the text at which each line starts, counting \r\n, \r and \n as line breaks like the reader
    private static List<int> GetLineStarts(string json)
    {
        var lineStarts = new List<int> { 0 };
        var index = 0;
        while (index < json.Length)
        {
            var c = json[index++];
            if (c == '\r' && index < json.Length && json[index] == '\n')
                index++;

            if (c is '\r' or '\n')
                lineStarts.Add(index);
        }

        return lineStarts;
    }

    // The reader reports the position just after a value, so the literal is the number ending there
    private static string GetLiteral(string json, List<int> lineStarts, IJsonLineInfo lineInfo)
    {
        var end = lineStarts[lineInfo.LineNumber - 1] + lineInfo.LinePosition;
        var start = end;
        while (start > 0 && IsNumberCharacter(json[start - 1]))
            start--;

        return json.Substring(start, end - start);
    }

    private static bool IsNumberCharacter(char c) =>
        c is >= '0' and <= '9' or '-' or '+' or '.' or 'e' or 'E';
}
