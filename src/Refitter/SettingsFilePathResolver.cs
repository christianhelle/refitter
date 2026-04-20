using Refitter.Core;

namespace Refitter;

internal static class SettingsFilePathResolver
{
    internal static void ResolveOpenApiSpecPaths(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OpenApiPath))
        {
            refitGeneratorSettings.OpenApiPath = ResolvePath(settingsFilePath, refitGeneratorSettings.OpenApiPath);
        }

        if (refitGeneratorSettings.OpenApiPaths is not { Length: > 0 })
        {
            return;
        }

        for (var i = 0; i < refitGeneratorSettings.OpenApiPaths.Length; i++)
        {
            refitGeneratorSettings.OpenApiPaths[i] = ResolvePath(settingsFilePath, refitGeneratorSettings.OpenApiPaths[i]);
        }
    }

    internal static string ResolvePath(string settingsFilePath, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || IsUrl(path) || Path.IsPathRooted(path))
        {
            return path;
        }

        var settingsFileDirectory = Path.GetDirectoryName(Path.GetFullPath(settingsFilePath)) ?? string.Empty;
        return Path.GetFullPath(Path.Combine(settingsFileDirectory, path));
    }

    internal static bool IsUrl(string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
