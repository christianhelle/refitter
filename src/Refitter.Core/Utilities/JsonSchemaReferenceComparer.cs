using System.Runtime.CompilerServices;
using NJsonSchema;

namespace Refitter.Core;

internal sealed class JsonSchemaReferenceComparer : IEqualityComparer<JsonSchema>
{
    public static JsonSchemaReferenceComparer Instance { get; } = new();

    public bool Equals(JsonSchema? x, JsonSchema? y) =>
        ReferenceEquals(x, y);

    public int GetHashCode(JsonSchema obj) =>
        RuntimeHelpers.GetHashCode(obj);
}
