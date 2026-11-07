namespace Refitter.Core;

/// <summary>
/// Loads <see cref="RefitGeneratorSettings"/> from the contents of a <c>.refitter</c> settings file.
/// </summary>
/// <remarks>
/// This module deliberately operates on already-read JSON text plus the directory the settings
/// file lives in, rather than reading the file itself. File reading stays with each consumer
/// (the CLI reads from disk; the source generator reads via Roslyn's <c>AdditionalText</c>), which
/// keeps filesystem APIs out of the analyzer assembly and preserves incremental-generation semantics.
/// </remarks>
public static class RefitterSettingsLoader
{
    /// <summary>
    /// Deserializes the contents of a <c>.refitter</c> settings file and resolves any relative
    /// OpenAPI specification paths against the directory the settings file lives in.
    /// </summary>
    /// <param name="json">The raw contents of the <c>.refitter</c> settings file.</param>
    /// <param name="baseDirectory">
    /// The directory the settings file lives in. Relative <see cref="RefitGeneratorSettings.OpenApiPath"/>
    /// and <see cref="RefitGeneratorSettings.OpenApiPaths"/> entries are resolved against this directory.
    /// </param>
    /// <returns>The deserialized settings with relative specification paths resolved to absolute paths.</returns>
    /// <exception cref="System.Text.Json.JsonException">Thrown when <paramref name="json"/> cannot be deserialized.</exception>
    public static RefitGeneratorSettings Load(string json, string baseDirectory)
    {
        var settings = Serializer.Deserialize<RefitGeneratorSettings>(json);
        ResolveRelativeSpecPaths(settings, baseDirectory);
        return settings;
    }

    /// <summary>
    /// Loads the contents of a <c>.refitter</c> settings file the way every Refitter host does: deserializes the
    /// settings, applies the settings file defaults (see <see cref="ApplyDefaults"/>) and resolves relative OpenAPI
    /// specification paths against the directory the settings file lives in.
    /// </summary>
    /// <param name="json">The raw contents of the <c>.refitter</c> settings file.</param>
    /// <param name="settingsFilePath">The path of the settings file.</param>
    /// <returns>The settings, ready to generate from.</returns>
    /// <exception cref="System.Text.Json.JsonException">Thrown when <paramref name="json"/> cannot be deserialized.</exception>
    public static RefitGeneratorSettings LoadSettingsFile(string json, string settingsFilePath)
    {
        var settings = Serializer.Deserialize<RefitGeneratorSettings>(json);
        PrepareSettings(settings, settingsFilePath);
        return settings;
    }

    /// <summary>
    /// Applies the settings file defaults (see <see cref="ApplyDefaults"/>) to settings read from a <c>.refitter</c>
    /// file and resolves their relative OpenAPI specification paths against the directory the file lives in.
    /// </summary>
    /// <param name="settings">The settings read from the settings file.</param>
    /// <param name="settingsFilePath">The path of the settings file.</param>
    public static void PrepareSettings(RefitGeneratorSettings settings, string settingsFilePath)
    {
        ApplyDefaults(settingsFilePath, settings);
        ResolveRelativeSpecPaths(settings, GetSettingsDirectory(settingsFilePath));
    }

    /// <summary>
    /// Gets the directory of a settings file path, following the path rules of the operating system like the
    /// compiler does for the paths it gives the source generator.
    /// </summary>
    /// <param name="settingsFilePath">The path of the settings file.</param>
    /// <returns>The directory, or an empty string when the path has none.</returns>
    public static string GetSettingsDirectory(string settingsFilePath) =>
        Path.GetDirectoryName(settingsFilePath) ?? string.Empty;

    /// <summary>
    /// Resolves any relative OpenAPI specification paths on <paramref name="settings"/> against
    /// <paramref name="baseDirectory"/>. URLs and already-rooted paths are left untouched.
    /// </summary>
    /// <param name="settings">The settings whose specification paths should be resolved.</param>
    /// <param name="baseDirectory">The directory to resolve relative paths against.</param>
    public static void ResolveRelativeSpecPaths(RefitGeneratorSettings settings, string baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(settings.OpenApiPath) &&
            !IsUrl(settings.OpenApiPath!) &&
            !Path.IsPathRooted(settings.OpenApiPath))
        {
            settings.OpenApiPath = Path.GetFullPath(Path.Combine(baseDirectory, settings.OpenApiPath!));
        }

        if (settings.OpenApiPaths is not { Length: > 0 })
            return;

        for (var i = 0; i < settings.OpenApiPaths.Length; i++)
        {
            var path = settings.OpenApiPaths[i];
            if (!IsUrl(path) && !Path.IsPathRooted(path))
            {
                settings.OpenApiPaths[i] = Path.GetFullPath(Path.Combine(baseDirectory, path));
            }
        }
    }

    /// <summary>
    /// Applies default values to settings that are not explicitly set in a settings file.
    /// </summary>
    /// <param name="settingsFilePath">The path to the settings file.</param>
    /// <param name="refitGeneratorSettings">The settings to apply defaults to.</param>
    public static void ApplyDefaults(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder))
            refitGeneratorSettings.GenerateMultipleFiles = true;

        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder))
            refitGeneratorSettings.OutputFolder = RefitGeneratorSettings.DefaultOutputFolder;

        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFilename))
            return;

        var refitterFileName = Path.GetFileNameWithoutExtension(settingsFilePath);
        if (string.IsNullOrEmpty(refitterFileName))
            refitterFileName = "Output";
        refitGeneratorSettings.OutputFilename = $"{refitterFileName}.cs";
    }

    /// <summary>
    /// Determines whether the specified path is an absolute HTTP or HTTPS URL.
    /// </summary>
    /// <param name="path">The path to inspect.</param>
    /// <returns><see langword="true"/> when the path is an HTTP or HTTPS URL; otherwise <see langword="false"/>.</returns>
    public static bool IsUrl(string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out var uriResult) &&
        (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
}
