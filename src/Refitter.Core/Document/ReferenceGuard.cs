using System.Runtime.InteropServices;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Refitter.Core;

/// <summary>
/// Validates <c>$ref</c> references inside an OpenAPI document before it is resolved, to prevent
/// generation-time SSRF / remote file inclusion (remote <c>http(s)</c> refs) and local file inclusion
/// (absolute paths or <c>../</c> traversal that escapes the input document's directory).
/// </summary>
internal static class ReferenceGuard
{
    public static Task ValidateAsync(
        string openApiPath,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(openApiPath, null, allowRemoteReferences, cancellationToken);

    public static async Task ValidateAsync(
        string openApiPath,
        string? content,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        string rootDirectory = Path.GetDirectoryName(fullPath);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await ValidateLocalFileAsync(fullPath, rootDirectory, allowRemoteReferences, visited, cancellationToken)
            .ConfigureAwait(false);
    }

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
                content = await httpResponse.Content
                    .ReadAsStringWithCancellationAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new ReferenceResolutionException(
                    $"Failed to read OpenAPI document from '{url}' during reference validation: {ex.Message}", ex);
            }
        }

        foreach (var reference in ExtractReferences(content, url))
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ReferenceResolutionException(
                $"Failed to read OpenAPI document from '{filePath}' during reference validation: {ex.Message}", ex);
        }

        string currentDirectory = Path.GetDirectoryName(filePath);

        foreach (var reference in ExtractReferences(content, filePath))
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

    private static List<string> ExtractReferences(string content, string source)
    {
        // Always try JSON first: the JSON parsers downstream accept comments, so a document can be
        // valid JSON without starting with '{' or '['
        try
        {
            return ExtractJsonReferences(content);
        }
        catch (JsonReaderException)
        {
            // Not valid JSON, fall through to the YAML reader
        }

        try
        {
            return ExtractYamlReferences(content);
        }
        catch (YamlException ex)
        {
            // Fail closed: a document the guard cannot read could still be read by a more lenient
            // parser further down the pipeline, with references the guard never inspected
            throw new ReferenceResolutionException(
                $"OpenAPI document \"{source}\" could not be parsed for '$ref' validation: {ex.Message}", ex);
        }
    }

    // Reads the token stream rather than a DOM, so escaped keys and values are decoded the way the
    // reference resolvers decode them, and duplicate "$ref" keys are all inspected
    private static List<string> ExtractJsonReferences(string content)
    {
        var references = new List<string>();
        var rootCompleted = false;
        using var reader = new JsonTextReader(new StringReader(content)) { DateParseHandling = DateParseHandling.None };
        while (reader.Read())
        {
            if (reader.Depth == 0 && reader.TokenType is (JsonToken.EndObject or JsonToken.EndArray))
                rootCompleted = true;

            if (reader.TokenType != JsonToken.PropertyName || (string?)reader.Value != "$ref")
                continue;

            // Comments may sit between the key and its value
            bool hasValue;
            do
            {
                hasValue = reader.Read();
            }
            while (hasValue && reader.TokenType == JsonToken.Comment);

            if (hasValue && reader.TokenType == JsonToken.String)
                AddReference(references, (string?)reader.Value);
        }

        // The reader stops quietly at the end of truncated input, so require a complete root object or array
        if (!rootCompleted)
            throw new JsonReaderException("Expected a complete JSON object or array.");

        return references;
    }

    // Deserializes the same way NSwag converts YAML, so escapes, anchors and aliases are resolved
    // before the "$ref" keys and values are inspected. Duplicate keys are rejected because parsers
    // disagree on which one wins
    private static List<string> ExtractYamlReferences(string content)
    {
        var references = new List<string>();
        var root = new DeserializerBuilder().WithDuplicateKeyChecking().Build().Deserialize(new StringReader(content));

        // A document read as a bare scalar is text the YAML reader did not understand as a structure,
        // which a different parser might still read as one
        if (root is not (null or IDictionary<object, object> or IList<object>))
            throw new YamlException("Expected a YAML mapping or sequence at the document root.");

        var pending = new Stack<object?>();
        var visited = new HashSet<object>(ReferenceComparer.Instance);
        pending.Push(root);

        while (pending.Count > 0)
        {
            var node = pending.Pop();

            // Aliases share one node, so each container is inspected once to keep the walk linear
            if (node is null or string || !visited.Add(node))
                continue;

            if (node is IDictionary<object, object> mapping)
            {
                foreach (var entry in mapping)
                {
                    if (entry.Key?.ToString() == "$ref" && entry.Value is string value)
                        AddReference(references, value);

                    pending.Push(entry.Key);
                    pending.Push(entry.Value);
                }
            }
            else if (node is IList<object> sequence)
            {
                foreach (var item in sequence)
                    pending.Push(item);
            }
        }

        return references;
    }

    private static void AddReference(List<string> references, string? value)
    {
        var trimmed = value?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
            references.Add(trimmed!);
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new();

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

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

    private static ReferenceResolutionException RemoteBlocked(string reference, string source) =>
        new($"Remote '$ref' \"{reference}\" in \"{source}\" was blocked. " +
            "Remote reference resolution is disabled by default; pass --allow-remote-refs " +
            "(or set allowRemoteReferences: true) to enable it.");

    private static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(path);
        return await reader
            .ReadToEndWithCancellationAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
