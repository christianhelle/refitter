using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Refitter;

[ExcludeFromCodeCoverage]
internal static class Program
{
    private const string OutputArg = "--output";
    private const string DefaultOpenApiPath = "./openapi.json";

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            args = new[]
            {
                "--help"
            };
        }

        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var app = new CommandApp<GenerateCommand>();
        app.Configure(
            configuration =>
            {
                configuration
                    .SetApplicationName("refitter")
                    .SetApplicationVersion(typeof(GenerateCommand).Assembly.GetName().Version!.ToString());

                configuration
                    .AddExample(DefaultOpenApiPath);

                configuration
                    .AddExample("https://petstore3.swagger.io/api/v3/openapi.yaml");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--settings-file",
                        "./openapi.refitter",
                        OutputArg,
                        "./GeneratedCode.cs");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--namespace",
                        "\"Your.Namespace.Of.Choice.GeneratedCode\"",
                        OutputArg,
                        "./GeneratedCode.cs");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--namespace",
                        "\"Your.Namespace.Of.Choice.GeneratedCode\"",
                        "--internal");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        OutputArg,
                        "./IGeneratedCode.cs",
                        "--interface-only");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        OutputArg,
                        "./GeneratedContracts.cs",
                        "--contract-only");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--use-api-response");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--cancellation-tokens");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--no-operation-headers");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--no-accept-headers");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--use-iso-date-format");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--additional-namespace",
                        "\"Your.Additional.Namespace\"",
                        "--additional-namespace",
                        "\"Your.Other.Additional.Namespace\"");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--multiple-interfaces",
                        "ByEndpoint");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--tag",
                        "Pet",
                        "--tag",
                        "Store",
                        "--tag",
                        "User");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--match-path",
                        "'^/pet/.*'");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--trim-unused-schema");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--trim-unused-schema",
                        " --keep-schema",
                        "'^Model$'",
                        "--keep-schema",
                        "'^Person.+'");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--no-deprecated-operations");
                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--operation-name-template",
                        "'{operationName}Async'");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--optional-nullable-parameters");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--use-polymorphic-serialization");

                configuration
                    .AddExample(
                        DefaultOpenApiPath,
                        "--collection-format",
                        "Csv");
            });

        return app.Run(args);
    }
}
