using System.Text.Json.Serialization;

namespace Refitter.Core.Settings;

public sealed record FeatureConfigSlice(
    [property: JsonPropertyName("usePolymorphicSerialization")] bool UsePolymorphicSerialization = false,
    [property: JsonPropertyName("authenticationHeaderStyle")] AuthenticationHeaderStyle AuthenticationHeaderStyle = AuthenticationHeaderStyle.None,
    [property: JsonPropertyName("securityScheme")] string? SecurityScheme = null,
    [property: JsonPropertyName("generateJsonSerializerContext")] bool GenerateJsonSerializerContext = false);
