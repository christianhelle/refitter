using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public record GeneratedCode(string TypeName, string Content)
{
    public string Filename { get; } = $"{TypeName}.cs";
}
