using System.Text.Json.Serialization;

namespace Refitter.Core.Settings;

public sealed record TypeConfigSlice(
    [property: JsonPropertyName("typeAccessibility")] TypeAccessibility TypeAccessibility = TypeAccessibility.Public,
    [property: JsonPropertyName("propertyNamingPolicy")] PropertyNamingPolicy PropertyNamingPolicy = PropertyNamingPolicy.PascalCase,
    [property: JsonPropertyName("immutableRecords")] bool ImmutableRecords = false,
    [property: JsonPropertyName("contractTypeSuffix")] string? ContractTypeSuffix = null);
