using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refitter.Core;

/// <summary>
/// Configurable settings for naming in the client API
/// </summary>
[ExcludeFromCodeCoverage]
public class NamingSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the OpenApi title should be used. Default is true.
    /// </summary>
    [JsonPropertyName("useOpenApiTitle")]
    public bool UseOpenApiTitle { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the Interface. Default is "ApiClient".
    /// </summary>
    [JsonPropertyName("interfaceName")]
    public string InterfaceName { get; set; } = "ApiClient";
}