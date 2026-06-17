using System.Text.Json.Serialization;

namespace Refitter.Core.Settings;

public sealed record OpenApiSourceConfigSlice(
    [property: JsonPropertyName("openApiPath")] string? OpenApiPath = null,
    [property: JsonPropertyName("openApiPaths")] string[]? OpenApiPaths = null);
