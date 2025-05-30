using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Configurable settings for naming in the client API
/// </summary>
[Description("Configurable settings for naming the client API interface")]
[ExcludeFromCodeCoverage]
public class NamingSettings
{
    /// <summary>
    /// Default interface name for generated API clients.
    /// </summary>
    public const string DefaultInterfaceName = "ApiClient";

    /// <summary>
    /// Gets or sets a value indicating whether the OpenApi title should be used. Default is true.
    /// </summary>
    [Description(
        """
            Set to true to use the title defined in the OpenAPI document for the interface name.
            Default is true
            """
    )]
    public bool UseOpenApiTitle { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the Interface. Default is "ApiClient".
    /// </summary>
    [Description("The name of the interface if UseOpenApiTitle is set to false. Default is IApiClient")]
    public string InterfaceName { get; set; } = DefaultInterfaceName;
}
