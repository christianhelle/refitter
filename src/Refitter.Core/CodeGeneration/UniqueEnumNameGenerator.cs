using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

/// <summary>
/// Generates enum member names like NJsonSchema's <see cref="DefaultEnumNameGenerator"/>, but adds a numeric
/// suffix when two values of the same enum map to the same member name, e.g. <c>a</c> and <c>A</c> (#1267).
/// The original values are kept in the generated <c>EnumMember</c> attributes.
/// </summary>
internal sealed class UniqueEnumNameGenerator : IEnumNameGenerator
{
    private readonly DefaultEnumNameGenerator defaultGenerator = new();
    private readonly Dictionary<JsonSchema, Dictionary<int, string>> namesBySchema = new();

    public string Generate(int index, string? name, object? value, JsonSchema schema)
    {
        if (!namesBySchema.TryGetValue(schema, out var names))
        {
            names = GenerateUniqueNames(schema);
            namesBySchema[schema] = names;
        }

        return names[index];
    }

    private Dictionary<int, string> GenerateUniqueNames(JsonSchema schema)
    {
        var names = new Dictionary<int, string>();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < schema.Enumeration.Count; i++)
        {
            // NJsonSchema skips null values when it builds the enum members
            var value = schema.Enumeration.ElementAt(i);
            if (value is null)
                continue;

            var candidate = defaultGenerator.Generate(i, GetOriginalName(schema, i, value), value, schema);
            var uniqueName = candidate;
            for (var suffix = 2; !usedNames.Add(uniqueName); suffix++)
            {
                uniqueName = candidate + suffix;
            }

            names[i] = uniqueName;
        }

        return names;
    }

    // Mirrors the names NJsonSchema's EnumTemplateModel passes to the enum name generator
    private static string GetOriginalName(JsonSchema schema, int index, object value)
    {
        if (schema.EnumerationNames.Count > index)
            return schema.EnumerationNames[index];

        if (schema.Type.HasFlag(JsonObjectType.Integer))
            return "_" + value.ToString();

        return value.ToString()!;
    }
}
