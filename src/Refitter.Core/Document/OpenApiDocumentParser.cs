using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.References;
using NJsonSchema.Yaml;
using NSwag;
using YamlDotNet.Serialization;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Parses OpenAPI documents the way NSwag does, but first normalizes the document and the local files it
/// references into a form NSwag can read (see <see cref="OpenApiJsonNormalizer"/>).
/// </summary>
internal static class OpenApiDocumentParser
{
    public static Task<OpenApiDocument> FromFileAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        FromFileAsync(path, ReferencePolicy.Unrestricted, cancellationToken);

    public static Task<OpenApiDocument> FromFileAsync(
        string path,
        ReferencePolicy policy,
        CancellationToken cancellationToken = default) =>
        ParseAsync(File.ReadAllText(path), path, PathUtilities.IsYaml(path), policy, cancellationToken);

    public static Task<OpenApiDocument> ParseAsync(
        string content,
        string? documentPath,
        bool isYaml,
        CancellationToken cancellationToken = default) =>
        ParseAsync(content, documentPath, isYaml, ReferencePolicy.Unrestricted, cancellationToken);

    public static Task<OpenApiDocument> ParseAsync(
        string content,
        string? documentPath,
        bool isYaml,
        ReferencePolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (!isYaml)
            DocumentNestingGuard.EnsureWithinLimit(content, documentPath ?? "the OpenAPI document");

        var json = OpenApiJsonNormalizer.NormalizeDocument(isYaml ? ConvertYamlToJson(content, documentPath) : content);
        return OpenApiDocument.FromJsonAsync(
            json,
            documentPath!,
            SchemaType.Swagger2,
            document =>
            {
                // Mirror NSwag: YAML documents resolve references as YAML, JSON documents as JSON
                var schemaResolver = new OpenApiSchemaResolver(document, new SystemTextJsonSchemaGeneratorSettings());
                return isYaml
                    ? new SanitizingYamlReferenceResolver(schemaResolver, policy)
                    : new SanitizingJsonReferenceResolver(schemaResolver, policy);
            },
            cancellationToken);
    }

    // Same conversion as NSwag's OpenApiYamlDocument and NJsonSchema's JsonSchemaYaml
    internal static string ConvertYamlToJson(string yaml, string? source = null)
    {
        // The YAML reader and serializer recurse per nesting level
        DocumentNestingGuard.EnsureYamlWithinLimit(yaml, source ?? "the YAML document");
        var yamlObject = new DeserializerBuilder().Build().Deserialize(new StringReader(yaml));
        return new SerializerBuilder().JsonCompatible().Build().Serialize(yamlObject!);
    }

    private static Task<JsonSchema> LoadReferencedFileAsync(
        string filePath,
        bool isYaml,
        JsonReferenceResolver referenceResolver,
        CancellationToken cancellationToken)
    {
        var json = ReadReferencedContent(File.ReadAllText(filePath), filePath, isYaml);

        // Nested references resolve against the root document, but the guard validated them against this
        // file, so pin them to this file's directory
        json = ReferencedFileReferenceRebaser.Rebase(json, filePath);
        return JsonSchema.FromJsonAsync(json, filePath, _ => referenceResolver, cancellationToken);
    }

    // The resolver downloads the content itself instead of delegating to NJsonSchema, which would parse it
    // with the recursive YAML reader before anything could check it. NJsonSchema's caller handles caching and
    // fragments: it asks for the URL without the fragment and looks into the result.
    private static async Task<JsonSchema> LoadReferencedUrlAsync(
        string url,
        bool isYaml,
        JsonReferenceResolver referenceResolver,
        CancellationToken cancellationToken)
    {
        var content = await RemoteDocumentDownloader.DownloadAsync(url, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var json = ReadReferencedContent(content, url, isYaml);

        // Relative references in a remote document resolve against its URL, which is its document path
        return await JsonSchema.FromJsonAsync(json, url, _ => referenceResolver, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ReadReferencedContent(string content, string source, bool isYaml)
    {
        if (!isYaml)
            DocumentNestingGuard.EnsureWithinLimit(content, source);

        return OpenApiJsonNormalizer.NormalizeReferencedFile(isYaml ? ConvertYamlToJson(content, source) : content);
    }

    // Defense in depth behind ReferenceGuard: the resolver is the code that actually fetches the URL
    private static void EnsureRemoteAllowed(string url, ReferencePolicy policy)
    {
        if (!policy.AllowRemoteReferences)
            throw ReferenceGuard.RemoteBlocked(url, "the OpenAPI document");
    }

    // The resolver is the code that actually reads referenced files, so it confines them itself rather
    // than trusting that the guard resolved the same path
    private static void EnsureFileAllowed(string filePath, ReferencePolicy policy)
    {
        if (policy.RootDirectory == null)
            return;

        var full = Path.GetFullPath(filePath);
        if (!ReferenceGuard.IsWithin(policy.RootDirectory, full))
            throw ReferenceGuard.OutsideInputDirectory(filePath, full, policy.RootDirectory);

        if (ReferenceGuard.ContainsLinkBelowRoot(policy.RootDirectory, full, policy.RootDocumentPath))
            throw ReferenceGuard.SymbolicLinkBlocked(filePath, full);
    }

    private sealed class SanitizingJsonReferenceResolver(JsonSchemaAppender schemaAppender, ReferencePolicy policy)
        : JsonReferenceResolver(schemaAppender)
    {
        public override async Task<IJsonReference> ResolveUrlReferenceAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            EnsureRemoteAllowed(url, policy);
            return await LoadReferencedUrlAsync(url, isYaml: false, this, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IJsonReference> ResolveFileReferenceAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            EnsureFileAllowed(filePath, policy);
            return await LoadReferencedFileAsync(filePath, isYaml: false, this, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class SanitizingYamlReferenceResolver(JsonSchemaAppender schemaAppender, ReferencePolicy policy)
        : JsonAndYamlReferenceResolver(schemaAppender)
    {
        public override async Task<IJsonReference> ResolveUrlReferenceAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            EnsureRemoteAllowed(url, policy);
            return await LoadReferencedUrlAsync(url, isYaml: true, this, cancellationToken).ConfigureAwait(false);
        }

        // JsonAndYamlReferenceResolver reads every referenced file as YAML, which also covers JSON
        public override async Task<IJsonReference> ResolveFileReferenceAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            EnsureFileAllowed(filePath, policy);
            return await LoadReferencedFileAsync(filePath, isYaml: true, this, cancellationToken).ConfigureAwait(false);
        }
    }
}
