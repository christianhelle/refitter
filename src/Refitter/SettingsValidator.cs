using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Refitter.Core;
using Spectre.Console;

namespace Refitter;

[ExcludeFromCodeCoverage(Justification = "CLI validation logic with many edge-case branches dependent on file system and user input")]
public static class SettingsValidator
{
    public static ValidationResult Validate(Settings settings)
    {
        return Validate(settings, out _);
    }

    public static ValidationResult Validate(Settings settings, out RefitGeneratorSettings? refitSettings)
    {
        refitSettings = null;

        if (BothSettingsFilesAreEmpty(settings) || BothSettingsFilesArePresent(settings))
        {
            return GetValidationErrorForSettingsFiles();
        }

        if (!string.IsNullOrWhiteSpace(settings.SettingsFilePath))
        {
            return ValidateFilePath(settings, out refitSettings);
        }

        return ValidateOperationNameAndUrl(settings);
    }

    private static bool BothSettingsFilesAreEmpty(Settings settings)
    {
        return string.IsNullOrWhiteSpace(settings.OpenApiPath) &&
               string.IsNullOrWhiteSpace(settings.SettingsFilePath);
    }

    private static bool BothSettingsFilesArePresent(Settings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.OpenApiPath) &&
               !string.IsNullOrWhiteSpace(settings.SettingsFilePath);
    }

    private static ValidationResult GetValidationErrorForSettingsFiles()
    {
        return ValidationResult.Error(
            "You should either specify an input URL/file directly " +
            "or use specify it in 'openApiPath' from the settings file, " +
            "not both");
    }

    private static ValidationResult ValidateFilePath(Settings settings, out RefitGeneratorSettings? refitSettings)
    {
        var json = File.ReadAllText(settings.SettingsFilePath!);

        RefitGeneratorSettings refitGeneratorSettings;
        try
        {
            refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(json);
        }
        catch (JsonException ex)
        {
            // Provide helpful error message for enum deserialization failures
            refitSettings = null;
            var enumName = ExtractEnumNameFromException(ex);
            return ValidationResult.Error(
                $"Invalid value in settings file: {ex.Message}\n\n" +
                $"Common causes:\n" +
                $"  - Invalid enum value for {enumName}\n" +
                $"  - Check that enum values match exactly (case-sensitive)\n" +
                $"  - See documentation for valid values: https://refitter.github.io");
        }

        refitSettings = refitGeneratorSettings;

        // First validate the file/output settings (includes check for both OpenApiPath and OpenApiPaths)
        var fileAndOutputResult = ValidateFileAndOutputSettings(settings, refitGeneratorSettings);
        if (!fileAndOutputResult.Successful)
        {
            return fileAndOutputResult;
        }

        var openApiValidationResult = ValidateAndResolveOpenApiSpecPaths(settings, refitGeneratorSettings);
        if (!openApiValidationResult.Successful)
        {
            return openApiValidationResult;
        }

        // Apply defaults before returning cached settings
        GenerateCommand.ApplySettingsFileDefaults(settings.SettingsFilePath!, refitGeneratorSettings);

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateFileAndOutputSettings(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        // Check if both OpenApiPath and OpenApiPaths are explicitly set in settings file
        var hasOpenApiPath = !string.IsNullOrWhiteSpace(refitGeneratorSettings.OpenApiPath);
        var hasOpenApiPaths = refitGeneratorSettings.OpenApiPaths?.Length > 0;

        if (hasOpenApiPath && hasOpenApiPaths)
        {
            return ValidationResult.Error(
                "Cannot specify both 'openApiPath' and 'openApiPaths' in settings file. " +
                "Use 'openApiPath' for a single specification or 'openApiPaths' for multiple specifications.");
        }

        if (!hasOpenApiPath && !hasOpenApiPaths)
        {
            return GetValidationErrorForOpenApiPath();
        }

        // Only validate output path conflict if BOTH folder AND filename are set in settings file
        // CLI --output should be able to override just the folder or just the filename
        if (!string.IsNullOrWhiteSpace(settings.OutputPath) &&
            settings.OutputPath != Settings.DefaultOutputPath &&
            !string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder) &&
            !string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFilename))
        {
            return GetValidationErrorForOutputPath();
        }

        return ValidateOperationNameAndUrl(settings);
    }

    private static ValidationResult GetValidationErrorForOpenApiPath()
    {
        return ValidationResult.Error(
            "The 'openApiPath' or 'openApiPaths' in settings file is required when " +
            "URL or file path to OpenAPI Specification file " +
            "is not specified in command line argument");
    }

    private static ValidationResult GetValidationErrorForOutputPath()
    {
        return ValidationResult.Error(
            "You should either specify an output path directly from --output " +
            "or use specify it in 'outputFolder' and 'outputFilename' from the settings file, " +
            "not both");
    }

    private static ValidationResult ValidateOperationNameAndUrl(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OperationNameTemplate) &&
            !settings.OperationNameTemplate.Contains("{operationName}") &&
            settings.MultipleInterfaces != MultipleInterfaces.ByEndpoint)
        {
            return GetValidationErrorForOperationName();
        }

        // If we have a single path, validate it
        if (!string.IsNullOrWhiteSpace(settings.OpenApiPath))
        {
            return SettingsFilePathResolver.IsUrl(settings.OpenApiPath) ? ValidationResult.Success() : ValidateFileExistence(settings);
        }

        // For settings file path, the validator would have already loaded and set settings.OpenApiPath
        return ValidationResult.Success();
    }

    private static ValidationResult GetValidationErrorForOperationName()
    {
        return ValidationResult.Error("'{operationName}' placeholder must be present in operation name template");
    }

    private static ValidationResult ValidateFileExistence(Settings settings)
    {
        return File.Exists(settings.OpenApiPath)
            ? ValidationResult.Success()
            : ValidationResult.Error($"File not found - {Path.GetFullPath(settings.OpenApiPath!)}");
    }

    private static ValidationResult ValidateAndResolveOpenApiSpecPaths(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        SettingsFilePathResolver.ResolveOpenApiSpecPaths(settings.SettingsFilePath!, refitGeneratorSettings);

        if (refitGeneratorSettings.OpenApiPaths is { Length: > 0 })
        {
            settings.OpenApiPath = refitGeneratorSettings.OpenApiPaths[0];

            for (var i = 0; i < refitGeneratorSettings.OpenApiPaths.Length; i++)
            {
                var path = refitGeneratorSettings.OpenApiPaths[i];
                if (!SettingsFilePathResolver.IsUrl(path) && !File.Exists(path))
                {
                    return ValidationResult.Error(
                        $"OpenAPI specification file not found in openApiPaths[{i}]: {path}");
                }
            }

            return ValidationResult.Success();
        }

        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OpenApiPath))
        {
            settings.OpenApiPath = refitGeneratorSettings.OpenApiPath;
            return SettingsFilePathResolver.IsUrl(refitGeneratorSettings.OpenApiPath)
                ? ValidationResult.Success()
                : ValidateFileExistence(settings);
        }

        return ValidationResult.Success();
    }

    private static string ExtractEnumNameFromException(JsonException ex)
    {
        // Try to extract enum type name from exception message
        var message = ex.Message;
        if (message.Contains("PropertyNamingPolicy"))
            return "propertyNamingPolicy (valid: PascalCase, PreserveOriginal)";
        if (message.Contains("MultipleInterfaces"))
            return "multipleInterfaces (valid: ByEndpoint, ByTag)";
        if (message.Contains("TypeAccessibility"))
            return "typeAccessibility (valid: Public, Internal)";
        if (message.Contains("OperationNameGeneratorTypes") || message.Contains("OperationNameGenerator"))
            return "operationNameGenerator (see documentation)";
        if (message.Contains("AuthenticationHeaderStyle"))
            return "authenticationHeaderStyle (valid: None, Method, Parameter)";
        if (message.Contains("CollectionFormat"))
            return "collectionFormat (valid: Multi, Csv, Ssv, Tsv, Pipes)";
        if (message.Contains("IntegerType"))
            return "integerType (valid: Int32, Int64)";

        return "enum property";
    }
}
