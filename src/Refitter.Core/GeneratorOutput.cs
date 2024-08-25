using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public record GeneratorOutput(IReadOnlyList<GeneratedCode> Files);
