using System.Diagnostics;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter;

public sealed class GenerationOrchestrator
{
    public async Task<int> RunAsync(
        RefitGeneratorSettings generatorSettings,
        Settings cliSettings,
        IGenerationReporter reporter,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            reporter.ReportHeader(VersionHelper.GetVersion());

            var supportKey = cliSettings.NoLogging
                ? "Unavailable when logging is disabled"
                : SupportInformation.GetSupportKey();

            reporter.ReportSupportKey(supportKey);

            string? settingsFilePath = cliSettings.SettingsFilePath;
            if (!string.IsNullOrWhiteSpace(settingsFilePath))
            {
                RefitterSettingsLoader.ResolveRelativeSpecPaths(
                    generatorSettings,
                    Path.GetDirectoryName(Path.GetFullPath(settingsFilePath)) ?? string.Empty);
            }

            var writer = new CliFileWriter(reporter);
            IValidator? validator = cliSettings.SkipValidation
                ? null
                : new OpenApiValidatorAdapter();

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                generatorSettings,
                writer,
                validator,
                settingsFilePath,
                cliSettings.OutputPath,
                cancellationToken);

            if (result.ExitCode != 0)
                throw result.Exception ?? new InvalidOperationException(
                    result.Diagnostics.FirstOrDefault()?.Message ?? "Unknown error");

            Analytics.LogFeatureUsage(cliSettings, generatorSettings);

            if (string.IsNullOrWhiteSpace(cliSettings.SettingsFilePath))
                await GenerateCommand.WriteRefitterSettingsFile(
                    cliSettings,
                    generatorSettings,
                    cancellationToken);

            if (result.Warnings.Count > 0)
                reporter.ReportConfigurationWarnings(result.Warnings);

            var filteredPaths = result.Diagnostics
                .FirstOrDefault(d => d.Message.Contains("All paths were filtered"));
            if (filteredPaths != null)
                reporter.ReportAllPathsFilteredWarning(generatorSettings.IncludePathMatches);

            stopwatch.Stop();
            reporter.ReportSuccess(stopwatch.Elapsed, generatorSettings.GenerateMultipleFiles);

            if (!cliSettings.NoBanner)
                reporter.ReportDonationBanner();

            return 0;
        }
        catch (Exception exception)
        {
            reporter.ReportGenerationFailed();

            if (exception is OpenApiUnsupportedSpecVersionException unsupportedSpecVersionException)
                reporter.ReportUnsupportedVersion(unsupportedSpecVersionException.SpecificationVersion);

            if (exception is not OpenApiValidationException)
                reporter.ReportExceptionDetails(exception);

            if (!cliSettings.SkipValidation)
                reporter.ReportSkipValidationSuggestion();

            reporter.ReportSupportHelp();

            await Analytics.LogError(exception, cliSettings);
            return exception.HResult;
        }
    }
}
