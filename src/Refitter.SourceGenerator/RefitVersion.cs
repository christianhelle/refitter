using Refitter.Core;

namespace Refitter.SourceGenerator;

/// <summary>
/// Problems with the generated dependency injection code for the referenced Refit version.
/// </summary>
internal enum RefitVersionWarning
{
    /// <summary>
    /// Refit 17 only registers generated clients, but Refitter doesn't run Refit's source generator over its output.
    /// </summary>
    RefitGeneratorNotRun,

    /// <summary>
    /// Refit 17 generated clients don't support <c>[FormObject]</c> flattening, which needs the reflection request builder.
    /// </summary>
    MultipartFormObject,
}

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

    /// <summary>
    /// Switches dependency injection to <c>AddRefitGeneratedClient</c> for Refit 17 or newer, which removed <c>AddRefitClient</c>.
    /// </summary>
    /// <param name="settings">The settings to adapt.</param>
    /// <param name="refitMajorVersion">The referenced Refit major version, or <see langword="null"/> when unknown.</param>
    /// <param name="refitGeneratorRuns">Whether Refitter runs Refit's source generator over its output.</param>
    /// <returns>The problems the generated registration code will have.</returns>
    internal static IReadOnlyList<RefitVersionWarning> Apply(
        RefitGeneratorSettings settings,
        int? refitMajorVersion,
        bool refitGeneratorRuns)
    {
        if (refitMajorVersion < GeneratedClientOnlyMajorVersion ||
            refitMajorVersion is null ||
            settings.DependencyInjectionSettings is not { } dependencyInjectionSettings)
        {
            return [];
        }

        dependencyInjectionSettings.UseGeneratedRefitClient = true;

        var warnings = new List<RefitVersionWarning>();
        if (!refitGeneratorRuns)
        {
            warnings.Add(RefitVersionWarning.RefitGeneratorNotRun);
        }

        if (settings.UseMultipartFormObject)
        {
            warnings.Add(RefitVersionWarning.MultipartFormObject);
        }

        return warnings;
    }
}
