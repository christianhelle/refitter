using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refitter.SourceGenerator;

/// <summary>
/// Finds generators and analyzers that the consuming project's packages already loaded into the compiler.
/// Using the consumer's own copies keeps their output in sync with the runtime libraries it references.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class LoadedGeneratorLocator
{
    private static readonly Version SourceBuildVersion = new(42, 42, 42, 42);

    /// <summary>
    /// Creates an instance of the Refit source generator matching the Refit version referenced by the compilation.
    /// </summary>
    /// <param name="compilation">The compilation being generated for.</param>
    /// <returns>The Refit generator, or <see langword="null"/> when Refit or its generator is not available.</returns>
    public static IIncrementalGenerator? CreateRefitGenerator(Compilation compilation) =>
        Create<IIncrementalGenerator>(
            compilation,
            "Refit",
            "InterfaceStubGeneratorV2",
            "Refit.Generator.InterfaceStubGeneratorV2");

    /// <summary>
    /// Creates an instance of the Refit interface analyzer matching the Refit version referenced by the compilation.
    /// </summary>
    /// <param name="compilation">The compilation being generated for.</param>
    /// <returns>The Refit analyzer, or <see langword="null"/> when Refit or its analyzer is not available.</returns>
    public static DiagnosticAnalyzer? CreateRefitAnalyzer(Compilation compilation) =>
        Create<DiagnosticAnalyzer>(
            compilation,
            "Refit",
            "Refit.Analyzers",
            "Refit.Analyzers.RefitInterfaceAnalyzer");

    /// <summary>
    /// Creates an instance of the System.Text.Json source generator matching the System.Text.Json version
    /// referenced by the compilation.
    /// </summary>
    /// <param name="compilation">The compilation being generated for.</param>
    /// <returns>The System.Text.Json generator, or <see langword="null"/> when it is not available.</returns>
    public static IIncrementalGenerator? CreateJsonGenerator(Compilation compilation) =>
        Create<IIncrementalGenerator>(
            compilation,
            "System.Text.Json",
            "System.Text.Json.SourceGeneration",
            "System.Text.Json.SourceGeneration.JsonSourceGenerator");

    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "By design")]
    private static T? Create<T>(
        Compilation compilation,
        string referenceAssemblyName,
        string loadedAssemblyName,
        string typeName)
        where T : class
    {
        var reference = compilation.ReferencedAssemblyNames
            .FirstOrDefault(a => string.Equals(a.Name, referenceAssemblyName, StringComparison.Ordinal));
        if (reference is null)
        {
            return null;
        }

        // A shared compiler server can have several versions loaded, so pick the one matching the reference
        var loaded = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => (Assembly: assembly, Name: assembly.GetName()))
            .Where(a => string.Equals(a.Name.Name, loadedAssemblyName, StringComparison.Ordinal) && a.Name.Version is not null)
            .ToList();

        var candidates = OrderCandidates(loaded.Select(a => a.Name.Version!), reference.Version)
            .SelectMany(version => loaded.Where(a => a.Name.Version == version));

        foreach (var candidate in candidates)
        {
            var type = candidate.Assembly.GetType(typeName, throwOnError: false);
            if (type is not null && typeof(T).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type) as T;
            }
        }

        return null;
    }

    /// <summary>
    /// Orders the versions of the loaded generator assemblies by preference for a referenced library version: the
    /// newest with the same major version first, then source-built ones. Other major versions are left out.
    /// </summary>
    /// <remarks>
    /// Source-built .NET SDKs, like the ones Linux distributions ship, version the analyzers in their targeting
    /// packs 42.42.42.42, so their major version never matches the library the compilation references.
    /// </remarks>
    internal static IReadOnlyList<Version> OrderCandidates(IEnumerable<Version> loadedVersions, Version referenceVersion) =>
        loadedVersions
            .Distinct()
            .Where(version => version.Major == referenceVersion.Major || version == SourceBuildVersion)
            .OrderBy(version => version == SourceBuildVersion)
            .ThenByDescending(version => version)
            .ToList();
}
