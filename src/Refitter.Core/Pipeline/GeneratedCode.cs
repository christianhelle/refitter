using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Represents generated code with a type name and content.
/// </summary>
/// <param name="TypeName">The name of the generated type.</param>
/// <param name="Content">The generated code content.</param>
[ExcludeFromCodeCoverage]
#pragma warning disable S1186 // Positional record constructor is compiler-generated
public record GeneratedCode(string TypeName, string Content)
#pragma warning restore S1186
{
    /// <summary>
    /// Gets the filename for the generated code based on the type name.
    /// </summary>
    public string Filename { get; } = $"{TypeName}.cs";

    /// <summary>
    /// Gets whether the generated type is a Refit interface, rather than a parameter wrapper type
    /// such as a dynamic querystring or multipart form object, so only interfaces are registered.
    /// </summary>
    internal bool IsInterface { get; init; } = true;
}
