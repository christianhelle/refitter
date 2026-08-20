using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter;

/// <summary>
/// Reporter used with <c>--silent</c> (or <c>"silent": true</c> in a settings file).
/// Suppresses every piece of console output while still running the generation and
/// validation delegates wrapped by the progress methods. Errors are still surfaced
/// through the process exit code.
/// </summary>
internal sealed class SilentGenerationReporter : IGenerationReporter
{
    public void ReportHeader(string version)
    {
    }

    public void ReportSupportKey(string supportKey)
    {
    }

    public Task ReportSingleFileGenerationProgressAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public void ReportSingleFileOutput(string fileName, string directory, string sizeFormatted, int lines)
    {
    }

    public Task<GeneratorOutput> GenerateMultipleFilesWithProgressAsync(
        Func<GeneratorOutput> generate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(generate());
    }

    public IMultiFileOutputReport BeginMultiFileOutput() => new SilentMultiFileOutputReport();

    public void ReportFileWritten(string outputPath)
    {
    }

    public async Task<OpenApiValidationResult> ValidateWithProgressAsync(
        Func<Task<OpenApiValidationResult>> validate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await validate();
    }

    public void ReportValidationFailed()
    {
    }

    public void ReportValidationDiagnostic(OpenApiError error, bool isError)
    {
    }

    public void ReportValidationStatistics(OpenApiValidationResult validationResult)
    {
    }

    public void ReportSuccess(TimeSpan duration, bool multipleFiles)
    {
    }

    public void ReportDonationBanner()
    {
    }

    public void ReportConfigurationWarnings(IReadOnlyList<Warning> warnings)
    {
    }

    public void ReportAllPathsFilteredWarning(IReadOnlyList<string> matchPatterns)
    {
    }

    public void ReportSettingsFileGenerated(string settingsFilePath)
    {
    }

    public void ReportGenerationFailed()
    {
    }

    public void ReportUnsupportedVersion(string specificationVersion)
    {
    }

    public void ReportExceptionDetails(Exception exception)
    {
    }

    public void ReportSkipValidationSuggestion()
    {
    }

    public void ReportSupportHelp()
    {
    }

    private sealed class SilentMultiFileOutputReport : IMultiFileOutputReport
    {
        public void AddFile(string fileName, string directory, string sizeFormatted, int lines)
        {
        }

        public void Complete(int fileCount, string totalSizeFormatted, int totalLines)
        {
        }
    }
}
