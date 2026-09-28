using System.Globalization;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Converts YAML to JSON nodes the way Microsoft.OpenApi.YamlReader (MIT license) does: plain scalars that parse
/// as a number or a boolean keep that type, plain null representations become <see cref="JsonNullSentinel"/>,
/// and every other scalar is a string.
/// </summary>
internal static class YamlToJsonConverter
{
    private static readonly HashSet<string> NullRepresentations = new(StringComparer.Ordinal) { "~", "null", "Null", "NULL" };

    public static JsonNode Read(TextReader reader)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(reader);
        }
        catch (YamlException exception)
        {
            throw ToSharpYamlException(exception);
        }

        if (stream.Documents.Count > 0)
        {
            var node = ToJsonNode(stream.Documents[0].RootNode);
            if (node != null)
                return node;
        }

        throw new InvalidOperationException("No documents found in the YAML stream.");
    }

    private static JsonNode ToJsonNode(YamlNode yaml) =>
        yaml switch
        {
            YamlMappingNode mapping => ToJsonObject(mapping),
            YamlSequenceNode sequence => ToJsonArray(sequence),
            _ => ToJsonValue((YamlScalarNode)yaml),
        };

    private static JsonObject ToJsonObject(YamlMappingNode yaml)
    {
        var jsonObject = new JsonObject();
        foreach (var item in yaml)
        {
            jsonObject[((YamlScalarNode)item.Key).Value!] = ToJsonNode(item.Value);
        }

        return jsonObject;
    }

    private static JsonArray ToJsonArray(YamlSequenceNode yaml)
    {
        var jsonArray = new JsonArray();
        foreach (var item in yaml)
        {
            jsonArray.Add(ToJsonNode(item));
        }

        return jsonArray;
    }

    private static JsonValue ToJsonValue(YamlScalarNode yaml)
    {
        // Quoted and block scalars are always strings
        if (yaml.Style != ScalarStyle.Plain)
            return JsonValue.Create(yaml.Value)!;

        if (decimal.TryParse(yaml.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return JsonValue.Create(number);

        if (bool.TryParse(yaml.Value, out var boolean))
            return JsonValue.Create(boolean);

        if (yaml.Value != null && NullRepresentations.Contains(yaml.Value))
            return (JsonValue)JsonNullSentinel.JsonNull.DeepClone();

        return JsonValue.Create(yaml.Value)!;
    }

    /// <summary>
    /// Microsoft.OpenApi parses YAML with SharpYaml, which reports zero-based lines and columns in its own format,
    /// and words some errors differently than YamlDotNet.
    /// </summary>
    private static YamlException ToSharpYamlException(YamlException exception)
    {
        var prefix = $"({exception.Start}) - ({exception.End}): ";
        var reason = exception.Message.StartsWith(prefix, StringComparison.Ordinal)
            ? exception.Message.Substring(prefix.Length)
            : exception.Message;
        var isSyntaxError = exception is not SemanticErrorException;
        if (SharpYamlErrors.TryGetValue(reason, out var sharpYamlError))
        {
            (reason, isSyntaxError) = sharpYamlError;
        }

        var message = $"({Describe(exception.Start)}) - ({Describe(exception.End)}): {reason}";
        return isSyntaxError
            ? new SyntaxErrorException(message)
            : new SemanticErrorException(message);
    }

    private static readonly Dictionary<string, (string Reason, bool IsSyntaxError)> SharpYamlErrors = new(StringComparer.Ordinal)
    {
        ["While scanning a plain scalar value, found invalid mapping."] =
            ("Mapping values are not allowed in this context.", true),
    };

    private static string Describe(Mark mark) =>
        $"Lin: {mark.Line - 1}, Col: {mark.Column - 1}, Chr: {mark.Index}";
}
