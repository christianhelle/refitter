using System.ComponentModel;

namespace Refitter.Core;

/// <summary>
/// Specifies the accessibility of a type.
/// </summary>
[Description("Specifies the accessibility of the generated types. Default is Public")]
public enum TypeAccessibility
{
    /// <summary>
    /// Indicates that the type is accessible by any assembly that references it.
    /// </summary>
    [Description("Indicates that the type is accessible by any assembly that references it.")]
    Public,

    /// <summary>
    /// Indicates that the type is only accessible within its own assembly.
    /// </summary>
    [Description("Indicates that the type is only accessible within its own assembly.")]
    Internal
}
