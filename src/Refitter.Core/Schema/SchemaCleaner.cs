using NSwag;

namespace Refitter.Core;

/// <summary>
/// Cleans up the OpenAPI schema by removing unreferenced schemas and handling inheritance hierarchies.
/// </summary>
[Obsolete("Use RefitGeneratorSettings.TrimUnusedSchema instead. SchemaCleaner exposes NSwag types and will be removed in the next major version.")]
public class SchemaCleaner
{
    private readonly OpenApiDocument document;
    private readonly string[] keepSchemaPatterns;

    /// <summary>
    /// Gets or sets a value indicating whether to include inheritance hierarchy in the schema cleaning process.
    /// </summary>
    public bool IncludeInheritanceHierarchy { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaCleaner"/> class.
    /// </summary>
    /// <param name="document">The OpenAPI document to clean.</param>
    /// <param name="keepSchemaPatterns">Regular expression patterns for schemas to keep.</param>
    public SchemaCleaner(OpenApiDocument document, string[] keepSchemaPatterns)
    {
        this.document = document;
        this.keepSchemaPatterns = keepSchemaPatterns;
    }

    /// <summary>
    /// Removes unreferenced schemas and discriminator mappings from the OpenAPI document.
    /// Mappings remain when their targets are otherwise reachable or inheritance inclusion is
    /// enabled.
    /// </summary>
    public void RemoveUnreferencedSchema() =>
        new NSwagSchemaCleaner(document, keepSchemaPatterns)
        {
            IncludeInheritanceHierarchy = IncludeInheritanceHierarchy
        }.RemoveUnreferencedSchema();
}
