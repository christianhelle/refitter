using NSwag;

namespace Refitter.Core;

public sealed class RefitSchemaCleaner : IRefitSchemaCleaner
{
    public OpenApiDocument Clean(
        OpenApiDocument document,
        bool removeUnusedSchema,
        string[] keepSchemaPatterns,
        bool includeInheritanceHierarchy)
    {
        if (!removeUnusedSchema)
            return document;

        var result = CloneDocument(document);
        var cleaner = new SchemaCleaner(result, keepSchemaPatterns)
        {
            IncludeInheritanceHierarchy = includeInheritanceHierarchy
        };

        cleaner.RemoveUnreferencedSchema();
        return result;
    }

    private static OpenApiDocument CloneDocument(OpenApiDocument document)
        => OpenApiDocument.FromJsonAsync(document.ToJson()).GetAwaiter().GetResult();
}
