using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Represents generated code with a type name and content.
/// </summary>
/// <param name="TypeName">The name of the generated type.</param>
/// <param name="Content">The generated code content.</param>
[ExcludeFromCodeCoverage]
public record GeneratedCode(string TypeName, string Content)
{
    /// <summary>
    /// Gets the filename for the generated code based on the type name.
    /// </summary>
    public string Filename { get; } = $"{TypeName}.cs";
}
