using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public class TypeConfig
{
    [Description("The generated type accessibility. Default is Public.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TypeAccessibility TypeAccessibility { get; set; } = TypeAccessibility.Public;

    [Description("Controls how generated contract properties are named. Default is PascalCase. Possible values: PascalCase, PreserveOriginal.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PropertyNamingPolicy PropertyNamingPolicy { get; set; } = PropertyNamingPolicy.PascalCase;

    [Description("Suffix to append to all generated contract type names. Default is null which doesn't append any suffix.")]
    public string? ContractTypeSuffix { get; set; }

    [Description("Generate contracts as immutable records instead of classes.")]
    public bool ImmutableRecords { get; set; }
}
