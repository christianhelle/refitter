using NJsonSchema;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Replaces a <c>oneOf</c>/<c>anyOf</c> whose members all derive (via <c>allOf</c>)
/// from, or are, the same discriminated base schema with a reference to that base schema.
/// </summary>
/// <remarks>
/// Specifications such as those produced by Swashbuckle with
/// <c>UseOneOfForPolymorphism</c> list the derived types inline, e.g.
/// <c>items: { oneOf: [ NewQuestion, ExistingQuestion ] }</c>, while the
/// discriminator lives on the shared base schema. NJsonSchema resolves a
/// <c>oneOf</c> without a type of its own to its first member, which would
/// otherwise generate <c>ICollection&lt;NewQuestion&gt;</c> instead of a
/// collection of the polymorphic base type.
/// </remarks>
internal sealed class InlineOneOfDerivedTypesToBaseTypeMutator : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        SchemaWalker.TraverseDocumentSchemas(document, ReplaceWithBaseType);
    }

    private static void ReplaceWithBaseType(JsonSchema schema)
    {
        if (schema.DiscriminatorObject != null)
            return;

        JsonSchema[] unionSchemas = schema.OneOf.Concat(schema.AnyOf).ToArray();
        if (unionSchemas.Length < 2)
            return;

        JsonSchema? baseSchema = null;
        foreach (JsonSchema unionSchema in unionSchemas)
        {
            JsonSchema? candidate = FindDiscriminatedBaseSchema(unionSchema);
            if (candidate == null || (baseSchema != null && baseSchema != candidate))
                return;

            baseSchema = candidate;
        }

        schema.OneOf.Clear();
        schema.AnyOf.Clear();
        schema.Reference = baseSchema;
    }

    private static JsonSchema? FindDiscriminatedBaseSchema(JsonSchema unionSchema)
    {
        if (!unionSchema.HasReference)
            return null;

        if (unionSchema.ActualSchema.DiscriminatorObject != null)
            return unionSchema.ActualSchema;

        return unionSchema.ActualSchema.AllOf
            .Where(a => a.HasReference && a.ActualSchema.DiscriminatorObject != null)
            .Select(a => a.ActualSchema)
            .FirstOrDefault();
    }
}
