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
        if (settings is null)
        {
            throw new System.Text.Json.JsonException("Failed to deserialize settings: result was null");
        }
        ResolveRelativeSpecPaths(settings, baseDirectory);
        return settings;
    }

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

        if (settings.OpenApiPaths is { Length: > 0 })
        {
            for (var i = 0; i < settings.OpenApiPaths.Length; i++)
            {
                var path = settings.OpenApiPaths[i];
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                if (!IsUrl(path) && !Path.IsPathRooted(path))
                {
                    settings.OpenApiPaths[i] = Path.GetFullPath(Path.Combine(baseDirectory, path));
                }
            }
        }
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
