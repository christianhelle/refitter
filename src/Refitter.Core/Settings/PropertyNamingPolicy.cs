using System.ComponentModel;

namespace Refitter.Core;

/// <summary>
/// Controls how generated contract property names are emitted.
/// </summary>
public enum PropertyNamingPolicy
{
    /// <summary>
    /// Convert OpenAPI property names to PascalCase C# property names.
    /// </summary>
    [Description("Convert OpenAPI property names to PascalCase C# property names.")]
    PascalCase,

    /// <summary>
    /// Preserve the original OpenAPI property name when possible and minimally sanitize invalid C# identifiers.
    /// </summary>
    [Description("Preserve original OpenAPI property names when possible and minimally sanitize invalid C# identifiers.")]
    PreserveOriginal,
}
