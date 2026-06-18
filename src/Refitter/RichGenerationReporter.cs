using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;
using Spectre.Console;

namespace Refitter;

/// <summary>
/// Spectre.Console reporter used for the default rich CLI experience: ASCII-art
/// banner, spinners, panels, and tables. Every method mirrors the original
/// rich-output branch of the generate command.
/// </summary>
internal sealed class RichGenerationReporter : IGenerationReporter
{
    private static readonly string Crlf = Environment.NewLine;

    private const string AsciiArt =
"""
  ██████╗ ███████╗███████╗██╗████████╗████████╗███████╗██████╗
  ██╔══██╗██╔════╝██╔════╝██║╚══██╔══╝╚══██╔══╝██╔════╝██╔══██╗
  ██████╔╝█████╗  █████╗  ██║   ██║      ██║   █████╗  ██████╔╝
  ██╔══██╗██╔══╝  ██╔══╝  ██║   ██║      ██║   ██╔══╝  ██╔══██╗
  ██║  ██║███████╗██║     ██║   ██║      ██║   ███████╗██║  ██║
  ╚═╝  ╚═╝╚══════╝╚═╝     ╚═╝   ╚═╝      ╚═╝   ╚══════╝╚═╝  ╚═╝
""";

    public void ReportHeader(string version)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold cyan]{AsciiArt}[/]");
        AnsiConsole.MarkupLine($"[bold cyan]╔═══════════════════════════════════════════════════════════════╗[/]");
        AnsiConsole.MarkupLine($"[bold cyan]║[/] [bold white]🚀 Refitter v{version,-48}[/] [bold cyan]║[/]");
        AnsiConsole.MarkupLine($"[bold cyan]║[/] [dim]   OpenAPI to Refit Interface Generator[/]{new string(' ', 22)} [bold cyan]║[/]");
        AnsiConsole.MarkupLine($"[bold cyan]╚═══════════════════════════════════════════════════════════════╝[/]");
        AnsiConsole.WriteLine();
    }

    public void ReportSupportKey(string supportKey)
    {
        AnsiConsole.MarkupLine($"[dim]🔑 Support key: {supportKey}[/]");
        AnsiConsole.WriteLine();
    }

    public async Task ReportSingleFileGenerationProgressAsync(CancellationToken cancellationToken = default)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync("[yellow]🔧 Generating code...[/]", async _ =>
            {
                await Task.Delay(100, cancellationToken); // Brief delay to show spinner
            });
    }

    public void ReportSingleFileOutput(string fileName, string directory, string sizeFormatted, int lines)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Yellow)
            .AddColumn(new TableColumn("[bold yellow]📁 Generated Output[/]").Centered());

        table.AddRow($"[bold white]📄 File:[/] [cyan]{fileName}[/]");
        table.AddRow($"[bold white]📂 Directory:[/] [dim]{directory}[/]");
        table.AddRow($"[bold white]📊 Size:[/] [green]{sizeFormatted}[/]");
        table.AddRow($"[bold white]📝 Lines:[/] [green]{lines:N0}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public async Task<GeneratorOutput> GenerateMultipleFilesWithProgressAsync(
        Func<GeneratorOutput> generate,
        CancellationToken cancellationToken = default)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync("[yellow]🔧 Generating multiple files...[/]", async _ =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken); // Brief delay to show spinner
                return generate();
            });
    }

    public IMultiFileOutputReport BeginMultiFileOutput() => new RichMultiFileOutputReport();

    public void ReportFileWritten(string outputPath)
    {
        // Rich output does not emit the machine-readable marker.
    }

    public async Task<OpenApiValidationResult> ValidateWithProgressAsync(
        Func<Task<OpenApiValidationResult>> validate,
        CancellationToken cancellationToken = default)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan bold"))
            .StartAsync("[cyan]🔍 Validating OpenAPI specification...[/]", async _ =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await validate();
            });
    }

    public void ReportValidationFailed()
    {
        AnsiConsole.WriteLine();
        var errorPanel = new Panel("[red]❌ OpenAPI validation failed![/]")
            .BorderColor(Color.Red)
            .RoundedBorder();
        AnsiConsole.Write(errorPanel);
        AnsiConsole.WriteLine();
    }

    public void ReportValidationDiagnostic(OpenApiError error, bool isError)
    {
        var color = isError ? "red" : "yellow";
        var label = isError ? "Error" : "Warning";

        try
        {
            AnsiConsole.MarkupLine($"[{color}]{label}:{Crlf}{error}{Crlf}[/]");
        }
        catch
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color switch
            {
                "red" => ConsoleColor.Red,
                "yellow" => ConsoleColor.Yellow,
                _ => originalColor
            };

            Console.WriteLine($"{label}:{Crlf}{error}{Crlf}");

            Console.ForegroundColor = originalColor;
        }
    }

    public void ReportValidationStatistics(OpenApiValidationResult validationResult)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Blue)
            .AddColumn(new TableColumn("[bold white]📊 Metric[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold white]📈 Count[/]").RightAligned())
            .AddColumn(new TableColumn("[bold white]📝 Details[/]").LeftAligned());

        table.Title = new TableTitle("[bold cyan]📊 OpenAPI Analysis Results[/]");

        foreach (var (label, value) in OpenApiStatisticsFormatter.Parse(validationResult.Statistics.ToString()))
        {
            var icon = OpenApiStatisticsFormatter.GetIcon(label);
            var description = OpenApiStatisticsFormatter.GetDescription(label);

            table.AddRow(
                $"{icon} [bold]{label}[/]",
                $"[green]{value}[/]",
                $"[dim]{description}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public void ReportSuccess(TimeSpan duration, bool multipleFiles)
    {
        var successPanel = new Panel(
            $"[bold green]✅ Generation completed successfully![/]\n\n" +
            $"[dim]📊 Duration:[/] [green]{duration:mm\\:ss\\.ffff}[/]\n" +
            $"[dim]🚀 Performance:[/] [green]{(multipleFiles ? "Multi-file" : "Single-file")} generation[/]"
        )
        .BorderColor(Color.Green)
        .RoundedBorder()
        .Header("[bold green]🎉 Success[/]")
        .HeaderAlignment(Justify.Center);

        AnsiConsole.Write(successPanel);
        AnsiConsole.WriteLine();
    }

    public void ReportDonationBanner()
    {
        var panel = new Panel(
            "[yellow]💖 [bold]Enjoying Refitter?[/] Consider supporting the project![/]\n\n" +
            "[cyan]🎯 Sponsor:[/] [link]https://github.com/sponsors/christianhelle[/]\n" +
            "[yellow]☕ Buy me a coffee:[/] [link]https://www.buymeacoffee.com/christianhelle[/]\n\n" +
            "[red]🐛 Found an issue?[/] [link]https://github.com/christianhelle/refitter/issues[/]"
        )
        .BorderColor(Color.Grey)
        .RoundedBorder()
        .Header("[bold dim]💝 Support[/]")
        .HeaderAlignment(Justify.Center);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public void ReportConfigurationWarnings(IReadOnlyList<Warning> warnings)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Orange3)
            .AddColumn(new TableColumn("[bold white]Warning[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold white]Description[/]").LeftAligned());

        table.Title = new("[bold yellow]⚠️ Configuration Warnings[/]");

        foreach ((string title, string description) in warnings)
        {
            table.AddRow(
                $"[bold orange3]{title}[/]",
                $"[orange3]{description}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public void ReportAllPathsFilteredWarning(IReadOnlyList<string> matchPatterns)
    {
        Console.WriteLine("⚠️ WARNING: All API paths were filtered out by --match-path patterns. ⚠️");
        Console.WriteLine($"   Match Patterns used: {string.Join(", ", matchPatterns)}");
        Console.WriteLine();
        Console.WriteLine("   This could indicate that:");
        Console.WriteLine("     1. The regex patterns don't match any available paths");
        Console.WriteLine("     2. There's a syntax error in the regex patterns");
        Console.WriteLine("     3. The patterns were corrupted by command line interpretation");
        Console.WriteLine();
        Console.WriteLine("   This commonly happens when using the Windows Command Prompt (CMD) instead of PowerShell.");
        Console.WriteLine("   The ^ character in regex patterns is interpreted as an escape character in CMD.");
        Console.WriteLine();
        Console.WriteLine("   Solutions:");
        Console.WriteLine("     1. Use PowerShell instead of CMD");
        Console.WriteLine("     2. In CMD, escape the ^ character or use different quoting");
        Console.WriteLine("     3. Use a .refitter settings file instead of command line arguments");
        Console.WriteLine();
    }

    public void ReportSettingsFileGenerated(string settingsFilePath)
    {
        var fileName = Path.GetFileName(settingsFilePath);
        var directory = Path.GetDirectoryName(settingsFilePath) ?? "";

        var panel = new Panel(
            $"[bold white]📄 File:[/] [cyan]{fileName}[/]\n" +
            $"[bold white]📂 Directory:[/] [dim]{directory}[/]"
        )
        .BorderColor(Color.Green)
        .RoundedBorder()
        .Header("[bold green]💾 Settings File Generated[/]")
        .HeaderAlignment(Justify.Center);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public void ReportGenerationFailed()
    {
        var errorPanel = new Panel("[bold red]❌ Generation failed![/]")
            .BorderColor(Color.Red)
            .RoundedBorder()
            .Header("[bold red]🚨 Error[/]")
            .HeaderAlignment(Justify.Center);

        AnsiConsole.Write(errorPanel);
        AnsiConsole.WriteLine();
    }

    public void ReportUnsupportedVersion(string specificationVersion)
    {
        var versionPanel = new Panel(
            $"[bold red]🚫 Unsupported OpenAPI version: {specificationVersion}[/]"
        )
        .BorderColor(Color.Red)
        .RoundedBorder();

        AnsiConsole.Write(versionPanel);
        AnsiConsole.WriteLine();
    }

    public void ReportExceptionDetails(Exception exception)
    {
        AnsiConsole.MarkupLine("[bold red]🐛 Exception Details:[/]");
        AnsiConsole.WriteException(exception);
        AnsiConsole.WriteLine();
    }

    public void ReportSkipValidationSuggestion()
    {
        var suggestionPanel = new Panel(
            "💡 Try using the --skip-validation argument."
        )
        .BorderColor(Color.Yellow)
        .RoundedBorder()
        .Header("Suggestion");

        AnsiConsole.Write(suggestionPanel);
        AnsiConsole.WriteLine();
    }

    public void ReportSupportHelp()
    {
        var helpPanel = new Panel(
            "🆘 Need Help?\n\n" +
            "🐛 Report an issue: https://github.com/christianhelle/refitter/issues"
        )
        .BorderColor(Color.Yellow)
        .RoundedBorder()
        .Header("Support")
        .HeaderAlignment(Justify.Center);

        AnsiConsole.Write(helpPanel);
        AnsiConsole.WriteLine();
    }

    private sealed class RichMultiFileOutputReport : IMultiFileOutputReport
    {
        private readonly Table table;

        public RichMultiFileOutputReport()
        {
            table = new Table()
                .RoundedBorder()
                .BorderColor(Color.Yellow)
                .AddColumn(new TableColumn("[bold white]📄 File[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold white]📂 Directory[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold white]📊 Size[/]").RightAligned())
                .AddColumn(new TableColumn("[bold white]📝 Lines[/]").RightAligned());

            table.Title = new TableTitle("[bold yellow]📁 Generated Output Files[/]");
        }

        public void AddFile(string fileName, string directory, string sizeFormatted, int lines) =>
            table.AddRow(
                $"[cyan]{fileName}[/]",
                $"[dim]{directory}[/]",
                $"[green]{sizeFormatted}[/]",
                $"[green]{lines:N0}[/]"
            );

        public void Complete(int fileCount, string totalSizeFormatted, int totalLines)
        {
            table.AddEmptyRow();
            table.AddRow(
                $"[bold yellow]📊 Total ({fileCount} files)[/]",
                "[dim]---[/]",
                $"[bold green]{totalSizeFormatted}[/]",
                $"[bold green]{totalLines:N0}[/]"
            );

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }
}
