using System.Globalization;
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
        {
            if (GetLiteral(json, lineStarts, value) is { } literal)
                value.Replace(new JRaw(literal));
        }

        return document;
    }

    // The offsets in the text at which each line starts, counting \r\n, \r and \n as line breaks like the reader
    private static List<int> GetLineStarts(string json)
    {
        var lineStarts = new List<int> { 0 };
        for (var index = 0; index < json.Length; index++)
        {
            if (json[index] == '\r' && index + 1 < json.Length && json[index + 1] == '\n')
                index++;

            if (json[index] is '\r' or '\n')
                lineStarts.Add(index + 1);
        }

        return lineStarts;
    }

    // The number literal ending where the reader reported the value, or null when it cannot be found
    private static string? GetLiteral(string json, List<int> lineStarts, JValue value)
    {
        var lineInfo = (IJsonLineInfo)value;
        if (!lineInfo.HasLineInfo() || lineInfo.LineNumber < 1 || lineInfo.LineNumber > lineStarts.Count)
            return null;

        var end = Math.Min(lineStarts[lineInfo.LineNumber - 1] + lineInfo.LinePosition, json.Length);
        while (end > 0 && !IsNumberCharacter(json[end - 1]))
            end--;

        var start = end;
        while (start > 0 && IsNumberCharacter(json[start - 1]))
            start--;

        var literal = json.Substring(start, end - start);
        return double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) &&
               number.Equals(Convert.ToDouble(value.Value, CultureInfo.InvariantCulture))
            ? literal
            : null;
    }

    private static bool IsNumberCharacter(char c) =>
        c is >= '0' and <= '9' or '-' or '+' or '.' or 'e' or 'E';
}
