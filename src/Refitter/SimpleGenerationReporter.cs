using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter;

/// <summary>
/// Plain-text reporter used with <c>--simple-output</c>. Produces deterministic,
/// machine-parseable console output (including the <c>GeneratedFile:</c> markers).
/// Every method mirrors the original simple-output branch of the generate command.
/// </summary>
internal sealed class SimpleGenerationReporter : IGenerationReporter
{
    private static readonly string Crlf = Environment.NewLine;

    public void ReportHeader(string version)
    {
        Console.WriteLine();
        Console.WriteLine($"Refitter v{version}");
        Console.WriteLine("OpenAPI to Refit Interface Generator");
        Console.WriteLine();
    }

    public void ReportSupportKey(string supportKey)
    {
        Console.WriteLine($"Support key: {supportKey}");
        Console.WriteLine();
    }

    public Task ReportSingleFileGenerationProgressAsync()
    {
        Console.WriteLine("Generating code...");
        return Task.CompletedTask;
    }

    public void ReportSingleFileOutput(string fileName, string directory, string sizeFormatted, int lines)
    {
        Console.WriteLine("Generated Output");
        Console.WriteLine($"File: {fileName}");
        Console.WriteLine($"Directory: {directory}");
        Console.WriteLine($"Size: {sizeFormatted}");
        Console.WriteLine($"Lines: {lines:N0}");
        Console.WriteLine();
    }

    public Task<GeneratorOutput> GenerateMultipleFilesWithProgressAsync(Func<GeneratorOutput> generate)
    {
        Console.WriteLine("Generating multiple files...");
        return Task.FromResult(generate());
    }

    public IMultiFileOutputReport BeginMultiFileOutput() => new SimpleMultiFileOutputReport();

    public void ReportFileWritten(string outputPath) =>
        Console.WriteLine(GenerateCommand.FormatGeneratedFileMarker(outputPath));

    public async Task<OpenApiValidationResult> ValidateWithProgressAsync(Func<Task<OpenApiValidationResult>> validate)
    {
        Console.WriteLine("Validating OpenAPI specification...");
        return await validate();
    }

    public void ReportValidationFailed()
    {
        Console.WriteLine();
        Console.WriteLine("OpenAPI validation failed!");
        Console.WriteLine();
    }

    public void ReportValidationDiagnostic(OpenApiError error, bool isError)
    {
        var label = isError ? "Error" : "Warning";
        Console.WriteLine($"{label}:{Crlf}{error}{Crlf}");
    }

    public void ReportValidationStatistics(OpenApiValidationResult validationResult)
    {
        Console.WriteLine("OpenAPI Analysis Results");
        foreach (var (label, value) in OpenApiStatisticsFormatter.Parse(validationResult.Statistics.ToString()))
        {
            var description = OpenApiStatisticsFormatter.GetDescription(label);
            Console.WriteLine($"{label}: {value} - {description}");
        }

        Console.WriteLine();
    }

    public void ReportSuccess(TimeSpan duration, bool multipleFiles)
    {
        Console.WriteLine("Generation completed successfully!");
        Console.WriteLine($"Duration: {duration:mm\\:ss\\.ffff}");
        Console.WriteLine($"Performance: {(multipleFiles ? "Multi-file" : "Single-file")} generation");
        Console.WriteLine();
    }

    public void ReportDonationBanner()
    {
        Console.WriteLine("Support");
        Console.WriteLine("Enjoying Refitter? Consider supporting the project!");
        Console.WriteLine();
        Console.WriteLine("Sponsor: https://github.com/sponsors/christianhelle");
        Console.WriteLine("Buy me a coffee: https://www.buymeacoffee.com/christianhelle");
        Console.WriteLine();
        Console.WriteLine("Found an issue? https://github.com/christianhelle/refitter/issues");
        Console.WriteLine();
    }

    public void ReportConfigurationWarnings(IReadOnlyList<(string Title, string Description)> warnings)
    {
        Console.WriteLine("Configuration Warnings");
        foreach (var (title, description) in warnings)
        {
            Console.WriteLine($"Warning: {title}");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine();
        }
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
        Console.WriteLine($"Settings file written to: {settingsFilePath}");
        Console.WriteLine();
    }

    public void ReportGenerationFailed()
    {
        Console.WriteLine("Generation failed!");
        Console.WriteLine();
    }

    public void ReportUnsupportedVersion(string specificationVersion)
    {
        Console.WriteLine($"Unsupported OpenAPI version: {specificationVersion}");
        Console.WriteLine();
    }

    public void ReportExceptionDetails(Exception exception)
    {
        Console.WriteLine("Exception Details:");
        Console.WriteLine(exception.ToString());
        Console.WriteLine();
    }

    public void ReportSkipValidationSuggestion()
    {
        Console.WriteLine("Suggestion");
        Console.WriteLine("Try using the --skip-validation argument.");
        Console.WriteLine();
    }

    public void ReportSupportHelp()
    {
        Console.WriteLine("Support");
        Console.WriteLine("Need Help?");
        Console.WriteLine();
        Console.WriteLine("Report an issue: https://github.com/christianhelle/refitter/issues");
        Console.WriteLine();
    }

    private sealed class SimpleMultiFileOutputReport : IMultiFileOutputReport
    {
        public SimpleMultiFileOutputReport()
        {
            Console.WriteLine("Generated Output Files");
            Console.WriteLine($"{"File",-30} {"Size",-10} {"Lines",-10}");
            Console.WriteLine(new string('-', 55));
        }

        public void AddFile(string fileName, string directory, string sizeFormatted, int lines) =>
            Console.WriteLine($"{fileName,-30} {sizeFormatted,-10} {lines,-10:N0}");

        public void Complete(int fileCount, string totalSizeFormatted, int totalLines)
        {
            Console.WriteLine(new string('-', 55));
            Console.WriteLine($"{"Total (" + fileCount + " files)",-30} {totalSizeFormatted,-10} {totalLines,-10:N0}");
            Console.WriteLine();
        }
    }
}
