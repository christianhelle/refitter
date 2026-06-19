namespace Refitter.Core;

/// <summary>
/// Configuration for schema processing and cleaning.
/// </summary>
public interface ISchemaConfiguration
{
    /// <summary>
    /// Gets a value indicating whether to trim unused schema components.
    /// </summary>
    bool TrimUnusedSchema { get; }

    /// <summary>
    /// Gets the regular expression patterns for schemas to keep during trimming.
    /// </summary>
    string[] KeepSchemaPatterns { get; }

    /// <summary>
    /// Gets a value indicating whether to keep all possible inherited types.
    /// </summary>
    bool IncludeInheritanceHierarchy { get; }

    /// <summary>
    /// Gets a value indicating whether to generate default additional properties.
    /// </summary>
    bool GenerateDefaultAdditionalProperties { get; }
}
