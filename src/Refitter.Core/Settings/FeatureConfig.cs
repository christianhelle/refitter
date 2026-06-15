using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refitter.Core;

/// <summary>
/// Configuration for optional features including Apizr, polymorphic serialization,
/// authentication headers, and AOT compilation support.
/// </summary>
[ExcludeFromCodeCoverage]
public class FeatureConfig
{
    /// <summary>
    /// Get ot set the settings describing how to configure Apizr
    /// </summary>
    [Description("The settings describing how to configure Apizr.")]
    public ApizrSettings? ApizrSettings { get; set; }

    /// <summary>
    /// Set to <c>true</c> to use System.Text.Json polymorphic serialization. Default is <c>false</c>
    /// Gets a value indicating whether to use System.Text.Json polymorphic serialization
    /// Replaces NSwag JsonInheritanceConverter attributes with System.Text.Json JsonPolymorphicAttributes.
    /// To have the native support of inheritance (de)serialization and fallback to base types when
    /// payloads with (yet) unknown types are offered by newer versions of an API
    /// See https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism for more information
    /// </summary>
    [Description(
        """
        Use System.Text.Json polymorphic serialization. Default is false.
        Replace NSwag JsonInheritanceConverter attributes with System.Text.Json JsonPolymorphicAttributes.
        To have the native support of inheritance (de)serialization and fallback to base types when
        payloads with (yet) unknown types are offered by newer versions of an API
        See https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism for more information
        """
    )]
    public bool UsePolymorphicSerialization { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate JsonSerializerContext for AOT compilation support.
    /// The context is emitted into the contracts namespace and only when contracts are generated.
    /// </summary>
    [Description("Generate JsonSerializerContext for AOT compilation support in the contracts namespace when contracts are generated.")]
    public bool GenerateJsonSerializerContext { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate Security Schema Authentication headers.
    /// </summary>
    [Description("Generate Security Schema Authentication headers")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthenticationHeaderStyle AuthenticationHeaderStyle { get; set; }

    /// <summary>
    /// Gets or sets the security scheme name for which to generate authentication headers.
    /// When specified, only endpoints using this security scheme will have authentication headers generated.
    /// </summary>
    [Description("Security scheme for which to generate authentication headers.")]
    public string? SecurityScheme { get; set; }
}
