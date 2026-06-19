using NJsonSchema;

namespace Refitter.Core;

internal sealed class SafeSchemaTypeNameGenerator(
    HashSet<string> preferredExactTypeNameHints) : ITypeNameGenerator
{
    private const string AnonymousTypeName = "Anonymous";
    private readonly DefaultTypeNameGenerator inner = new();

    public string Generate(
        JsonSchema schema,
        string? typeNameHint,
        IEnumerable<string> reservedTypeNames)
    {
        var normalizedHint = IdentifierUtils.NormalizeSchemaTypeNameHint(typeNameHint)
                          ?? IdentifierUtils.NormalizeSchemaTypeNameHint(schema.Title)
                          ?? AnonymousTypeName;

        var typeNames = reservedTypeNames as string[] ?? reservedTypeNames.ToArray();
        if (!string.IsNullOrEmpty(typeNameHint) &&
            !string.Equals(typeNameHint, normalizedHint, StringComparison.Ordinal) &&
            preferredExactTypeNameHints.Contains(normalizedHint))
        {
            var reservedHints = typeNames
                .Concat(preferredExactTypeNameHints);

            var reservedHintSet = new HashSet<string>(reservedHints, StringComparer.Ordinal);

            normalizedHint = IdentifierUtils.Counted(reservedHintSet, normalizedHint);
        }

        var generatedTypeName = inner.Generate(schema, normalizedHint, typeNames);
        return string.IsNullOrWhiteSpace(generatedTypeName)
            ? inner.Generate(schema, AnonymousTypeName, typeNames)
            : generatedTypeName;
    }
}
