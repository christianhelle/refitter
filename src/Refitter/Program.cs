using Refitter;
using Spectre.Console.Cli;


Analytics.Configure();

var app = new CommandApp<GenerateCommand>();
app.Configure(
    config =>
    {
        var configuration = config
            .SetApplicationName("refitter")
            .SetApplicationVersion(typeof(GenerateCommand).Assembly.GetName().Version!.ToString());

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                });

        configuration
            .AddExample(
                new[]
                {
                    "https://petstore3.swagger.io/api/v3/openapi.yaml"
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--namespace",
                    "\"Your.Namespace.Of.Choice.GeneratedCode\"",
                    "--output",
                    "./GeneratedCode.cs"
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--namespace",
                    "\"Your.Namespace.Of.Choice.GeneratedCode\"",
                    "--internal"
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--output",
                    "./IGeneratedCode.cs",
                    "--interface-only"
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--use-api-response"
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--cancellation-tokens"
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--no-operation-headers"
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--use-iso-date-format"
                });

        configuration
            .AddExample(
                new[]
                {
                     "./openapi.json",
                     "--additional-namespace",
                     "\"Your.Additional.Namespace\"",
                     "--additional-namespace",
                     "\"Your.Other.Additional.Namespace\"",
                });

        configuration
            .AddExample(
                new[]
                {
                    "./openapi.json",
                    "--multiple-interfaces"
                });
    });

return app.Run(args);