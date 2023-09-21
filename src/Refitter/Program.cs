using System.Diagnostics.CodeAnalysis;

using Spectre.Console.Cli;

namespace Refitter;

[ExcludeFromCodeCoverage]
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            args = new[]
            {
                "--help"
            };
        }

        var app = new CommandApp<GenerateCommand>();
        app.Configure(
            configuration =>
            {
                configuration
                    .SetApplicationName("refitter")
                    .SetApplicationVersion(typeof(GenerateCommand).Assembly.GetName().Version!.ToString());

                configuration
                    .AddExample("./openapi.json");

                configuration
                    .AddExample("https://petstore3.swagger.io/api/v3/openapi.yaml");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--settings-file",
                        "./openapi.refitter",
                        "--output",
                        "./GeneratedCode.cs");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--namespace",
                        "\"Your.Namespace.Of.Choice.GeneratedCode\"",
                        "--output",
                        "./GeneratedCode.cs");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--namespace",
                        "\"Your.Namespace.Of.Choice.GeneratedCode\"",
                        "--internal");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--output",
                        "./IGeneratedCode.cs",
                        "--interface-only");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--use-api-response");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--cancellation-tokens");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--no-operation-headers");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--no-accept-headers");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--use-iso-date-format");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--additional-namespace",
                        "\"Your.Additional.Namespace\"",
                        "--additional-namespace",
                        "\"Your.Other.Additional.Namespace\"");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--multiple-interfaces",
                        "ByEndpoint");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--tag",
                        "Pet",
                        "--tag",
                        "Store",
                        "--tag",
                        "User");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--match-path",
                        "'^/pet/.*'");

                configuration
                    .AddExample(
                        "./openapi.json",
                        "--no-deprecated-operations");
                configuration
                    .AddExample(
                        "./openapi.json",
                        "--operation-name-template",
                        "'{operationName}Async'");

				configuration
                    .AddExample(
                        "./openapi.json",
                        "--optional-nullable-parameters");
            });

        return app.Run(args);
    }
}