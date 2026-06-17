using System.Text.Json.Serialization;

namespace Refitter.Core.Settings;

public sealed record SchemaConfigSlice(
    [property: JsonPropertyName("trimUnusedSchema")] bool TrimUnusedSchema = false,
    [property: JsonPropertyName("keepSchemaPatterns")] string[]? KeepSchemaPatterns = null,
    [property: JsonPropertyName("includeInheritanceHierarchy")] bool IncludeInheritanceHierarchy = false);
