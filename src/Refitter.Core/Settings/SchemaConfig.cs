using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Configuration for OpenAPI schema tree-shaking and unused schema trimming.
/// </summary>
[ExcludeFromCodeCoverage]
public class SchemaConfig
{
    /// <summary>
    /// Set to <c>true</c> to apply tree-shaking to the OpenApi schema.
    /// This works in conjunction with <see cref="FilterConfig.IncludeTags"/> and <see cref="FilterConfig.IncludePathMatches"/>.
    /// </summary>
    [Description(
        """
        Apply tree-shaking to the OpenApi schema.
        This works in conjunction with includeTags and includePathMatches.
        """
    )]
    public bool TrimUnusedSchema { get; set; }

    /// <summary>
    /// Array of regular expressions that determine if a schema needs to be kept.
    /// This works in conjunction with <see cref="TrimUnusedSchema"/>.
    /// </summary>
    [Description(
        """
        Array of regular expressions that determine if a schema needs to be kept.
        This works in conjunction with TrimUnusedSchema.
        """
    )]
    public string[] KeepSchemaPatterns { get; set; } = [];

    /// <summary>
    /// Set to <c>true</c> to keep all possible type-instances of inheritance/union types.
    /// If this is <c>false</c> only directly referenced types will be kept.
    /// This works in conjunction with <see cref="TrimUnusedSchema"/>.
    /// </summary>
    [Description(
        """
        Keep all possible type-instances of inheritance/union types.
        If this is false only directly referenced types will be kept.
        This works in conjunction with TrimUnusedSchema.
        """
    )]
    public bool IncludeInheritanceHierarchy { get; set; }
}
