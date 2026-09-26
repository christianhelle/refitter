using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

/// <summary>
/// Wraps a property name generator so the generated members of a contract type are unique (#1268): properties
/// that normalize to the same name (<c>user_name</c>, <c>userName</c>) get a numeric suffix, as do properties
/// that would clash with the enclosing type name (CS0542) or with the generated <c>AdditionalProperties</c>
/// dictionary. The wire names are kept by the generated <c>JsonPropertyName</c> attributes.
/// </summary>
internal sealed class UniquePropertyNameGenerator(
    IPropertyNameGenerator inner,
    Func<JsonSchema, string?> getTypeName) : IPropertyNameGenerator
{
    private const string AdditionalPropertiesName = "AdditionalProperties";

    private readonly Dictionary<JsonSchema, Dictionary<JsonSchemaProperty, string>> namesBySchema = new();

    internal IPropertyNameGenerator Inner => inner;

    public string Generate(JsonSchemaProperty property)
    {
        if (property.Parent is not JsonSchema parent)
            return inner.Generate(property);

        if (!namesBySchema.TryGetValue(parent, out var names))
        {
            names = GenerateUniqueNames(parent);
            namesBySchema[parent] = names;
        }

        return names[property];
    }

    private Dictionary<JsonSchemaProperty, string> GenerateUniqueNames(JsonSchema schema)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        var typeName = getTypeName(schema);
        if (typeName != null)
            usedNames.Add(typeName);

        // Mirrors when NJsonSchema generates the AdditionalProperties dictionary for a class
        if (schema.ActualTypeSchema.AllowAdditionalProperties || schema.ActualTypeSchema.AdditionalPropertiesSchema != null)
            usedNames.Add(AdditionalPropertiesName);

        var names = new Dictionary<JsonSchemaProperty, string>();
        foreach (var property in schema.Properties.Values)
        {
            var candidate = inner.Generate(property);

            // Like NJsonSchema's class template, which does not emit inheritance discriminators as members
            if (property.IsInheritanceDiscriminator)
            {
                names[property] = candidate;
                continue;
            }

            var uniqueName = candidate;
            for (var suffix = 2; !usedNames.Add(uniqueName); suffix++)
            {
                uniqueName = candidate + suffix;
            }

            names[property] = uniqueName;
        }

        return names;
    }
}
