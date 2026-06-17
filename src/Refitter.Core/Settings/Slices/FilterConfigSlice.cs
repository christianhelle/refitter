using System.Text.Json.Serialization;

namespace Refitter.Core.Settings;

public sealed record FilterConfigSlice(
    [property: JsonPropertyName("includeTags")] string[]? IncludeTags = null,
    [property: JsonPropertyName("includePathMatches")] string[]? IncludePathMatches = null,
    [property: JsonPropertyName("ignoredOperationHeaders")] string[]? IgnoredOperationHeaders = null,
    [property: JsonPropertyName("additionalNamespaces")] string[]? AdditionalNamespaces = null,
    [property: JsonPropertyName("excludeNamespaces")] string[]? ExcludeNamespaces = null);
