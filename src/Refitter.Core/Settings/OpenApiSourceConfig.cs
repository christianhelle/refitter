using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public class OpenApiSourceConfig
{
    [Description("The path to the OpenAPI document.")]
    [JsonPropertyName("openApiPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OpenApiPath { get; set; }

    [Description("The paths to multiple OpenAPI documents. When specified, the documents are merged into a single client.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string[]? OpenApiPaths { get; set; }
}
