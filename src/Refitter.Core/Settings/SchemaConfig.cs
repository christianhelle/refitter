using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public class SchemaConfig
{
    [Description(
        """
        Apply tree-shaking to the OpenApi schema.
        This works in conjunction with includeTags and includePathMatches.
        """
    )]
    public bool TrimUnusedSchema { get; set; }

    [Description(
        """
        Array of regular expressions that determine if a schema needs to be kept.
        This works in conjunction with TrimUnusedSchema.
        """
    )]
    public string[] KeepSchemaPatterns { get; set; } = [];

    [Description(
        """
        Keep all possible type-instances of inheritance/union types.
        If this is false only directly referenced types will be kept.
        This works in conjunction with TrimUnusedSchema.
        """
    )]
    public bool IncludeInheritanceHierarchy { get; set; }
}
