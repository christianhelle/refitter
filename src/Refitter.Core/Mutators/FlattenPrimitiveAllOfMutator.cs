using NJsonSchema;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Collapses a schema whose <c>allOf</c> contains a single primitive sub-schema
/// (e.g. <c>{ "allOf": [ { "type": "string" } ] }</c>) into that primitive type.
/// </summary>
/// <remarks>
/// OpenAPI specifications frequently wrap a primitive in <c>allOf</c> purely to
/// attach a description (since a sibling <c>description</c> next to a <c>$ref</c>
/// is not allowed in OpenAPI 3.0). NSwag treats every <c>allOf</c> as
/// composition and would otherwise generate a class deriving from the primitive
/// type, e.g. <c>public partial class Parent : string</c>, which is invalid C#
/// because <see cref="string"/> and the other primitive types are sealed.
/// </remarks>
internal sealed class FlattenPrimitiveAllOfMutator : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        SchemaWalker.TraverseDocumentSchemas(document, Flatten);
    }

    private static void Flatten(JsonSchema schema)
    {
        if (schema.AllOf.Count != 1)
            return;

        if (schema.Type == JsonObjectType.Object ||
            schema.Properties.Count != 0 ||
            schema.OneOf.Count != 0 ||
            schema.AnyOf.Count != 0)
            return;

        JsonSchema? inner = schema.AllOf.First().ActualSchema;
        if (inner == null || !IsPrimitive(inner.Type))
            return;

        schema.Type = inner.Type;
        schema.Format = inner.Format;

        if (string.IsNullOrEmpty(schema.Description))
            schema.Description = inner.Description;

        if (inner.IsEnumeration)
        {
            foreach (object? value in inner.Enumeration)
                schema.Enumeration.Add(value);

            foreach (string name in inner.EnumerationNames)
                schema.EnumerationNames.Add(name);
        }

        schema.AllOf.Clear();
    }

    private static bool IsPrimitive(JsonObjectType type) =>
        type == JsonObjectType.String ||
        type == JsonObjectType.Integer ||
        type == JsonObjectType.Number ||
        type == JsonObjectType.Boolean;
}
