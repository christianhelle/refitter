using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public class FeatureConfig
{
    [Description("The settings describing how to configure Apizr.")]
    public ApizrSettings? ApizrSettings { get; set; }

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

    [Description("Generate JsonSerializerContext for AOT compilation support in the contracts namespace when contracts are generated.")]
    public bool GenerateJsonSerializerContext { get; set; }

    [Description("Generate Security Schema Authentication headers")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthenticationHeaderStyle AuthenticationHeaderStyle { get; set; }

    [Description("Security scheme for which to generate authentication headers.")]
    public string? SecurityScheme { get; set; }
}
