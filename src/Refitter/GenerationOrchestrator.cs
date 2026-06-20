using System.Diagnostics;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter;

public sealed class GenerationOrchestrator
{
    private readonly IOutputPlanner _planner;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationOrchestrator"/> class
    /// with the default <see cref="OutputPlannerAdapter"/>.
    /// </summary>
    public GenerationOrchestrator()
        : this(new OutputPlannerAdapter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationOrchestrator"/> class.
    /// </summary>
    /// <param name="planner">The output planner to use for resolving file paths.</param>
    public GenerationOrchestrator(IOutputPlanner planner)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

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

            if (!string.IsNullOrWhiteSpace(cliSettings.SettingsFilePath))
            {
                RefitterSettingsLoader.ResolveRelativeSpecPaths(
                    generatorSettings,
                    Path.GetDirectoryName(Path.GetFullPath(cliSettings.SettingsFilePath!)) ?? string.Empty);
            }

            var generator = await RefitGenerator.CreateAsync(
                generatorSettings,
                cancellationToken);

            if (!cliSettings.SkipValidation)
                await ValidateOpenApiSpecs(generatorSettings, reporter, cancellationToken);

            var writer = new CliFileWriter(reporter);

            await (generatorSettings.GenerateMultipleFiles
                ? WriteMultipleFiles(generator, cliSettings, generatorSettings, reporter, writer, cancellationToken)
                : WriteSingleFile(generator, cliSettings, generatorSettings, reporter, writer, cancellationToken));

            Analytics.LogFeatureUsage(cliSettings, generatorSettings);

            if (string.IsNullOrWhiteSpace(cliSettings.SettingsFilePath))
                await GenerateCommand.WriteRefitterSettingsFile(
                    cliSettings,
                    generatorSettings,
                    cancellationToken);

            if (generatorSettings.IncludePathMatches.Length > 0 &&
                generator.OpenApiDocument.Paths.Count == 0)
            {
                reporter.ReportAllPathsFilteredWarning(generatorSettings.IncludePathMatches);
            }

            stopwatch.Stop();
            reporter.ReportSuccess(stopwatch.Elapsed, generatorSettings.GenerateMultipleFiles);

            if (!cliSettings.NoBanner)
                reporter.ReportDonationBanner();

            ShowWarnings(generatorSettings, reporter);
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

    private async Task WriteSingleFile(
        RefitGenerator generator,
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        IGenerationReporter reporter,
        IFileWriter writer,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await reporter.ReportSingleFileGenerationProgressAsync(cancellationToken);

        var code = generator.Generate().ReplaceLineEndings();
        var output = new GeneratorOutput(
            new List<GeneratedCode> { new(string.Empty, code) });

        var planned = _planner.Plan(
            output,
            refitGeneratorSettings,
            settings.SettingsFilePath,
            settings.OutputPath);

        var plannedFile = planned[0];
        var fileName = Path.GetFileName(plannedFile.Path);
        var directory = Path.GetDirectoryName(plannedFile.Path) ?? string.Empty;
        var sizeFormatted = FormatFileSize(code.Length);
        var lines = code.Split('\n').Length;

        reporter.ReportSingleFileOutput(fileName, directory, sizeFormatted, lines);

        await writer.WriteAsync(plannedFile, cancellationToken);
    }

    private async Task WriteMultipleFiles(
        RefitGenerator generator,
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        IGenerationReporter reporter,
        IFileWriter writer,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var generatorOutput = await reporter.GenerateMultipleFilesWithProgressAsync(
            generator.GenerateMultipleFiles,
            cancellationToken);

        var planned = _planner.Plan(
            generatorOutput,
            refitGeneratorSettings,
            settings.SettingsFilePath,
            settings.OutputPath);

        var totalSize = 0L;
        var totalLines = 0;
        var report = reporter.BeginMultiFileOutput();

        for (var i = 0; i < generatorOutput.Files.Count; i++)
        {
            var outputFile = generatorOutput.Files[i];
            var plannedFile = planned[i];

            var size = outputFile.Content.Length;
            var lines = outputFile.Content.Split('\n').Length;
            var directory = Path.GetDirectoryName(plannedFile.Path) ?? "";

            report.AddFile(outputFile.Filename, directory, FormatFileSize(size), lines);

            totalSize += size;
            totalLines += lines;

            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteAsync(plannedFile, cancellationToken);
        }

        report.Complete(generatorOutput.Files.Count, FormatFileSize(totalSize), totalLines);
    }

    private static string FormatFileSize(long bytes)
    {
        var suffixes = new[]
        {
            "B", "KB", "MB", "GB"
        };
        var size = (double)bytes;
        var suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }

    private static async Task ValidateOpenApiSpecs(
        RefitGeneratorSettings refitGeneratorSettings,
        IGenerationReporter reporter,
        CancellationToken cancellationToken)
    {
        if (refitGeneratorSettings.OpenApiPaths is { Length: > 0 })
        {
            foreach (var specPath in refitGeneratorSettings.OpenApiPaths)
                await ValidateOpenApiSpec(specPath, reporter, cancellationToken);
        }
        else if (refitGeneratorSettings.OpenApiPath is not null)
        {
            await ValidateOpenApiSpec(refitGeneratorSettings.OpenApiPath, reporter, cancellationToken);
        }
    }

    private static async Task ValidateOpenApiSpec(
        string openApiPath,
        IGenerationReporter reporter,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validationResult = await reporter.ValidateWithProgressAsync(
            () => Core.Validation.OpenApiValidator.Validate(openApiPath, cancellationToken),
            cancellationToken);

        if (!validationResult.IsValid)
        {
            reporter.ReportValidationFailed();

            foreach (var error in validationResult.Diagnostics.Errors)
                reporter.ReportValidationDiagnostic(error, isError: true);

            foreach (var warning in validationResult.Diagnostics.Warnings)
                reporter.ReportValidationDiagnostic(warning, isError: false);

            validationResult.ThrowIfInvalid();
        }

        reporter.ReportValidationStatistics(validationResult);
    }

    private static void ShowWarnings(
        RefitGeneratorSettings refitGeneratorSettings,
        IGenerationReporter reporter)
    {
        var warnings = new List<Warning>();

        if (refitGeneratorSettings is { UseIsoDateFormat: true, CodeGeneratorSettings.DateFormat: not null })
        {
            warnings.Add(
                new (
                    "Date Format Override",
                    "'codeGeneratorSettings.dateFormat' will be ignored due to 'useIsoDateFormat' set to true"
                ));
        }

#pragma warning disable CS0618
        if (refitGeneratorSettings.DependencyInjectionSettings?.UsePolly is true)
#pragma warning restore CS0618
        {
            warnings.Add(
                new (
                    "Deprecated Setting",
                    "The 'usePolly' property is deprecated. Use 'transientErrorHandler: Polly' instead"
                ));
        }

        if (warnings.Count > 0)
            reporter.ReportConfigurationWarnings(warnings);
    }
}
