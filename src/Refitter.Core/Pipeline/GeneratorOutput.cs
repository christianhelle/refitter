using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Represents the output of the code generator containing generated files.
/// </summary>
/// <param name="Files">The collection of generated code files.</param>
[ExcludeFromCodeCoverage]
public record GeneratorOutput(IReadOnlyList<GeneratedCode> Files);
