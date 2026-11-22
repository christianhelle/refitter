using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Refitter.Core;

/// <summary>
/// Reads an OpenAPI document and every file it references exactly once into a <see cref="DocumentSet"/>. Each
/// file is checked as it is read: remote <c>http(s)</c> references are rejected unless allowed, to prevent
/// generation-time SSRF, and local references (absolute paths or <c>../</c> traversal) must stay inside the
/// input document's directory tree, to prevent local file inclusion. The reference resolvers serve files from
/// the set only, so what is parsed is what was checked.
/// </summary>
internal static class OpenApiDocumentReader
{
    // IsWithin uses a platform-aware comparison (case-insensitive on Windows). The visited set always uses
    // an ordinal comparison, even on Windows, because individual NTFS directories can be case-sensitive:
    // folding case there could collapse two distinct files such as "Model.json" and "model.json" into one
    // entry so the second is never inspected. Redundant validation of case aliases is acceptable
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static readonly StringComparer PathComparer = StringComparer.Ordinal;

    private static readonly StringComparison PathComparison =
        IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static Task<DocumentSet> ReadAsync(
        string openApiPath,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default) =>
        ReadAsync(openApiPath, null, allowRemoteReferences, cancellationToken);

    /// <summary>
    /// Reads the OpenAPI document at <paramref name="openApiPath"/>, a local path or an <c>http(s)</c> URL. When
    /// <paramref name="content"/> is given it is used as the document's content instead of reading it again.
    /// </summary>
    public static async Task<DocumentSet> ReadAsync(
        string openApiPath,
        string? content,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(openApiPath))
            throw new ArgumentException(
                "The path parameter cannot be null, empty, or contain only whitespace.",
                nameof(openApiPath));

        var reading = new Reading(allowRemoteReferences, cancellationToken);
        if (PathUtilities.IsHttp(openApiPath))
        {
            content ??= await DownloadAsync(openApiPath, cancellationToken).ConfigureAwait(false);
            var remoteRoot = await reading.ReadRemoteRootAsync(openApiPath, content).ConfigureAwait(false);
            return reading.ToSet(null, remoteRoot);
        }

        var fullPath = Path.GetFullPath(openApiPath);
        content ??= ReadRootFile(openApiPath, fullPath);
        var root = await reading.ReadLocalRootAsync(fullPath, content).ConfigureAwait(false);
        return reading.ToSet(fullPath, root);
    }

    /// <summary>
    /// Replaces the OpenAPI document of <paramref name="set"/> with <paramref name="content"/>, such as a document
    /// the multi-file reader merged, keeping the referenced files that were read with it.
    /// </summary>
    public static DocumentSet WithRootContent(DocumentSet set, string content)
    {
        var source = set.RootDocumentPath ?? "the OpenAPI document";
        var (json, _) = ReadContent(content, source, readAsYaml: false);
        return set.WithRootJson(OpenApiJsonNormalizer.NormalizeDocument(json));
    }

    /// <summary>
    /// Converts YAML to JSON the way the reader does, applying the nesting and alias expansion limits.
    /// </summary>
    internal static string ConvertYamlToJson(
        string yaml,
        string? source = null,
        long maxExpandedNodes = YamlAliasExpansionGuard.MaxExpandedNodes,
        int maxAmplification = YamlAliasExpansionGuard.MaxAmplification,
        long maxExpandedSize = YamlAliasExpansionGuard.MaxExpandedSize)
    {
        source ??= "the YAML document";
        DocumentNestingGuard.EnsureYamlWithinLimit(yaml, source);
        try
        {
            var root = DeserializeYaml(yaml, source, maxExpandedNodes, maxAmplification, maxExpandedSize);
            return new SerializerBuilder().JsonCompatible().Build().Serialize(root!);
        }
        catch (YamlException ex)
        {
            throw new ReferenceResolutionException(
                $"OpenAPI document \"{source}\" could not be parsed for '$ref' validation: {ex.Message}", ex);
        }
    }

    private static string ReadRootFile(string openApiPath, string fullPath)
    {
        try
        {
            return File.ReadAllText(fullPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new InvalidOperationException(
                $"Failed to load OpenAPI document from '{openApiPath}': {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ReferenceResolutionException(
                $"Failed to read OpenAPI document from '{fullPath}' during reference validation: {ex.Message}", ex);
        }
    }

    private static async Task<string> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            return await RemoteDocumentDownloader
                .DownloadAsync(url, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ReferenceResolutionException)
        {
            throw new ReferenceResolutionException(
                $"Failed to read OpenAPI document from '{url}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// The state of one read: the files read so far and the rules they are checked against.
    /// </summary>
    private sealed class Reading(bool allowRemoteReferences, CancellationToken cancellationToken)
    {
        private readonly Dictionary<string, string> files = new(PathComparer);
        private readonly Dictionary<string, string> urls = new(StringComparer.Ordinal);
        private readonly HashSet<string> visitedFiles = new(PathComparer);
        private readonly HashSet<string> visitedUrls = new(StringComparer.Ordinal);
        private string rootDirectory = string.Empty;
        private string rootDocumentPath = string.Empty;
        private string? remoteRootUrl;
        private string? remoteRootContent;
        private bool readAsYaml;

        public DocumentSet ToSet(string? rootPath, string rootJson) => new(rootPath, rootJson, files, urls);

        public async Task<string> ReadLocalRootAsync(string fullPath, string content)
        {
            rootDirectory = Path.GetDirectoryName(fullPath);
            rootDocumentPath = fullPath;

            // A YAML root document is loaded by a reader that reads every referenced file as YAML
            readAsYaml = MayBeReadAsYaml(fullPath);

            var json = await ReadLocalFileAsync(fullPath, content).ConfigureAwait(false);
            return OpenApiJsonNormalizer.NormalizeDocument(json!);
        }

        public async Task<string> ReadRemoteRootAsync(string url, string content)
        {
            // A YAML root document is loaded by a reader that reads every referenced document as YAML
            readAsYaml = MayBeReadAsYaml(url);

            // A reference back to the root document reuses the content that was read, never a second download
            remoteRootUrl = url;
            remoteRootContent = content;

            var (json, references) = ReadContent(content, url, readAsYaml: false);
            var normalized = OpenApiJsonNormalizer.NormalizeDocument(json);
            var resolvable = ResolvableReferences(normalized);

            foreach (var reference in references)
            {
                if (!reference.StartsWith("#", StringComparison.Ordinal))
                    await ReadRemoteRootReferenceAsync(reference, url, resolvable.Contains(reference)).ConfigureAwait(false);
            }

            return normalized;
        }

        // The root document is parsed without a document path, so its relative references are never
        // resolved; only absolute remote references are read
        private async Task ReadRemoteRootReferenceAsync(string reference, string url, bool resolvable)
        {
            var resolvedUri = ResolveAgainst(url, reference);
            if (resolvedUri != null && !string.IsNullOrEmpty(resolvedUri.Scheme))
            {
                // Non-HTTP schemes (file:, data:, etc.) are always blocked
                if (!IsHttpScheme(resolvedUri) || !allowRemoteReferences)
                    throw RemoteBlocked(reference, url);

                if (resolvable && PathUtilities.IsHttp(reference))
                    await ReadRemoteFileAsync(reference.Split('#')[0]).ConfigureAwait(false);
            }
            else if (!allowRemoteReferences &&
                     !reference.StartsWith("/", StringComparison.Ordinal) &&
                     !reference.StartsWith("./", StringComparison.Ordinal) &&
                     !reference.StartsWith("../", StringComparison.Ordinal))
            {
                // Relative references from remote documents treated as remote
                throw RemoteBlocked(reference, url);
            }
        }

        private static Uri? ResolveAgainst(string url, string reference)
        {
            if (Uri.TryCreate(reference, UriKind.Absolute, out var absoluteUri))
                return absoluteUri;

            return Uri.TryCreate(new Uri(url), reference.Split('#')[0], out var relativeUri) ? relativeUri : null;
        }

        // The schema resolver passes an absolute reference through as written and resolves a relative one
        // against the document's URL, which normalizes it (a default port, for example, is dropped)
        private static string RemoteKey(string reference, Uri resolved) =>
            reference.StartsWith("http://", StringComparison.Ordinal) ||
            reference.StartsWith("https://", StringComparison.Ordinal)
                ? reference.Split('#')[0]
                : resolved.ToString().Split('#')[0];

        private static bool IsHttpScheme(Uri uri) =>
            uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);

        private static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(path);
            return await reader
                .ReadToEndWithCancellationAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        // Reads a referenced remote document once. Its relative references resolve against its URL, the way the
        // schema resolver resolves them, so they are remote references too
        private async Task ReadRemoteFileAsync(string url)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!visitedUrls.Add(url))
                return;

            var content = url.Equals(remoteRootUrl, StringComparison.Ordinal)
                ? remoteRootContent!
                : await RemoteDocumentDownloader
                    .DownloadAsync(url, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            var (json, references) = ReadContent(content, url, readAsYaml);
            var normalized = OpenApiJsonNormalizer.NormalizeReferencedFile(json);
            urls[url] = normalized;

            var resolvable = ResolvableReferences(normalized);
            foreach (var reference in references)
            {
                if (reference.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (!Uri.TryCreate(new Uri(url), reference, out var resolved) || !IsHttpScheme(resolved))
                    throw RemoteBlocked(reference, url);

                if (resolvable.Contains(reference))
                    await ReadRemoteFileAsync(RemoteKey(reference, resolved)).ConfigureAwait(false);
            }
        }

        // Returns the content as JSON, or null when the file does not exist or was already read
        private async Task<string?> ReadLocalFileAsync(string filePath, string? content = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!visitedFiles.Add(filePath))
                return null;

            if (content == null)
            {
                if (!File.Exists(filePath))
                    return null;

                try
                {
                    content = await ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new ReferenceResolutionException(
                        $"Failed to read OpenAPI document from '{filePath}' during reference validation: {ex.Message}", ex);
                }
            }

            var (json, references) = ReadContent(content, filePath, readAsYaml);
            var normalized = OpenApiJsonNormalizer.NormalizeReferencedFile(json);

            // The schema resolver resolves nested references against the root document, but they were checked
            // against this file, so pin them to this file's directory
            files[filePath] = ReferencedFileReferenceRebaser.Rebase(normalized, filePath);

            var resolvable = ResolvableReferences(normalized);
            string currentDirectory = Path.GetDirectoryName(filePath);
            foreach (var reference in references)
            {
                var download = resolvable.Contains(reference);
                await ReadLocalReferenceAsync(reference, filePath, currentDirectory, download).ConfigureAwait(false);

                // The rebaser cannot pin the references of a file whose path contains '#', so the schema
                // resolver resolves them against the root document's directory. Read that file too
                if (filePath.IndexOf('#') >= 0 && !filePath.Equals(rootDocumentPath, StringComparison.Ordinal))
                    await ReadLocalReferenceAsync(reference, filePath, rootDirectory, download).ConfigureAwait(false);
            }

            return json;
        }

        private async Task ReadLocalReferenceAsync(
            string reference,
            string filePath,
            string currentDirectory,
            bool resolvable)
        {
            if (reference.StartsWith("#", StringComparison.Ordinal))
                return;

            // Classify by URI scheme rather than by a literal "http://" prefix: the URI parser also reads
            // forms such as "http:/\\host" as network locations, so they must not pass as local paths
            if (PathUtilities.TryGetRemoteUri(reference, out Uri? remoteUri) || PathUtilities.IsHttp(reference))
            {
                var isHttpScheme = remoteUri == null || IsHttpScheme(remoteUri);

                // Non-HTTP schemes (ftp:, data:, etc.) are always blocked
                if (!isHttpScheme || !allowRemoteReferences)
                    throw RemoteBlocked(reference, filePath);

                // Resolvers only fetch the standard "http://" and "https://" spellings and read anything else,
                // such as "http:/\\host/../x", as a local file name that would escape the confinement check
                if (!PathUtilities.IsHttp(reference))
                {
                    throw new ReferenceResolutionException(
                        $"Remote '$ref' \"{reference}\" in \"{filePath}\" does not use a standard " +
                        "\"http://\" or \"https://\" prefix and is rejected.");
                }

                if (resolvable)
                    await ReadRemoteFileAsync(reference.Split('#')[0]).ConfigureAwait(false);

                return;
            }

            var fileRef = reference.Split('#')[0];
            if (string.IsNullOrWhiteSpace(fileRef))
                return;

            // NJsonSchema joins the raw text of a "file:" URI to the document directory instead of converting it,
            // so it would open a different file from the one validated here. Reject them outright
            if (fileRef.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                throw new ReferenceResolutionException(
                    $"The '$ref' \"{reference}\" in \"{filePath}\" is a file URI. File URIs are not supported in " +
                    "'$ref'; use a relative path instead.");
            }

            var resolved = Path.GetFullPath(Path.Combine(currentDirectory, fileRef));
            if (!IsWithin(rootDirectory, resolved))
            {
                throw OutsideInputDirectory(reference, resolved, rootDirectory);
            }

            if (ContainsLinkBelowRoot(rootDirectory, resolved, rootDocumentPath))
                throw SymbolicLinkBlocked(reference, resolved);

            // OasReader resolves a reference whose file is missing at its literal path by searching the
            // whole tree for a file whose path ends with the reference, so a link elsewhere can be read
            if (!File.Exists(resolved) &&
                FindLinkedSearchMatch(rootDirectory, fileRef, rootDocumentPath) is { } linked)
                throw SymbolicLinkBlocked(reference, linked);

            await ReadLocalFileAsync(resolved).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The references the schema resolver follows: <c>$ref</c> values and discriminator mappings outside example,
    /// default, enum, const and extension values, which are only data. Every reference is still checked, but only
    /// these are downloaded.
    /// </summary>
    private static HashSet<string> ResolvableReferences(string json)
    {
        var references = new HashSet<string>(StringComparer.Ordinal);
        if (SanitizerJson.Parse(json) is not JContainer document)
            return references;

        foreach (var property in document.Descendants().OfType<JProperty>())
        {
            if (property.Value is not JValue { Type: JTokenType.String } value ||
                NamedMapEntries.IsInsideLiteralValue(property) ||
                !(string.Equals(property.Name, "$ref", StringComparison.OrdinalIgnoreCase) || IsDiscriminatorMapping(property)))
                continue;

            var reference = ((string)value.Value!).Trim();
            if (reference.Length > 0)
                references.Add(reference);
        }

        return references;
    }

    private static bool IsDiscriminatorMapping(JProperty property) =>
        property.Parent?.Parent is JProperty mapping &&
        string.Equals(mapping.Name, "mapping", StringComparison.OrdinalIgnoreCase) &&
        mapping.Parent?.Parent is JProperty discriminator &&
        string.Equals(discriminator.Name, "discriminator", StringComparison.OrdinalIgnoreCase);

    internal static List<string> ExtractReferences(
        string content,
        string source,
        long maxExpandedNodes = YamlAliasExpansionGuard.MaxExpandedNodes,
        int maxAmplification = YamlAliasExpansionGuard.MaxAmplification,
        long maxExpandedSize = YamlAliasExpansionGuard.MaxExpandedSize,
        bool readAsYaml = false) =>
        ReadContent(content, source, readAsYaml, maxExpandedNodes, maxAmplification, maxExpandedSize).References;

    /// <summary>
    /// Reads the content of one file: the JSON the schema resolver parses, and the references to check. Content
    /// that is valid JSON is used as is; anything else is read as YAML and converted to JSON.
    /// </summary>
    private static (string Json, List<string> References) ReadContent(
        string content,
        string source,
        bool readAsYaml,
        long maxExpandedNodes = YamlAliasExpansionGuard.MaxExpandedNodes,
        int maxAmplification = YamlAliasExpansionGuard.MaxAmplification,
        long maxExpandedSize = YamlAliasExpansionGuard.MaxExpandedSize)
    {
        // Always try JSON first: the JSON parsers downstream accept comments, so a document can be
        // valid JSON without starting with '{' or '['
        List<string>? jsonReferences = null;
        try
        {
            jsonReferences = ExtractJsonReferences(content, source);
        }
        catch (JsonReaderException)
        {
            // Not valid JSON, fall through to the YAML reader
        }

        // A document can be valid JSON with comments and still read differently as YAML, where the text of a
        // comment such as "/*: null, "$ref": "x" # */" is a mapping entry. When a YAML loader (the multi-file
        // reader) can read the document, either because the root is YAML or because the file has a YAML
        // extension, check the references of both readings
        if (jsonReferences != null && !readAsYaml && !MayBeReadAsYaml(source))
            return (content, jsonReferences);

        // The YAML reader recurses per nesting level, and a stack overflow is uncatchable. The JSON reader
        // above is iterative and checks the depth as it reads, so only the YAML path needs this walk first.
        // Every root and referenced document is read through here, so this covers all load paths
        DocumentNestingGuard.EnsureYamlWithinLimit(content, source);

        try
        {
            var root = DeserializeYaml(content, source, maxExpandedNodes, maxAmplification, maxExpandedSize);
            var references = ExtractYamlReferences(root);
            if (jsonReferences != null)
                references.AddRange(jsonReferences);

            // A document a YAML loader reads is parsed as YAML, even when it is also valid JSON, the way NSwag
            // reads a YAML document. Same conversion as NSwag's OpenApiYamlDocument and NJsonSchema's JsonSchemaYaml
            return (new SerializerBuilder().JsonCompatible().Build().Serialize(root!), references);
        }
        catch (YamlException ex)
        {
            // Fail closed: a document the reader cannot read could still be read by a more lenient
            // parser further down the pipeline, with references the reader never inspected
            throw new ReferenceResolutionException(
                $"OpenAPI document \"{source}\" could not be parsed for '$ref' validation: {ex.Message}", ex);
        }
    }

    // A local path may contain "?" or "#", which PathUtilities.IsYaml reads as the start of a URL query or
    // fragment, while a reader that picks its parser by the file name sees the literal extension. Either
    // reading can lead a loader to YAML, so a match on either counts. A URL has a real query and fragment,
    // so only its path decides
    private static bool MayBeReadAsYaml(string path) =>
        PathUtilities.IsYaml(path) ||
        !PathUtilities.IsHttp(path) &&
        (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
         path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

    // Reads the token stream rather than a DOM, so escaped keys and values are decoded the way the
    // reference resolvers decode them, and duplicate "$ref" keys are all inspected. The key is matched
    // case-insensitively because Newtonsoft.Json binds properties case-insensitively, so "$REF" is a reference.
    // The values of "discriminator.mapping" are references too: the schema loader resolves them as files
    private static List<string> ExtractJsonReferences(string content, string source)
    {
        var references = new List<string>();
        var rootCompleted = false;

        // The property name each enclosing object or array was found under, innermost first
        var containers = new Stack<string?>();
        string? propertyName = null;
        var expectReferenceValue = false;

        using var reader = DocumentNestingGuard.CreateJsonReader(content);
        while (reader.Read())
        {
            // Not caught below: a document nested too deeply is rejected, never handed to the YAML reader
            DocumentNestingGuard.EnsureJsonDepth(reader, source);

            if (reader.Depth == 0 && reader.TokenType is (JsonToken.EndObject or JsonToken.EndArray))
                rootCompleted = true;

            switch (reader.TokenType)
            {
                // Comments may sit between the key and its value
                case JsonToken.Comment:
                    continue;
                case JsonToken.PropertyName:
                    propertyName = (string?)reader.Value;
                    expectReferenceValue = string.Equals(propertyName, "$ref", StringComparison.OrdinalIgnoreCase);
                    continue;
                case JsonToken.StartObject:
                case JsonToken.StartArray:
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                    TrackContainer(reader.TokenType, propertyName, containers);
                    break;
                case JsonToken.String:
                    if (expectReferenceValue || IsDiscriminatorMappingValue(containers))
                        AddReference(references, (string)reader.Value!);
                    break;
            }

            expectReferenceValue = false;
            propertyName = null;
        }

        // The reader stops quietly at the end of truncated input, so require a complete root object or array
        if (!rootCompleted)
            throw new JsonReaderException("Expected a complete JSON object or array.");

        return references;
    }

    // Records the property name an object was found under when it opens, and forgets it when it closes.
    // Arrays are recorded without a name, so a mapping nested in an array is not mistaken for a direct one
    private static void TrackContainer(JsonToken token, string? propertyName, Stack<string?> containers)
    {
        switch (token)
        {
            case JsonToken.StartObject:
                containers.Push(propertyName);
                break;
            case JsonToken.StartArray:
                containers.Push(null);
                break;
            default:
                if (containers.Count > 0)
                    containers.Pop();
                break;
        }
    }

    private static bool IsDiscriminatorMappingValue(Stack<string?> containers)
    {
        if (containers.Count < 2 || !string.Equals(containers.Peek(), "mapping", StringComparison.OrdinalIgnoreCase))
            return false;

        // Stack enumerates innermost first: "mapping", then the object it is a property of
        using var enumerator = containers.GetEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();
        return string.Equals(enumerator.Current, "discriminator", StringComparison.OrdinalIgnoreCase);
    }

    // Deserializes the same way NSwag converts YAML, so escapes, anchors and aliases are resolved
    // before the "$ref" keys and values are inspected. Duplicate keys are rejected because parsers
    // disagree on which one wins
    private static object? DeserializeYaml(
        string content,
        string source,
        long maxExpandedNodes,
        int maxAmplification,
        long maxExpandedSize)
    {
        var root = new DeserializerBuilder().WithDuplicateKeyChecking().Build().Deserialize(new StringReader(content));

        // A document read as a bare scalar is text the YAML reader did not understand as a structure,
        // which a different parser might still read as one
        if (root is not (null or IDictionary<object, object> or IList<object>))
            throw new YamlException("Expected a YAML mapping or sequence at the document root.");

        // Aliases share nodes, so the walk below is linear, but serializing the document as JSON later
        // writes a shared node once per alias. Reject a document whose expansion is out of proportion
        YamlAliasExpansionGuard.EnsureWithinBudget(root, source, maxExpandedNodes, maxAmplification, maxExpandedSize);
        return root;
    }

    private static List<string> ExtractYamlReferences(object? root)
    {
        var references = new List<string>();
        var pending = new Stack<object?>();
        var visited = new HashSet<object>(ReferenceComparer.Instance);

        // Aliased schemas can share one discriminator or mapping node, so each is read only once
        // in each role. One node can be both, so the two roles are tracked separately
        var visitedDiscriminators = new HashSet<object>(ReferenceComparer.Instance);
        var visitedMappings = new HashSet<object>(ReferenceComparer.Instance);
        pending.Push(root);

        while (pending.Count > 0)
        {
            var node = pending.Pop();

            // Aliases share one node, so each container is inspected once to keep the walk linear
            if (node is null or string || !visited.Add(node))
                continue;

            VisitYamlNode(node, pending, references, visitedDiscriminators, visitedMappings);
        }

        return references;
    }

    private static void VisitYamlNode(
        object node,
        Stack<object?> pending,
        List<string> references,
        HashSet<object> visitedDiscriminators,
        HashSet<object> visitedMappings)
    {
        if (node is IDictionary<object, object> mapping)
        {
            foreach (var entry in mapping)
            {
                if (entry.Key is string key &&
                    string.Equals(key, "$ref", StringComparison.OrdinalIgnoreCase) &&
                    entry.Value is string value)
                    AddReference(references, value);

                if (entry.Key is string name &&
                    string.Equals(name, "discriminator", StringComparison.OrdinalIgnoreCase) &&
                    entry.Value is IDictionary<object, object> discriminator &&
                    visitedDiscriminators.Add(discriminator))
                {
                    AddDiscriminatorMappingReferences(discriminator, references, visitedMappings);
                }

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

    private static void AddDiscriminatorMappingReferences(
        IDictionary<object, object> discriminator,
        List<string> references,
        HashSet<object> visitedMappings)
    {
        foreach (var entry in discriminator)
        {
            if (entry.Key is string key &&
                string.Equals(key, "mapping", StringComparison.OrdinalIgnoreCase) &&
                entry.Value is IDictionary<object, object> mapping &&
                visitedMappings.Add(mapping))
            {
                foreach (var target in mapping.Values)
                {
                    if (target is string value)
                        AddReference(references, value);
                }
            }
        }
    }

    private static void AddReference(List<string> references, string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > 0)
            references.Add(trimmed);
    }

    internal sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new();

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    internal static ReferenceResolutionException OutsideInputDirectory(
        string reference,
        string resolved,
        string rootDirectory) =>
        new($"Local '$ref' \"{reference}\" resolves to \"{resolved}\", outside the input directory " +
            $"\"{rootDirectory}\". Refitter confines local references to the input document's directory tree.");

    internal static ReferenceResolutionException SymbolicLinkBlocked(string reference, string resolved) =>
        new($"Local '$ref' \"{reference}\" resolves to \"{resolved}\", which is or passes through a symbolic " +
            "link. Refitter does not follow links in referenced files, because a link can point outside the " +
            "input document's directory tree.");

    /// <summary>
    /// Determines whether <paramref name="fullPath"/> or any directory between it and
    /// <paramref name="rootDirectory"/> is a symbolic link or other reparse point. The root and the directories
    /// above it are deliberately not checked, since they are the user's own choice of input location.
    /// </summary>
    internal static bool ContainsLinkBelowRoot(string rootDirectory, string fullPath, string? rootDocumentPath = null)
    {
        var root = Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string? current = Path.GetFullPath(fullPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // The input document is the user's own choice, so it may be a link; no other path is exempt
        if (rootDocumentPath != null &&
            current.Equals(Path.GetFullPath(rootDocumentPath), PathComparison))
        {
            return false;
        }

        while (!string.IsNullOrEmpty(current) && !current.Equals(root, PathComparison))
        {
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }

            current = Path.GetDirectoryName(current);
        }

        return false;
    }

    /// <summary>
    /// Mirrors the file search of OasReader: a file whose path ends with the reference, anywhere below the root.
    /// Directories are enumerated by hand so that links are never followed into a cycle; the files of a
    /// linked directory are still reported, because their path passes through the link.
    /// </summary>
    private static string? FindLinkedSearchMatch(string rootDirectory, string fileReference, string? rootDocumentPath)
    {
        var separator = Path.DirectorySeparatorChar;
        var suffix = fileReference.Replace('\\', separator).Replace('/', separator);
        while (suffix.StartsWith("." + separator, StringComparison.Ordinal))
            suffix = suffix.Substring(2);

        if (suffix.Length == 0)
            return null;

        var pending = new Stack<(string Directory, bool ThroughLink)>();
        pending.Push((Path.GetFullPath(rootDirectory), false));
        while (pending.Count > 0)
        {
            var (directory, throughLink) = pending.Pop();
            try
            {
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    if (file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                        ContainsLinkBelowRoot(rootDirectory, file, rootDocumentPath))
                        return file;
                }

                foreach (var subdirectory in Directory.EnumerateDirectories(directory))
                {
                    var isLink = (File.GetAttributes(subdirectory) & FileAttributes.ReparsePoint) != 0;
                    if (!(isLink && throughLink))
                        pending.Push((subdirectory, throughLink || isLink));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A directory that cannot be listed cannot be searched by the resolver either
            }
        }

        return null;
    }

    internal static bool IsWithin(string rootDirectory, string candidate)
    {
        var root = Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var path = Path.GetFullPath(candidate);

        var comparison = PathComparison;

        return path.Equals(root, comparison) ||
               path.StartsWith(root + Path.DirectorySeparatorChar, comparison) ||
               path.StartsWith(root + Path.AltDirectorySeparatorChar, comparison);
    }

    internal static ReferenceResolutionException RemoteBlocked(string reference, string source) =>
        new($"Remote '$ref' \"{reference}\" in \"{source}\" was blocked. " +
            "Remote reference resolution is disabled by default; pass --allow-remote-refs " +
            "(or set allowRemoteReferences: true) to enable it.");
}
