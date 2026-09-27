using System.Text;
using System.Text.Json.Nodes;
using Refitter.Core.Validation.Model;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Tracks where the reader is in the document, and collects the diagnostics it finds there.
/// </summary>
/// <remarks>Reproduces the <c>ParsingContext</c> of Microsoft.OpenApi (MIT license).</remarks>
internal sealed class ParsingContext(ValidationDiagnostics diagnostics)
{
    private readonly Stack<string> currentLocation = new();

    public ValidationDiagnostics Diagnostics { get; } = diagnostics;

    /// <summary>
    /// Reads the document with the reader for the specification version it declares.
    /// </summary>
    /// <exception cref="SpecificationException">Thrown when the document declares no version.</exception>
    /// <exception cref="UnsupportedSpecificationVersionException">Thrown when the declared version is not supported.</exception>
    public SpecDocument Parse(JsonNode jsonNode)
    {
        var version = GetVersion(jsonNode);
        if (version.Equals("2.0", StringComparison.OrdinalIgnoreCase))
        {
            Diagnostics.SpecificationVersion = OpenApiSpecificationVersion.OpenApi2_0;
            return new SpecDocument();
        }

        if (version.StartsWith("3.0", StringComparison.OrdinalIgnoreCase))
        {
            Diagnostics.SpecificationVersion = OpenApiSpecificationVersion.OpenApi3_0;
            return new SpecDocument();
        }

        if (version.StartsWith("3.1", StringComparison.OrdinalIgnoreCase))
        {
            Diagnostics.SpecificationVersion = OpenApiSpecificationVersion.OpenApi3_1;
            return new SpecDocument();
        }

        if (version.StartsWith("3.2", StringComparison.OrdinalIgnoreCase))
        {
            Diagnostics.SpecificationVersion = OpenApiSpecificationVersion.OpenApi3_2;
            return new SpecDocument();
        }

        throw new UnsupportedSpecificationVersionException(version);
    }

    public void StartObject(string objectName) => currentLocation.Push(objectName);

    public void EndObject() => currentLocation.Pop();

    /// <summary>
    /// Returns the JSON pointer of the current location, such as <c>#/paths/~1pets/get</c>.
    /// </summary>
    public string GetLocation()
    {
        var location = new StringBuilder("#/");
        var segments = currentLocation.ToArray();
        for (var i = segments.Length - 1; i >= 0; i--)
        {
            location.Append(segments[i].Replace("~", "~0").Replace("/", "~1"));
            if (i > 0)
                location.Append('/');
        }

        return location.ToString();
    }

    private static string GetVersion(JsonNode jsonNode)
    {
        var version = Find(jsonNode, "openapi");
        if (version == null)
        {
            return Find(jsonNode, "swagger")?.GetScalarValue()?.Replace("\"", string.Empty)
                   ?? throw new SpecificationException("Version node not found.");
        }

        return version.GetScalarValue()?.Replace("\"", string.Empty)
               ?? throw new SpecificationException("Version node not found.");
    }

    /// <summary>
    /// Looks up a top-level property the way Microsoft.OpenApi's JSON pointer does: a node that is not an
    /// object is returned as is.
    /// </summary>
    private static JsonNode? Find(JsonNode jsonNode, string property)
    {
        if (jsonNode is JsonObject jsonObject)
            return jsonObject.TryGetPropertyValue(property, out var value) ? value : null;

        return jsonNode;
    }
}
