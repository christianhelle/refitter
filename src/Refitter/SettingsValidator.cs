using Refitter.Core;

using Spectre.Console;

namespace Refitter;

public class SettingsValidator
{
    public ValidationResult Validate(Settings settings)
    {
        if (BothSettingsFilesAreEmpty(settings) || BothSettingsFilesArePresent(settings))
        {
            return GetValidationErrorForSettingsFiles();
        }

        return !string.IsNullOrWhiteSpace(settings.SettingsFilePath)
            ? ValidateFilePath(settings)
            : ValidateOperationNameAndUrl(settings);
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

    private ValidationResult ValidateFilePath(Settings settings)
    {
        var json = File.ReadAllText(settings.SettingsFilePath!);
        var refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(json);
        settings.OpenApiPath = refitGeneratorSettings.OpenApiPath;

        return ValidateFileAndOutputSettings(settings, refitGeneratorSettings);
    }

    private ValidationResult ValidateFileAndOutputSettings(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OpenApiPath))
        {
            return GetValidationErrorForOpenApiPath();
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputPath) &&
            settings.OutputPath != Settings.DefaultOutputPath &&
            (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder) ||
             !string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFilename)))
        {
            return GetValidationErrorForOutputPath();
        }

        return ValidateOperationNameAndUrl(settings);
    }

    private static ValidationResult GetValidationErrorForOpenApiPath()
    {
        return ValidationResult.Error(
            "The 'openApiPath' in settings file is required when " +
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

    private ValidationResult ValidateOperationNameAndUrl(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OperationNameTemplate) &&
            !settings.OperationNameTemplate.Contains("{operationName}") &&
            settings.MultipleInterfaces != MultipleInterfaces.ByEndpoint)
        {
            return GetValidationErrorForOperationName();
        }

        return IsUrl(settings.OpenApiPath!) ? ValidationResult.Success() : ValidateFileExistence(settings);
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

    private static bool IsUrl(string openApiPath)
    {
        return Uri.TryCreate(openApiPath, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}