using System.Text.Json.Serialization;

namespace Refitter.Core.Settings;

public sealed record ParameterConfigSlice(
    [property: JsonPropertyName("useCancellationTokens")] bool UseCancellationTokens = false,
    [property: JsonPropertyName("useIsoDateFormat")] bool UseIsoDateFormat = false,
    [property: JsonPropertyName("optionalParameters")] bool OptionalParameters = false,
    [property: JsonPropertyName("useDynamicQuerystringParameters")] bool UseDynamicQuerystringParameters = false,
    [property: JsonPropertyName("collectionFormat")] CollectionFormat CollectionFormat = CollectionFormat.Multi);
