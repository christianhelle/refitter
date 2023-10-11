namespace Refitter.Core;

/// <summary>
/// Specifies the accessibility of a type.
/// </summary>
public enum TypeAccessibility
{
    /// <summary>
    /// Indicates that the type is accessible by any assembly that references it.
    /// </summary>
    Public,

    /// <summary>
    /// Indicates that the type is only accessible within its own assembly.
    /// </summary>
    Internal
}