using NSwag;

namespace Refitter.Core;

public interface IRefitSchemaCleaner
{
    OpenApiDocument Clean(
        OpenApiDocument document,
        bool removeUnusedSchema,
        string[] keepSchemaPatterns,
        bool includeInheritanceHierarchy);
}
