using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refitter.Core;

/// <summary>
/// Configuration for generated type visibility, naming conventions, and immutability.
/// </summary>
[ExcludeFromCodeCoverage]
public class TypeConfig
{
    /// <summary>
    /// Gets or sets the generated type accessibility. (default: Public)
    /// </summary>
    [Description("The generated type accessibility. Default is Public.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TypeAccessibility TypeAccessibility { get; set; } = TypeAccessibility.Public;

    /// <summary>
    /// Gets or sets how generated contract properties are named.
    /// </summary>
    [Description("Controls how generated contract properties are named. Default is PascalCase. Possible values: PascalCase, PreserveOriginal.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PropertyNamingPolicy PropertyNamingPolicy { get; set; } = PropertyNamingPolicy.PascalCase;

    /// <summary>
    /// Gets or sets a suffix to append to all generated contract type names.
    /// </summary>
    [Description("Suffix to append to all generated contract type names. Default is null which doesn't append any suffix.")]
    public string? ContractTypeSuffix { get; set; }

    /// <summary>
    /// Set to <c>true</c> to generate contracts as immutable records instead of classes
    /// </summary>
    [Description("Generate contracts as immutable records instead of classes.")]
    public bool ImmutableRecords { get; set; }
}
