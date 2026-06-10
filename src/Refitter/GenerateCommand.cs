using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Validation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Refitter;

[ExcludeFromCodeCoverage]
public sealed class GenerateCommand : AsyncCommand<Settings>
{
    internal const string GeneratedFileMarker = "GeneratedFile: ";

    private RefitGeneratorSettings? cachedSettings;
    private IGenerationReporter reporter = new RichGenerationReporter();

    private static IGenerationReporter CreateReporter(Settings settings) =>
        settings.SimpleOutput
            ? new SimpleGenerationReporter()
            : new RichGenerationReporter();

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!settings.NoLogging)
            Analytics.Configure();

        if (context.Arguments.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase)) ||
            context.Arguments.Any(a => a.Equals("-v", StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Success();

        // Cache settings if using settings file to avoid reading twice
        var validationResult = SettingsValidator.Validate(settings, out var refitSettings);
        if (refitSettings != null)
        {
            cachedSettings = refitSettings;
        }

        // Detect conflict: both CLI and settings file specify non-default jsonLibraryVersion
        if (settings.JsonLibraryVersion is { } cliValue &&
            cliValue != 8.0m &&
            refitSettings?.CodeGeneratorSettings?.JsonLibraryVersion is { } fileValue &&
            fileValue != 8.0m)
        {
            return ValidationResult.Error(
                "Cannot specify --json-library-version via CLI when the settings file also specifies a non-default value. Use only one source.");
        }

        return validationResult;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        RefitGeneratorSettings refitGeneratorSettings;

        reporter = CreateReporter(settings);

        try
        {
            // Use cached settings from Validate() if available
            if (cachedSettings != null)
            {
                refitGeneratorSettings = cachedSettings;
                cachedSettings = null; // Clear cache after use
            }
            else if (!string.IsNullOrWhiteSpace(settings.SettingsFilePath))
            {
                // Fallback: read settings file if not cached (shouldn't normally happen)
                var json = await File.ReadAllTextAsync(settings.SettingsFilePath);
                refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(json);

                // Allow CLI to override OpenApiPath if explicitly provided
                if (!string.IsNullOrWhiteSpace(settings.OpenApiPath))
                    refitGeneratorSettings.OpenApiPath = settings.OpenApiPath;

                ApplySettingsFileDefaults(settings.SettingsFilePath, refitGeneratorSettings);
            }
            else
            {
                // No settings file - build from CLI arguments
                refitGeneratorSettings = CreateRefitGeneratorSettings(settings);
            }
            var stopwatch = Stopwatch.StartNew();
            var version = GetType().Assembly.GetName().Version!.ToString();
            if (version == "1.0.0.0")
                version += " (local build)";

            // Header with branding
            reporter.ReportHeader(version);

            // Support information
            var supportKey = settings.NoLogging
                ? "Unavailable when logging is disabled"
                : SupportInformation.GetSupportKey();

            reporter.ReportSupportKey(supportKey);

            if (context.Arguments.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase)) ||
                context.Arguments.Any(a => a.Equals("-v", StringComparison.OrdinalIgnoreCase)))
            {
                return 0;
            }

            // Resolve relative paths from settings file directory before creating generator
            if (!string.IsNullOrWhiteSpace(settings.SettingsFilePath))
            {
                ResolveRelativeSpecPaths(settings.SettingsFilePath, refitGeneratorSettings);
            }

            var generator = await RefitGenerator.CreateAsync(refitGeneratorSettings);
            if (!settings.SkipValidation)
            {
                if (refitGeneratorSettings.OpenApiPaths == null || refitGeneratorSettings.OpenApiPaths.Length == 0)
                {
                    var specPath = refitGeneratorSettings.OpenApiPath!;
                    await ValidateOpenApiSpec(specPath, reporter);
                }
                else
                {
                    foreach (var specPath in refitGeneratorSettings.OpenApiPaths)
                    {
                        await ValidateOpenApiSpec(specPath, reporter);
                    }
                }
            }

            await (refitGeneratorSettings.GenerateMultipleFiles
                ? WriteMultipleFiles(generator, settings, refitGeneratorSettings)
                : WriteSingleFile(generator, settings, refitGeneratorSettings));

            Analytics.LogFeatureUsage(settings, refitGeneratorSettings);

            // Generate .refitter settings file if not using existing settings file
            if (string.IsNullOrWhiteSpace(settings.SettingsFilePath))
            {
                await WriteRefitterSettingsFile(settings, refitGeneratorSettings);
            }

            if (refitGeneratorSettings.IncludePathMatches.Length > 0 &&
                generator.OpenApiDocument.Paths.Count == 0)
            {
                reporter.ReportAllPathsFilteredWarning(refitGeneratorSettings.IncludePathMatches);
            }

            // Success summary with performance metrics
            stopwatch.Stop();
            reporter.ReportSuccess(stopwatch.Elapsed, refitGeneratorSettings.GenerateMultipleFiles);

            if (!settings.NoBanner)
            {
                reporter.ReportDonationBanner();
            }

            ShowWarnings(refitGeneratorSettings, settings);
            return 0;
        }
        catch (Exception exception)
        {
            // Error summary panel
            reporter.ReportGenerationFailed();

            if (exception is OpenApiUnsupportedSpecVersionException unsupportedSpecVersionException)
            {
                reporter.ReportUnsupportedVersion(unsupportedSpecVersionException.SpecificationVersion);
            }

            if (exception is not OpenApiValidationException)
            {
                reporter.ReportExceptionDetails(exception);
            }

            if (!settings.SkipValidation)
            {
                reporter.ReportSkipValidationSuggestion();
            }

            reporter.ReportSupportHelp();

            await Analytics.LogError(exception, settings);
            return exception.HResult;
        }
    }

    private static RefitGeneratorSettings CreateRefitGeneratorSettings(Settings settings)
    {
        settings.TryGetAuthenticationHeaderStyle(out var authenticationHeaderStyle, out _);

        return new RefitGeneratorSettings
        {
            OpenApiPath = settings.OpenApiPath!,
            Namespace = settings.Namespace ?? "GeneratedCode",
            PropertyNamingPolicy = settings.PropertyNamingPolicy,
            AddAutoGeneratedHeader = !settings.NoAutoGeneratedHeader,
            AddAcceptHeaders = !settings.NoAcceptHeaders,
            GenerateContracts = !settings.InterfaceOnly,
            GenerateClients = !settings.ContractOnly,
            ReturnIApiResponse = settings.ReturnIApiResponse,
            ReturnIObservable = settings.ReturnIObservable,
            UseCancellationTokens = settings.UseCancellationTokens,
            GenerateOperationHeaders = !settings.NoOperationHeaders,
            IgnoredOperationHeaders = settings.IgnoredOperationHeaders ?? Array.Empty<string>(),
            UseIsoDateFormat = settings.UseIsoDateFormat,
            TypeAccessibility = settings.InternalTypeAccessibility
                ? TypeAccessibility.Internal
                : TypeAccessibility.Public,
            AdditionalNamespaces = settings.AdditionalNamespaces!,
            ExcludeNamespaces = settings.ExcludeNamespaces ?? Array.Empty<string>(),
            MultipleInterfaces = settings.MultipleInterfaces,
            IncludePathMatches = settings.MatchPaths ?? Array.Empty<string>(),
            IncludeTags = settings.Tags ?? Array.Empty<string>(),
            GenerateDeprecatedOperations = !settings.NoDeprecatedOperations,
            OperationNameTemplate = settings.OperationNameTemplate,
            OptionalParameters = settings.OptionalNullableParameters,
            TrimUnusedSchema = settings.TrimUnusedSchema,
            KeepSchemaPatterns = settings.KeepSchemaPatterns ?? Array.Empty<string>(),
            IncludeInheritanceHierarchy = settings.IncludeInheritanceHierarchy,
            OperationNameGenerator = settings.OperationNameGenerator,
            GenerateDefaultAdditionalProperties = !settings.SkipDefaultAdditionalProperties,
            ImmutableRecords = settings.ImmutableRecords,
            ApizrSettings = settings.UseApizr ? new ApizrSettings() : null,
            UseDynamicQuerystringParameters = settings.UseDynamicQuerystringParameters,
            GenerateMultipleFiles = settings.GenerateMultipleFiles || !string.IsNullOrWhiteSpace(settings.ContractsOutputPath),
            ContractsOutputFolder = settings.ContractsOutputPath ?? settings.OutputPath,
            ContractsNamespace = settings.ContractsNamespace,
            UsePolymorphicSerialization = settings.UsePolymorphicSerialization,
            GenerateDisposableClients = settings.GenerateDisposableClients,
            CollectionFormat = settings.CollectionFormat,
            GenerateXmlDocCodeComments = !settings.NoXmlDocCodeComments,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                InlineJsonConverters = !settings.NoInlineJsonConverters,
                IntegerType = settings.IntegerType,
                JsonLibraryVersion = settings.JsonLibraryVersion ?? 8.0m,
            },
            CustomTemplateDirectory = settings.CustomTemplateDirectory,
            AuthenticationHeaderStyle = authenticationHeaderStyle,
            SecurityScheme = settings.SecurityScheme,
            GenerateJsonSerializerContext = settings.GenerateJsonSerializerContext,
        };
    }
    private async Task WriteSingleFile(
        RefitGenerator generator,
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        await reporter.ReportSingleFileGenerationProgressAsync();

        var code = generator.Generate().ReplaceLineEndings();
        var planned = OutputPlanner.PlanSingleFile(settings, refitGeneratorSettings, code);

        var fileName = Path.GetFileName(planned.Path);
        var directory = Path.GetDirectoryName(planned.Path) ?? "";
        var sizeFormatted = FormatFileSize(code.Length);
        var lines = code.Split('\n').Length;

        reporter.ReportSingleFileOutput(fileName, directory, sizeFormatted, lines);

        await FileWriter.WriteAsync(planned);
        reporter.ReportFileWritten(planned.Path);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }
    private async Task WriteMultipleFiles(
        RefitGenerator generator,
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        var generatorOutput = await reporter.GenerateMultipleFilesWithProgressAsync(generator.GenerateMultipleFiles);
        var planned = OutputPlanner.PlanMultipleFiles(settings, refitGeneratorSettings, generatorOutput);

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

            await FileWriter.WriteAsync(plannedFile);
            reporter.ReportFileWritten(plannedFile.Path);
        }

        report.Complete(generatorOutput.Files.Count, FormatFileSize(totalSize), totalLines);
    }

    private void ShowWarnings(RefitGeneratorSettings refitGeneratorSettings, Settings settings)
    {
        var warnings = new List<(string title, string description)>();

        if (refitGeneratorSettings.UseIsoDateFormat &&
            refitGeneratorSettings.CodeGeneratorSettings?.DateFormat is not null)
        {
            warnings.Add((
                "Date Format Override",
                "'codeGeneratorSettings.dateFormat' will be ignored due to 'useIsoDateFormat' set to true"
            ));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        if (refitGeneratorSettings.DependencyInjectionSettings?.UsePolly is true)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            warnings.Add((
                "Deprecated Setting",
                "The 'usePolly' property is deprecated. Use 'transientErrorHandler: Polly' instead"
            ));
        }

        if (warnings.Any())
        {
            reporter.ReportConfigurationWarnings(warnings);
        }
    }
    private static string GetOutputPath(Settings settings, RefitGeneratorSettings refitGeneratorSettings) =>
        OutputPlanner.GetSingleFileOutputPath(settings, refitGeneratorSettings);

    internal static void ApplySettingsFileDefaults(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        // Re-apply multi-file trigger logic
        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder))
            refitGeneratorSettings.GenerateMultipleFiles = true;

        // Default outputFolder to ./Generated if not specified (property initializer not invoked by JSON deserialization)
        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder))
        {
            refitGeneratorSettings.OutputFolder = RefitGeneratorSettings.DefaultOutputFolder;
        }

        // Default outputFilename to .refitter filename if not specified
        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFilename))
        {
            var refitterFileName = Path.GetFileNameWithoutExtension(settingsFilePath);
            if (string.IsNullOrEmpty(refitterFileName))
                refitterFileName = "Output";
            refitGeneratorSettings.OutputFilename = $"{refitterFileName}.cs";
        }
    }

    private string GetOutputPath(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile) =>
        OutputPlanner.GetMultiFileOutputPath(settings, refitGeneratorSettings, outputFile);

    internal static string FormatGeneratedFileMarker(string outputPath) =>
        $"{GeneratedFileMarker}{Path.GetFullPath(outputPath)}";

    private static async Task ValidateOpenApiSpec(string openApiPath, IGenerationReporter reporter)
    {
        var validationResult = await reporter.ValidateWithProgressAsync(
            () => Validation.OpenApiValidator.Validate(openApiPath));

        if (!validationResult.IsValid)
        {
            reporter.ReportValidationFailed();

            foreach (var error in validationResult.Diagnostics.Errors)
            {
                reporter.ReportValidationDiagnostic(error, isError: true);
            }

            foreach (var warning in validationResult.Diagnostics.Warnings)
            {
                reporter.ReportValidationDiagnostic(warning, isError: false);
            }

            validationResult.ThrowIfInvalid();
        }

        reporter.ReportValidationStatistics(validationResult);
    }

    internal static async Task WriteRefitterSettingsFile(Settings settings, RefitGeneratorSettings refitGeneratorSettings)
    {
        var settingsFilePath = DetermineSettingsFilePath(settings);
        var settingsDirectory = Path.GetDirectoryName(settingsFilePath);

        if (!string.IsNullOrWhiteSpace(settingsDirectory) && !Directory.Exists(settingsDirectory))
            Directory.CreateDirectory(settingsDirectory);

        var json = Serializer.Serialize(refitGeneratorSettings);
        await File.WriteAllTextAsync(settingsFilePath, json);

        CreateReporter(settings).ReportSettingsFileGenerated(settingsFilePath);
    }

    internal static string DetermineSettingsFilePath(Settings settings)
    {
        // If output path is specified and is a directory, put .refitter file there
        if (!string.IsNullOrWhiteSpace(settings.OutputPath) &&
            settings.OutputPath != Settings.DefaultOutputPath)
        {
            var outputDir = settings.GenerateMultipleFiles || !string.IsNullOrWhiteSpace(settings.ContractsOutputPath)
                ? settings.OutputPath
                : Path.GetDirectoryName(settings.OutputPath);

            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                return Path.Combine(outputDir, FileExtensionConstants.Refitter);
            }
        }

        // Default: put .refitter file in current directory
        return FileExtensionConstants.Refitter;
    }

    internal static void ResolveRelativeSpecPaths(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        var settingsFileDirectory = Path.GetDirectoryName(Path.GetFullPath(settingsFilePath)) ?? string.Empty;
        RefitterSettingsLoader.ResolveRelativeSpecPaths(refitGeneratorSettings, settingsFileDirectory);
    }

    internal static bool IsUrl(string path) =>
        RefitterSettingsLoader.IsUrl(path);
}
