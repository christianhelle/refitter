using System.ComponentModel;
using Refitter.Core;
using Spectre.Console;
using Spectre.Console.Cli;


var app = new CommandApp<GenerateCommand>();
return app.Run(args);

internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to OpenAPI Specification file")]
        [CommandArgument(0, "[openApiPath]")]
        public string? OpenApiPath { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var searchPath = settings.OpenApiPath ?? ".";
        
        var generator= new RefitGenerator();
        var code = await generator.Generate(searchPath);
        
        AnsiConsole.WriteLine(code);
        return 0;
    }
}