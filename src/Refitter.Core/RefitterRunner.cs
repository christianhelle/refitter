using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Refitter.Core.Validation;

namespace Refitter.Core;

/// <summary>
/// Encapsulates the shared generation workflow across all distribution forms (CLI, MSBuild, Source Generator).
/// Handles generator creation, code generation, output planning, validation, warning detection, and file writing.
/// </summary>
public class RefitterRunner
{
    /// <summary>
    /// Runs the complete generation workflow.
    /// </summary>
    /// <param name="settings">The generator settings.</param>
    /// <param name="writer">Optional file writer for writing output files. When null, files are not written.</param>
    /// <param name="validator">Optional validator for OpenAPI spec validation. When null, validation is skipped.</param>
    /// <param name="settingsFilePath">Optional path to the .refitter settings file, used for resolving relative output paths.</param>
    /// <param name="outputPath">Optional CLI output path override.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="RunResult"/> containing generated files, warnings, diagnostics, and the exit code.</returns>
    public async Task<RunResult> RunAsync(
        RefitGeneratorSettings settings,
        IFileWriter? writer = null,
        IValidator? validator = null,
        string? settingsFilePath = null,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<Warning>();
        var diagnostics = new List<RunnerDiagnostic>();

        try
        {
            var generator = await RefitGenerator.CreateAsync(
                settings,
                cancellationToken);

            var output = GenerateCode(generator, settings);

            var planner = new OutputPlannerAdapter();
            var plannedFiles = planner.Plan(
                output,
                settings,
                settingsFilePath,
                outputPath);

            if (validator != null)
            {
                await ValidateOpenApiSpecsAsync(
                    settings,
                    validator,
                    diagnostics,
                    cancellationToken);
            }

            if (writer != null)
            {
                await WriteFilesAsync(plannedFiles, writer, cancellationToken);
            }

            DetectWarnings(settings, warnings);

            DetectPathFilteringDiagnostics(settings, generator, diagnostics);

            stopwatch.Stop();

            return new RunResult(
                plannedFiles,
                warnings.AsReadOnly(),
                diagnostics.AsReadOnly(),
                stopwatch.Elapsed,
                ExitCode: 0);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var errorDiagnostics = new List<RunnerDiagnostic>(diagnostics)
            {
                new RunnerDiagnostic(ex.Message, IsError: true)
            };

            return new RunResult(
                Array.Empty<PlannedFile>(),
                warnings.AsReadOnly(),
                errorDiagnostics.AsReadOnly(),
                stopwatch.Elapsed,
                ex.HResult,
                ex);
        }
    }

    private static GeneratorOutput GenerateCode(RefitGenerator generator, RefitGeneratorSettings settings)
    {
        if (settings.GenerateMultipleFiles)
        {
            return generator.GenerateMultipleFiles();
        }
        else
        {
            var code = generator.Generate()
                .Replace("\r\n", "\n")
                .Replace("\n", "\r\n");
            return new GeneratorOutput(
                new List<GeneratedCode> { new(string.Empty, code) });
        }
    }

    /// <summary>
    /// Validates the configured OpenAPI specifications and records any diagnostics.
    /// </summary>
    /// <param name="settings">The generator settings that provide the OpenAPI paths to validate.</param>
    /// <param name="validator">The OpenAPI validator used to validate each specification.</param>
    /// <param name="diagnostics">The list that receives validation errors and warnings.</param>
    /// <param name="cancellationToken">The token used to cancel validation.</param>
    private static async Task ValidateOpenApiSpecsAsync(
        RefitGeneratorSettings settings,
        IValidator validator,
        List<RunnerDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var openApiPaths = GetOpenApiPaths(settings);
        foreach (var specPath in openApiPaths)
        {
            if (!string.IsNullOrWhiteSpace(specPath))
            {
                var validationResult = await validator.ValidateAsync(
                    specPath,
                    settings.AllowRemoteReferences,
                    cancellationToken);

                foreach (var error in validationResult.Diagnostics.Errors)
                    diagnostics.Add(new RunnerDiagnostic(error.Message, IsError: true));

                foreach (var warning in validationResult.Diagnostics.Warnings)
                    diagnostics.Add(new RunnerDiagnostic(warning.Message, IsError: false));

                validationResult.ThrowIfInvalid();
            }
        }
    }

    private static async Task WriteFilesAsync(
        IReadOnlyList<PlannedFile> plannedFiles,
        IFileWriter writer,
        CancellationToken cancellationToken)
    {
        foreach (var file in plannedFiles)
        {
            await writer.WriteAsync(file, cancellationToken);
        }
    }

    private static void DetectPathFilteringDiagnostics(
        RefitGeneratorSettings settings,
        RefitGenerator generator,
        List<RunnerDiagnostic> diagnostics)
    {
        if (settings.IncludePathMatches.Length > 0 &&
            generator.OpenApiDocument.Paths.Count == 0)
        {
            diagnostics.Add(new RunnerDiagnostic(
                $"All paths were filtered out by include path patterns: [{string.Join(", ", settings.IncludePathMatches)}]",
                IsError: false));
        }
    }

    private static string[] GetOpenApiPaths(RefitGeneratorSettings settings)
    {
        if (settings.OpenApiPaths is { Length: > 0 })
            return settings.OpenApiPaths;

        if (settings.OpenApiPath is not null)
            return new[] { settings.OpenApiPath };

        return Array.Empty<string>();
    }

    /// <summary>
    /// Detects configuration warnings in the settings and adds them to the provided list.
    /// </summary>
    internal static void DetectWarnings(
        RefitGeneratorSettings settings,
        List<Warning> warnings)
    {
        if (settings is { UseIsoDateFormat: true, CodeGeneratorSettings.DateFormat: not null })
        {
            warnings.Add(
                new Warning(
                    "Date Format Override",
                    "'codeGeneratorSettings.dateFormat' will be ignored due to 'useIsoDateFormat' set to true"));
        }

#pragma warning disable CS0618
        if (settings.DependencyInjectionSettings?.UsePolly is true)
#pragma warning restore CS0618
        {
            warnings.Add(
                new Warning(
                    "Deprecated Setting",
                    "The 'usePolly' property is deprecated. Use 'transientErrorHandler: Polly' instead"));
        }
    }
}
