using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Validates <c>$ref</c> references inside an OpenAPI document before it is resolved, to prevent
/// generation-time SSRF / remote file inclusion (remote <c>http(s)</c> refs) and local file inclusion
/// (absolute paths or <c>../</c> traversal that escapes the input document's directory).
/// </summary>
internal static class ReferenceGuard
{
    private static readonly Regex RefPattern = new(
        "[\"']?\\$ref[\"']?\\s*:\\s*[\"']([^\"']+)[\"']",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static async Task ValidateAsync(
        string openApiPath,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(openApiPath))
            return;

        if (PathUtilities.IsHttp(openApiPath))
        {
            await ValidateRemoteEntryAsync(openApiPath, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var fullPath = Path.GetFullPath(openApiPath);
        if (!File.Exists(fullPath))
            return;

        var rootDirectory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await ValidateLocalFileAsync(fullPath, rootDirectory, allowRemoteReferences, visited, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task ValidateRemoteEntryAsync(
        string url,
        bool allowRemoteReferences,
        CancellationToken cancellationToken)
    {
        string content;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            content = await client.GetStringAsync(url).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        foreach (var reference in ExtractReferences(content))
        {
            if (PathUtilities.IsHttp(reference))
            {
                if (!allowRemoteReferences)
                    throw RemoteBlocked(reference, url);
            }
            else
            {
                throw new ReferenceResolutionException(
                    $"Local '$ref' \"{reference}\" in remote document \"{url}\" was rejected. " +
                    "Refitter does not resolve local files referenced from a remote document.");
            }
        }
    }

    private static async Task ValidateLocalFileAsync(
        string filePath,
        string rootDirectory,
        bool allowRemoteReferences,
        HashSet<string> visited,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!visited.Add(filePath) || !File.Exists(filePath))
            return;

        string content;
        try
        {
            content = await ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        var currentDirectory = Path.GetDirectoryName(filePath) ?? rootDirectory;

        foreach (var reference in ExtractReferences(content))
        {
            if (reference.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (PathUtilities.IsHttp(reference))
            {
                if (!allowRemoteReferences)
                    throw RemoteBlocked(reference, filePath);
                continue;
            }

            var fileRef = reference.Split('#')[0];
            if (string.IsNullOrWhiteSpace(fileRef))
                continue;

            var resolved = Path.GetFullPath(Path.Combine(currentDirectory, fileRef));
            if (!IsWithin(rootDirectory, resolved))
            {
                throw new ReferenceResolutionException(
                    $"Local '$ref' \"{reference}\" resolves to \"{resolved}\", outside the input directory " +
                    $"\"{rootDirectory}\". Refitter confines local references to the input document's directory tree.");
            }

            await ValidateLocalFileAsync(resolved, rootDirectory, allowRemoteReferences, visited, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static IEnumerable<string> ExtractReferences(string content)
    {
        foreach (Match match in RefPattern.Matches(content))
        {
            var value = match.Groups[1].Value.Trim();
            if (value.Length > 0)
                yield return value;
        }
    }

    private static bool IsWithin(string rootDirectory, string candidate)
    {
        var root = Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var path = Path.GetFullPath(candidate);
        return path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static ReferenceResolutionException RemoteBlocked(string reference, string source) =>
        new($"Remote '$ref' \"{reference}\" in \"{source}\" was blocked. " +
            "Remote reference resolution is disabled by default; pass --allow-remote-refs " +
            "(or set allowRemoteReferences: true) to enable it.");

    private static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(path);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }
}
