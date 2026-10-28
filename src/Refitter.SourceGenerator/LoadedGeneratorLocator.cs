using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Refitter.SourceGenerator;

/// <summary>
/// Finds the Refit source generator that the consuming project's Refit package already loaded into the compiler.
/// Using the consumer's own generator keeps the generated stubs in sync with the Refit runtime it references.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RefitGeneratorLocator
{
    private const string RefitAssemblyName = "Refit";
    private const string GeneratorAssemblyName = "InterfaceStubGeneratorV2";
    private const string GeneratorTypeName = "Refit.Generator.InterfaceStubGeneratorV2";

    /// <summary>
    /// Creates an instance of the Refit source generator matching the Refit version referenced by the compilation.
    /// </summary>
    /// <param name="compilation">The compilation being generated for.</param>
    /// <returns>The Refit generator, or <see langword="null"/> when Refit or its generator is not available.</returns>
    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "By design")]
    public static IIncrementalGenerator? Create(Compilation compilation)
    {
        var refit = compilation.ReferencedAssemblyNames
            .FirstOrDefault(a => string.Equals(a.Name, RefitAssemblyName, StringComparison.Ordinal));
        if (refit is null)
        {
            return null;
        }

        // A shared compiler server can have several Refit versions loaded, so pick the one matching the reference
        var candidates = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => (Assembly: assembly, Name: assembly.GetName()))
            .Where(a => string.Equals(a.Name.Name, GeneratorAssemblyName, StringComparison.Ordinal) &&
                        a.Name.Version?.Major == refit.Version.Major)
            .OrderByDescending(a => a.Name.Version);

        foreach (var candidate in candidates)
        {
            var type = candidate.Assembly.GetType(GeneratorTypeName, throwOnError: false);
            if (type is not null && typeof(IIncrementalGenerator).IsAssignableFrom(type))
            {
                return (IIncrementalGenerator)Activator.CreateInstance(type);
            }
        }

        return null;
    }
}
