using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Refitter.Core;

namespace Refitter.SourceGenerator.Models;
/// <summary>
/// A special settings definition that only applies to the source generator.
/// It is inherited from the <see cref="RefitGeneratorSettings"/> class,
/// so it just adds more settings, to the existing ones.
/// </summary>
/// <remarks>
/// Some properties are not used in the source generator.
/// An example is the <see cref="RefitGeneratorSettings.GenerateMultipleFiles"/> property.
/// That's because the source generator has with the current implamentation, some limitations.
/// </remarks>
[ExcludeFromCodeCoverage]
public class RefitSourceGeneratorSettings: RefitGeneratorSettings
{
    /// <summary>
    /// Will generate the file, visibile into the project directory.
    /// If this is enabled the generated files must be checked in into the repository, to not break CI builds.
    /// </summary>
    /// <remarks>
    /// For new projects it's recommend to not touch this property, if your CI has access to the OpenApi file,
    /// without any protection. (Like throttling, login protection, etc.)
    /// That's because if this is set to false (the default value), the source generator will work in a mode, that the compiler will get a notice that the files where changed.
    /// If this setting is set to true, it can happen that only the second build is successful, inspecial if your OpenApi Spec is generated while your solution builds.
    /// For old projects it's recommend to set this property to true, to not break existing documentation on how to build a project.
    /// </remarks>
    [Description("Will generate the file, visibile into the project directory. " +
    "If this is enabled the generated files must be checked in into the repository, to not break CI builds.")]
    public bool GenerateVisibilFile { get; set; } = false;
}