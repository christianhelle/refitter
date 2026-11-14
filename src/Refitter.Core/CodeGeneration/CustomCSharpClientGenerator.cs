using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class CustomCSharpClientGenerator(OpenApiDocument document, CSharpClientGeneratorSettings settings)
    : CSharpClientGenerator(document, settings)
{
    internal CSharpOperationModel CreateOperationModel(OpenApiOperation operation) =>
        base.CreateOperationModel(operation, Settings);

    internal string? GetTypeName(NJsonSchema.JsonSchema schema) =>
        Resolver.Types.TryGetValue(schema, out var typeName) ? typeName : null;

    internal IEnumerable<string> GeneratedTypeNames => Resolver.Types.Values;

    /// <summary>
    /// Gets the type names of named array and dictionary schemas that are inlined wherever they are used,
    /// so the named types themselves are never referenced by the generated code.
    /// </summary>
    internal IEnumerable<string> GetInlinedCollectionTypeNames() =>
        Resolver.Types
            .Where(
                type =>
                    (type.Key.IsArray && Settings.CSharpGeneratorSettings.InlineNamedArrays) ||
                    (type.Key.IsDictionary && Settings.CSharpGeneratorSettings.InlineNamedDictionaries))
            .Select(type => type.Value);

    internal bool HasGeneratedType(string typeName) =>
        Resolver.Types.Values.Contains(typeName, StringComparer.Ordinal);
}
