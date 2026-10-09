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
        HashSet<JsonSchema> replacedSchemas = new HashSet<JsonSchema>();
        SchemaWalker.TraverseDocumentSchemas(document, schema =>
        {
            if (ReplaceWithBaseType(schema))
                replacedSchemas.Add(schema);
        });

        // A named union now aliases the base schema; drop its key so the
        // generated base type keeps the base schema's name
        string[] replacedSchemaNames = document.Components.Schemas
            .Where(kvp => replacedSchemas.Contains(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToArray();

        foreach (string name in replacedSchemaNames)
            document.Components.Schemas.Remove(name);
    }

    private static bool ReplaceWithBaseType(JsonSchema schema)
    {
        if (schema.DiscriminatorObject != null)
            return false;

        if (schema.OneOf.Count != 0 && schema.AnyOf.Count != 0)
            return false;

        ICollection<JsonSchema> unionSchemas = schema.OneOf.Count != 0 ? schema.OneOf : schema.AnyOf;
        if (unionSchemas.Count < 2)
            return false;

        JsonSchema? baseSchema = null;
        foreach (JsonSchema unionSchema in unionSchemas)
        {
            JsonSchema? candidate = FindDiscriminatedBaseSchema(unionSchema);
            if (candidate == null || (baseSchema != null && baseSchema != candidate))
                return false;

            baseSchema = candidate;
        }

        schema.OneOf.Clear();
        schema.AnyOf.Clear();
        schema.Reference = baseSchema;
        return true;
    }

    private static JsonSchema? FindDiscriminatedBaseSchema(JsonSchema unionSchema)
    {
        if (!unionSchema.HasReference)
            return null;

        if (unionSchema.ActualSchema.DiscriminatorObject != null)
            return unionSchema.ActualSchema;

        // Only the base that NJsonSchema generates as the C# parent class qualifies
        JsonSchema? inheritedSchema = unionSchema.ActualSchema.InheritedSchema;
        return inheritedSchema?.DiscriminatorObject != null ? inheritedSchema : null;
    }
}
