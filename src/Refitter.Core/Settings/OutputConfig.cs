using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Configuration for generated output paths, filenames, and namespaces.
/// </summary>
[ExcludeFromCodeCoverage]
public class OutputConfig
{
    /// <summary>
    /// Default output folder for generated files.
    /// </summary>
    public const string DefaultOutputFolder = "./Generated";

    /// <summary>
    /// Default namespace for generated code.
    /// </summary>
    public const string DefaultNamespace = "GeneratedCode";

    /// <summary>
    /// Gets or sets the namespace for the generated code. (default: GeneratedCode)
    /// </summary>
    [Description("The namespace for the generated code. Default is GeneratedCode.")]
    public string Namespace { get; set; } = DefaultNamespace;

    /// <summary>
    /// Gets or sets the namespace for the generated contracts. (default: GeneratedCode);
    /// </summary>
    [Description("The namespace for the generated contracts. Default is GeneratedCode.")]
    public string? ContractsNamespace { get; set; }

    /// <summary>
    /// Gets or sets the relative path to a folder in which the output files are generated. (default: ./Generated)
    /// </summary>
    [Description("The relative path to a folder in which the output files are generated. Default is ./Generated.")]
    public string OutputFolder { get; set; } = DefaultOutputFolder;

    /// <summary>
    /// Gets or sets the relative path to a folder where to store the generated contracts. (default: ./Generated)
    /// </summary>
    [Description("The relative path to a folder where to store the generated contracts. Default is ./Generated.")]
    public string? ContractsOutputFolder { get; set; }

    /// <summary>
    /// Gets or sets the filename of the generated code.
    /// For the CLI tool, the default is Output.cs
    /// For the Source Generator, this is the name of the generated class and the default is [.refitter defined naming OR .refitter filename].g.cs)
    /// </summary>
    [Description(
        """
        The filename of the generated code.
        For the CLI tool, the default is Output.cs
        For the Source Generator, this is the name of the generated class
        and the default is [.refitter defined naming OR .refitter filename].g.cs
        """
    )]
    public string? OutputFilename { get; set; }

    /// <summary>
    /// Set to <c>true</c> to generate multiple files. Default is <c>false</c>
    /// This is automatically set to <c>true</c> when <see cref="ContractsOutputFolder"/> is specified
    /// Refit interface(s) are written to a file called RefitInterfaces.cs
    /// Contracts are written to a file called Contracts.cs
    /// Dependency Injection is written to a file called DependencyInjection.cs
    /// When <see cref="RefitGeneratorSettings.GenerateJsonSerializerContext"/> is enabled, an additional serializer context file is emitted.
    /// </summary>
    [Description(
        """
        Generate multiple files. Default is false.
        This is automatically set to true when ContractsOutputFolder is specified
        Refit interface(s) are written to a file called RefitInterfaces.cs
        Contracts are written to a file called Contracts.cs
        Dependency Injection is written to a file called DependencyInjection.cs
        When GenerateJsonSerializerContext is enabled, an additional serializer context file is emitted.
        """
    )]
    public bool GenerateMultipleFiles { get; set; }
}
