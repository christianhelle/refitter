using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refitter.Core;

/// <summary>
/// Configuration for the OpenAPI document source paths.
/// </summary>
[ExcludeFromCodeCoverage]
public class OpenApiSourceConfig
{
    /// <summary>
    /// Gets or sets the path to the Open API.
    /// </summary>
    [Description("The path to the OpenAPI document.")]
    [JsonPropertyName("openApiPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OpenApiPath { get; set; }

    /// <summary>
    /// Gets or sets the paths to multiple Open API documents. When specified, the documents are merged.
    /// This takes precedence over <see cref="OpenApiPath"/> when non-empty.
    /// </summary>
    [Description("The paths to multiple OpenAPI documents. When specified, the documents are merged into a single client.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string[]? OpenApiPaths { get; set; }
}
