using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Validation;

namespace Refitter;

/// <summary>
/// The console presentation seam for the generate command. Every piece of
/// human-facing output the CLI produces flows through one of these calls, so the
/// command/runner orchestration no longer branches on <c>--simple-output</c>.
/// Two adapters implement it: <see cref="SimpleGenerationReporter"/> (plain text,
/// machine-friendly) and <see cref="RichGenerationReporter"/> (Spectre.Console).
/// </summary>
public interface IGenerationReporter
{
    void ReportHeader(string version);

    void ReportSupportKey(string supportKey);

    Task ReportSingleFileGenerationProgressAsync();

    void ReportSingleFileOutput(string fileName, string directory, string sizeFormatted, int lines);

    Task<GeneratorOutput> GenerateMultipleFilesWithProgressAsync(Func<GeneratorOutput> generate);

    IMultiFileOutputReport BeginMultiFileOutput();

    /// <summary>
    /// Emits the machine-readable <c>GeneratedFile:</c> marker after a file is
    /// written. Only the simple reporter produces output; the rich reporter is a no-op.
    /// </summary>
    void ReportFileWritten(string outputPath);

    Task<OpenApiValidationResult> ValidateWithProgressAsync(Func<Task<OpenApiValidationResult>> validate);

    void ReportValidationFailed();

    void ReportValidationDiagnostic(OpenApiError error, bool isError);

    void ReportValidationStatistics(OpenApiValidationResult validationResult);

    void ReportSuccess(TimeSpan duration, bool multipleFiles);

    void ReportDonationBanner();

    void ReportConfigurationWarnings(IReadOnlyList<(string Title, string Description)> warnings);

    /// <summary>
    /// Emitted when every API path was filtered out by <c>--match-path</c> patterns.
    /// The output is identical in both reporters (plain text in each).
    /// </summary>
    void ReportAllPathsFilteredWarning(IReadOnlyList<string> matchPatterns);

    void ReportSettingsFileGenerated(string settingsFilePath);

    void ReportGenerationFailed();

    void ReportUnsupportedVersion(string specificationVersion);

    void ReportExceptionDetails(Exception exception);

    void ReportSkipValidationSuggestion();

    void ReportSupportHelp();
}

/// <summary>
/// Stateful sub-report for the multi-file output listing. The simple reporter
/// prints each row as it is added (interleaved with file writes); the rich
/// reporter buffers rows into a table rendered on <see cref="Complete"/>.
/// </summary>
public interface IMultiFileOutputReport
{
    void AddFile(string fileName, string directory, string sizeFormatted, int lines);

    void Complete(int fileCount, string totalSizeFormatted, int totalLines);
}
