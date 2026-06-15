using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public class OutputConfig
{
    public const string DefaultOutputFolder = "./Generated";
    public const string DefaultNamespace = "GeneratedCode";

    [Description("The namespace for the generated code. Default is GeneratedCode.")]
    public string Namespace { get; set; } = DefaultNamespace;

    [Description("The namespace for the generated contracts. Default is GeneratedCode.")]
    public string? ContractsNamespace { get; set; }

    [Description("The relative path to a folder in which the output files are generated. Default is ./Generated.")]
    public string OutputFolder { get; set; } = DefaultOutputFolder;

    [Description("The relative path to a folder where to store the generated contracts. Default is ./Generated.")]
    public string? ContractsOutputFolder { get; set; }

    [Description(
        """
        The filename of the generated code.
        For the CLI tool, the default is Output.cs
        The the Source Generator, this is the name of the generated class
        and the default is [.refitter defined naming OR .refitter filename].g.cs)
        """
    )]
    public string? OutputFilename { get; set; }

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
