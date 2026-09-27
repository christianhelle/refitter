using System.Text.Json;
using System.Text.Json.Nodes;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Stands in for JSON and YAML nulls while reading, as Microsoft.OpenApi (MIT license) does, so a null reads as
/// this sentinel string instead of being skipped.
/// </summary>
internal static class JsonNullSentinel
{
    private const string SentinelValue = "openapi-json-null-sentinel-value-2BF93600-0FE4-4250-987A-E5DDB203E464";

    public static JsonValue JsonNull { get; } = JsonValue.Create(SentinelValue);

    public static bool IsJsonNullSentinel(this JsonNode? node) =>
        node == JsonNull
        || (node != null && node.GetValueKind() == JsonValueKind.String && JsonNode.DeepEquals(JsonNull, node));
}
