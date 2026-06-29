using System.Runtime.InteropServices;
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
        "[\"']?\\$ref[\"']?\\s*:\\s*(?:[\"']([^\"']+)[\"']|([^\\s#][^\\s]*))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
        /// Validates the OpenAPI document at the specified path.
        /// </summary>
        /// <param name="openApiPath">The path or URL of the OpenAPI document to validate.</param>
        /// <param name="allowRemoteReferences">Whether remote $ref values are allowed.</param>
        public static Task ValidateAsync(
        string openApiPath,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(openApiPath, null, allowRemoteReferences, cancellationToken);

    /// <summary>
    /// Validates the $ref values in an OpenAPI document at the specified path.
    /// </summary>
    /// <param name="openApiPath">The local file path or HTTP(S) URL of the OpenAPI document.</param>
    /// <param name="content">Optional preloaded document content used when validating a remote document.</param>
    /// <param name="allowRemoteReferences">Whether remote $ref values are permitted.</param>
    /// <param name="cancellationToken">A token used to cancel validation.</param>
    public static async Task ValidateAsync(
        string openApiPath,
        string? content,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(openApiPath))
            return;

        if (PathUtilities.IsHttp(openApiPath))
        {
            await ValidateRemoteEntryAsync(openApiPath, content, allowRemoteReferences, cancellationToken)
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

    /// <summary>
    /// Validates $ref values in a remote OpenAPI document.
    /// </summary>
    /// <param name="url">The document URL.</param>
    /// <param name="preloadedContent">The document content to validate, or null to load it from <paramref name="url"/>.</param>
    /// <param name="allowRemoteReferences">Whether HTTP and HTTPS references are allowed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static async Task ValidateRemoteEntryAsync(
        string url,
        string? preloadedContent,
        bool allowRemoteReferences,
        CancellationToken cancellationToken)
    {
        string content;
        if (preloadedContent != null)
        {
            content = preloadedContent;
        }
        else
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                using var httpResponse = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, url),
                    cancellationToken).ConfigureAwait(false);
                content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ReferenceResolutionException(
                    $"Failed to read OpenAPI document from '{url}' during reference validation: {ex.Message}", ex);
            }
        }

        foreach (var reference in ExtractReferences(content))
        {
            if (reference.StartsWith("#", StringComparison.Ordinal))
                continue;

            Uri? resolvedUri = null;
            if (Uri.TryCreate(reference, UriKind.Absolute, out var absoluteUri))
            {
                resolvedUri = absoluteUri;
            }
            else if (Uri.TryCreate(new Uri(url), reference.Split('#')[0], out var relativeUri))
            {
                resolvedUri = relativeUri;
            }

            if (resolvedUri != null && !string.IsNullOrEmpty(resolvedUri.Scheme))
            {
                var isHttpScheme = resolvedUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                                   resolvedUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);

                if (isHttpScheme)
                {
                    if (!allowRemoteReferences)
                        throw RemoteBlocked(reference, url);
                }
                else
                {
                    // Non-HTTP schemes (file:, data:, etc.) are always blocked
                    throw RemoteBlocked(reference, url);
                }
            }
            else if (!reference.StartsWith("/", StringComparison.Ordinal) &&
                     !reference.StartsWith("./", StringComparison.Ordinal) &&
                     !reference.StartsWith("../", StringComparison.Ordinal))
            {
                // Relative references from remote documents treated as remote
                if (!allowRemoteReferences)
                    throw RemoteBlocked(reference, url);
            }
        }
    }

    /// <summary>
    /// Validates local OpenAPI reference files within the input directory tree.
    /// </summary>
    /// <param name="filePath">The path to the OpenAPI document to inspect.</param>
    /// <param name="rootDirectory">The directory that bounds local reference traversal.</param>
    /// <param name="allowRemoteReferences">Whether HTTP and HTTPS references are permitted.</param>
    /// <param name="visited">The set of file paths already processed during traversal.</param>
    /// <param name="cancellationToken">The token used to cancel validation.</param>
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
        catch (Exception ex)
        {
            throw new ReferenceResolutionException(
                $"Failed to read OpenAPI document from '{filePath}' during reference validation: {ex.Message}", ex);
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

    /// <summary>
    /// Extracts <c>$ref</c> values from text content.
    /// </summary>
    /// <param name="content">The text to scan.</param>
    /// <returns>The extracted <c>$ref</c> values.</returns>
    private static IEnumerable<string> ExtractReferences(string content)
    {
        foreach (Match match in RefPattern.Matches(content))
        {
            // Group 1 = quoted value, Group 2 = unquoted YAML scalar
            var value = !string.IsNullOrEmpty(match.Groups[1].Value)
                ? match.Groups[1].Value.Trim()
                : match.Groups[2].Value.Trim();
            if (value.Length > 0)
                yield return value;
        }
    }

    /// <summary>
    /// Determines whether a path is inside a root directory.
    /// </summary>
    /// <param name="rootDirectory">The root directory to compare against.</param>
    /// <param name="candidate">The path to test.</param>
    /// <returns><c>true</c> if the path equals the root directory or is located beneath it; otherwise, <c>false</c>.</returns>
    private static bool IsWithin(string rootDirectory, string candidate)
    {
        var root = Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var path = Path.GetFullPath(candidate);

        // Use filesystem-aware comparison: case-sensitive on Linux/macOS, case-insensitive on Windows
        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return path.Equals(root, comparison) ||
               path.StartsWith(root + Path.DirectorySeparatorChar, comparison) ||
               path.StartsWith(root + Path.AltDirectorySeparatorChar, comparison);
    }

    /// <summary>
            /// Creates an exception for a blocked remote reference.
            /// </summary>
            /// <param name="reference">The blocked $ref value.</param>
            /// <param name="source">The document that contains the reference.</param>
            /// <returns>An exception that reports the remote reference was blocked.</returns>
            private static ReferenceResolutionException RemoteBlocked(string reference, string source) =>
        new($"Remote '$ref' \"{reference}\" in \"{source}\" was blocked. " +
            "Remote reference resolution is disabled by default; pass --allow-remote-refs " +
            "(or set allowRemoteReferences: true) to enable it.");

    /// <summary>
    /// Reads the entire text content of a file.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <param name="cancellationToken">A token used to cancel the read operation.</param>
    /// <returns>The file contents.</returns>
    private static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(path);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }
}
