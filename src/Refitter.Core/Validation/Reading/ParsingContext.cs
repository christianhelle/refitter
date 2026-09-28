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
    private readonly Dictionary<string, object> tempStorage = new(StringComparer.Ordinal);
    private readonly Dictionary<object, Dictionary<string, object>> scopedTempStorage = new();

    public ValidationDiagnostics Diagnostics { get; } = diagnostics;

    /// <summary>
    /// The location a local file is read from, which Swagger 2.0 servers default to.
    /// </summary>
    public Uri? BaseUrl { get; set; }

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
            var document = new OpenApiV2Reader().LoadDocument(jsonNode, this);
            Diagnostics.SpecificationVersion = OpenApiSpecificationVersion.OpenApi2_0;
            return document;
        }

        if (version.StartsWith("3.0", StringComparison.OrdinalIgnoreCase))
        {
            return Load(OpenApiSpecificationVersion.OpenApi3_0, jsonNode);
        }

        if (version.StartsWith("3.1", StringComparison.OrdinalIgnoreCase))
        {
            return Load(OpenApiSpecificationVersion.OpenApi3_1, jsonNode);
        }

        if (version.StartsWith("3.2", StringComparison.OrdinalIgnoreCase))
        {
            return Load(OpenApiSpecificationVersion.OpenApi3_2, jsonNode);
        }

        throw new UnsupportedSpecificationVersionException(version);
    }

    private SpecDocument Load(OpenApiSpecificationVersion version, JsonNode jsonNode)
    {
        var document = new OpenApiV3Reader(version).LoadDocument(jsonNode, this);
        Diagnostics.SpecificationVersion = version;
        return document;
    }

    /// <summary>
    /// Values the Swagger 2.0 reader keeps between fields, globally or for one object.
    /// </summary>
    public T? GetFromTempStorage<T>(string key, object? scope = null)
    {
        Dictionary<string, object>? storage = tempStorage;
        if (scope != null)
            storage = scopedTempStorage.TryGetValue(scope, out var scoped) ? scoped : null;

        return storage != null && storage.TryGetValue(key, out var value) ? (T)value : default;
    }

    public void SetTempStorage(string key, object? value, object? scope = null)
    {
        Dictionary<string, object> storage;
        if (scope == null)
        {
            storage = tempStorage;
        }
        else if (!scopedTempStorage.TryGetValue(scope, out storage!))
        {
            storage = new Dictionary<string, object>(StringComparer.Ordinal);
            scopedTempStorage[scope] = storage;
        }

        if (value == null)
            storage.Remove(key);
        else
            storage[key] = value;
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
            return Find(jsonNode, "swagger")?.GetScalarValue().Replace("\"", string.Empty)
                   ?? throw new SpecificationException("Version node not found.");
        }

        return version.GetScalarValue().Replace("\"", string.Empty);
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
