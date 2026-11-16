using Refitter.Core;

namespace Refitter.SourceGenerator;

/// <summary>
/// Problems with the generated dependency injection code for the referenced Refit version.
/// </summary>
internal enum RefitVersionWarning
{
    /// <summary>
    /// Refit 17 generated clients need Refit's source generator, but Refitter doesn't run it over its output.
    /// </summary>
    RefitGeneratorNotRun,

    /// <summary>
    /// Refit 17 generated clients don't support <c>[FormObject]</c> flattening, which needs the reflection request builder.
    /// </summary>
    MultipartFormObject,

    /// <summary>
    /// Refit 17 generated clients need JSON type metadata, but Refitter doesn't generate a <c>JsonSerializerContext</c>.
    /// </summary>
    NoJsonSerializerContext,
}

/// <summary>
/// Adapts the generated code to the Refit version referenced by the consuming project.
/// </summary>
internal static class RefitVersion
{
    /// <summary>
    /// The first Refit major version that moved <c>AddRefitClient</c> to <c>Refit.HttpClientFactory.Reflection</c>.
    /// </summary>
    internal const int GeneratedClientOnlyMajorVersion = 17;

    private const string RefitAssemblyName = "Refit";
    private const string ReflectionRegistrationAssemblyName = "Refit.HttpClientFactory.Reflection";

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

    internal static bool ReferencesReflectionRegistration(IEnumerable<(string Name, Version Version)> referencedAssemblies) =>
        referencedAssemblies.Any(assembly =>
            string.Equals(assembly.Name, ReflectionRegistrationAssemblyName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Switches dependency injection to <c>AddRefitGeneratedClient</c> for Refit 17 or newer, unless the project
    /// references <c>Refit.HttpClientFactory.Reflection</c>, which still provides <c>AddRefitClient</c>.
    /// </summary>
    /// <param name="settings">The settings to adapt.</param>
    /// <param name="refitMajorVersion">The referenced Refit major version, or <see langword="null"/> when unknown.</param>
    /// <param name="reflectionRegistration">Whether the project references <c>Refit.HttpClientFactory.Reflection</c>.</param>
    /// <param name="refitGeneratorRuns">Whether Refitter runs Refit's source generator over its output.</param>
    /// <returns>The problems the generated registration code will have.</returns>
    internal static IReadOnlyList<RefitVersionWarning> Apply(
        RefitGeneratorSettings settings,
        int? refitMajorVersion,
        bool reflectionRegistration,
        bool refitGeneratorRuns)
    {
        if (refitMajorVersion is null ||
            refitMajorVersion < GeneratedClientOnlyMajorVersion ||
            settings.DependencyInjectionSettings is not { } dependencyInjectionSettings)
        {
            return [];
        }

        if (!reflectionRegistration)
        {
            dependencyInjectionSettings.UseGeneratedRefitClient = true;
        }

        if (!dependencyInjectionSettings.UseGeneratedRefitClient)
        {
            return [];
        }

        var warnings = new List<RefitVersionWarning>();
        if (!refitGeneratorRuns)
        {
            warnings.Add(RefitVersionWarning.RefitGeneratorNotRun);
        }

        if (settings.UseMultipartFormObject)
        {
            warnings.Add(RefitVersionWarning.MultipartFormObject);
        }

        // Refitter only generates a JsonSerializerContext together with the contracts
        if (!settings.GenerateJsonSerializerContext || !settings.GenerateContracts)
        {
            warnings.Add(RefitVersionWarning.NoJsonSerializerContext);
        }

        return warnings;
    }
}
