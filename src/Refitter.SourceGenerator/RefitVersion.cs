using Refitter.Core;

namespace Refitter.SourceGenerator;

/// <summary>
/// Adapts the generated code to the Refit version referenced by the consuming project.
/// </summary>
internal static class RefitVersion
{
    /// <summary>
    /// The first Refit major version without the reflection-based <c>RestService.For</c> and <c>AddRefitClient</c> APIs.
    /// </summary>
    internal const int GeneratedClientOnlyMajorVersion = 17;

    private const string RefitAssemblyName = "Refit";

    internal static int? FindMajorVersion(IEnumerable<(string Name, Version Version)> referencedAssemblies)
    {
        foreach (var (name, version) in referencedAssemblies)
        {
            if (string.Equals(name, RefitAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return version.Major;
            }
        }

        return null;
    }

    internal static void Apply(RefitGeneratorSettings settings, int? refitMajorVersion)
    {
        if (refitMajorVersion >= GeneratedClientOnlyMajorVersion &&
            settings.DependencyInjectionSettings is { } dependencyInjectionSettings)
        {
            dependencyInjectionSettings.UseGeneratedRefitClient = true;
        }
    }
}
