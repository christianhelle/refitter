using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.References;
using NJsonSchema.Yaml;
using NSwag;
using YamlDotNet.Serialization;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Parses OpenAPI documents the way NSwag does, but first clamps numeric bounds that do not fit in a
/// <see cref="decimal"/>, both in the document and in the local files it references (#1273), and inlines
/// path items referenced from OpenAPI 3.1 <c>components/pathItems</c> (#1274).
/// </summary>
internal static class OpenApiDocumentParser
{
    public static Task<OpenApiDocument> FromFileAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        ParseAsync(File.ReadAllText(path), path, PathUtilities.IsYaml(path), cancellationToken);

    public static Task<OpenApiDocument> ParseAsync(
        string content,
        string? documentPath,
        bool isYaml,
        CancellationToken cancellationToken = default)
    {
        var json = PathItemReferenceInliner.Inline(
            NumericBoundsSanitizer.Sanitize(isYaml ? ConvertYamlToJson(content) : content));
        return OpenApiDocument.FromJsonAsync(
            json,
            documentPath!,
            SchemaType.Swagger2,
            document =>
            {
                // Mirror NSwag: YAML documents resolve references as YAML, JSON documents as JSON
                var schemaResolver = new OpenApiSchemaResolver(document, new SystemTextJsonSchemaGeneratorSettings());
                return isYaml
                    ? new SanitizingYamlReferenceResolver(schemaResolver)
                    : new SanitizingJsonReferenceResolver(schemaResolver);
            },
            cancellationToken);
    }

    // Same conversion as NSwag's OpenApiYamlDocument and NJsonSchema's JsonSchemaYaml
    internal static string ConvertYamlToJson(string yaml)
    {
        var yamlObject = new DeserializerBuilder().Build().Deserialize(new StringReader(yaml));
        return new SerializerBuilder().JsonCompatible().Build().Serialize(yamlObject!);
    }

    private static Task<JsonSchema> LoadReferencedFileAsync(
        string filePath,
        bool isYaml,
        JsonReferenceResolver referenceResolver,
        CancellationToken cancellationToken)
    {
        var content = File.ReadAllText(filePath);
        var json = NumericBoundsSanitizer.Sanitize(isYaml ? ConvertYamlToJson(content) : content);
        return JsonSchema.FromJsonAsync(json, filePath, _ => referenceResolver, cancellationToken);
    }

    private sealed class SanitizingJsonReferenceResolver(JsonSchemaAppender schemaAppender)
        : JsonReferenceResolver(schemaAppender)
    {
        public override async Task<IJsonReference> ResolveFileReferenceAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            await LoadReferencedFileAsync(filePath, isYaml: false, this, cancellationToken).ConfigureAwait(false);
    }

    private sealed class SanitizingYamlReferenceResolver(JsonSchemaAppender schemaAppender)
        : JsonAndYamlReferenceResolver(schemaAppender)
    {
        // JsonAndYamlReferenceResolver reads every referenced file as YAML, which also covers JSON
        public override async Task<IJsonReference> ResolveFileReferenceAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            await LoadReferencedFileAsync(filePath, isYaml: true, this, cancellationToken).ConfigureAwait(false);
    }
}
