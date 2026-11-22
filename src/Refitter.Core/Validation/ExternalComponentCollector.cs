using System.Text;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Refitter.Core.Validation;

/// <summary>
/// Collects the components that an OpenAPI document reaches through external references, following them from
/// file to file through the <see cref="DocumentSet"/>. Each referenced file is parsed on its own, against its own
/// location, so two files can define a component with the same name, and a file without an OpenAPI version is
/// parsed as the version of the OpenAPI document. The walker does not follow references, so the collected
/// components are checked and counted by walking them as a document of their own.
/// </summary>
internal sealed class ExternalComponentCollector
{
    private readonly DocumentSet set;
    private readonly string rootKey;
    private readonly OpenApiSpecVersion version;
    private readonly KeyValuePair<string, JsonNode?> versionField;
    private readonly Dictionary<string, OpenApiDocument?> documents = new(StringComparer.Ordinal);
    private readonly HashSet<string> reached = new(StringComparer.Ordinal);
    private readonly Queue<(string Host, OpenApiDocument Item)> pending = new();

    private ExternalComponentCollector(DocumentSet set, string rootKey, OpenApiSpecVersion version)
    {
        this.set = set;
        this.rootKey = rootKey;
        this.version = version;
        versionField = ReadVersionField(set.RootJson);
    }

    private OpenApiComponents Components { get; } = new()
    {
        Schemas = new Dictionary<string, IOpenApiSchema>(),
        Responses = new Dictionary<string, IOpenApiResponse>(),
        Parameters = new Dictionary<string, IOpenApiParameter>(),
        Examples = new Dictionary<string, IOpenApiExample>(),
        RequestBodies = new Dictionary<string, IOpenApiRequestBody>(),
        Headers = new Dictionary<string, IOpenApiHeader>(),
        SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(),
        Links = new Dictionary<string, IOpenApiLink>(),
        Callbacks = new Dictionary<string, IOpenApiCallback>(),
        PathItems = new Dictionary<string, IOpenApiPathItem>(),
    };

    /// <summary>
    /// Collects the components that a local OpenAPI document reaches in other files, directly or through
    /// references inside those files. A component that is reached more than once is collected once.
    /// </summary>
    public static async Task<OpenApiComponents> CollectAsync(
        DocumentSet set,
        OpenApiDocument document,
        OpenApiSpecVersion version,
        CancellationToken cancellationToken = default)
    {
        var collector = new ExternalComponentCollector(set, set.RootDocumentPath!, version);
        collector.pending.Enqueue((collector.rootKey, document));

        while (collector.pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (host, item) = collector.pending.Dequeue();
            var visitor = new ReferenceVisitor();
            new OpenApiWalker(visitor).Walk(item);

            foreach (var reference in visitor.References)
                await collector.ReachAsync(host, reference, cancellationToken).ConfigureAwait(false);
        }

        return collector.Components;
    }

    private async Task ReachAsync(string host, BaseOpenApiReference reference, CancellationToken cancellationToken)
    {
        // The components of the OpenAPI document itself are walked with it
        if (!reference.IsExternal && host == rootKey)
            return;

        var file = reference.IsExternal ? Resolve(host, reference.ExternalResource) : host;
        // A reference back into the OpenAPI document reaches a component that is walked with it
        if (file is null || file == rootKey || reference.Id is null)
            return;

        if (!reached.Add(reference.Type + "|" + file + "#" + reference.Id))
            return;

        if (!set.TryGetFile(file, out var json) && !set.TryGetUrl(file, out json))
            return;

        var components = (await ParseAsync(file, json, cancellationToken).ConfigureAwait(false))?.Components;
        var id = reference.Id;
        var item = reference.Type switch
        {
            ReferenceType.Schema => Collect(
                components?.Schemas, Components.Schemas!, file, id, (c, d) => c.Schemas = d, WholeFileSchema(reference, json)),
            ReferenceType.Response => Collect(components?.Responses, Components.Responses!, file, id, (c, d) => c.Responses = d),
            ReferenceType.Parameter => Collect(components?.Parameters, Components.Parameters!, file, id, (c, d) => c.Parameters = d),
            ReferenceType.Example => Collect(components?.Examples, Components.Examples!, file, id, (c, d) => c.Examples = d),
            ReferenceType.RequestBody => Collect(
                components?.RequestBodies, Components.RequestBodies!, file, id, (c, d) => c.RequestBodies = d),
            ReferenceType.Header => Collect(components?.Headers, Components.Headers!, file, id, (c, d) => c.Headers = d),
            ReferenceType.SecurityScheme => Collect(
                components?.SecuritySchemes, Components.SecuritySchemes!, file, id, (c, d) => c.SecuritySchemes = d),
            ReferenceType.Link => Collect(components?.Links, Components.Links!, file, id, (c, d) => c.Links = d),
            ReferenceType.Callback => Collect(components?.Callbacks, Components.Callbacks!, file, id, (c, d) => c.Callbacks = d),
            ReferenceType.PathItem => Collect(components?.PathItems, Components.PathItems!, file, id, (c, d) => c.PathItems = d),
            _ => null,
        };

        if (item is not null)
            pending.Enqueue((file, item));
    }

    // Adds the component the file defines under the id, and returns a document holding only it, to walk for the
    // references it holds
    private static OpenApiDocument? Collect<T>(
        IDictionary<string, T>? defined,
        IDictionary<string, T> collected,
        string file,
        string id,
        Action<OpenApiComponents, IDictionary<string, T>> assign,
        T? fallback = null)
        where T : class
    {
        var component = defined is not null && defined.TryGetValue(id, out var found) ? found : fallback;
        if (component is null)
            return null;

        // Two files can define a component with the same name, so the second one is keyed by its file too
        var name = collected.ContainsKey(id) ? file + "#" + id : id;
        collected[name] = component;

        var item = new OpenApiComponents();
        assign(item, new Dictionary<string, T> { [name] = component });
        return new OpenApiDocument { Components = item };
    }

    // A reference to a whole file has the file name as its id, and the file is a schema of its own
    private IOpenApiSchema? WholeFileSchema(BaseOpenApiReference reference, string json) =>
        reference.IsExternal && reference.Id == Path.GetFileName(reference.ExternalResource)
            ? ParseSchema(json)
            : null;

    // A reference is resolved against the file it is written in. A remote document is keyed by its URL
    // as written, and a local file by its full path, as the reader keyed them
    private static string? Resolve(string host, string? externalResource)
    {
        if (string.IsNullOrWhiteSpace(externalResource))
            return null;

        try
        {
            var isAbsolute = Uri.TryCreate(externalResource, UriKind.Absolute, out var absolute);
            if (isAbsolute && absolute!.Scheme is "http" or "https")
                return externalResource;

            if (PathUtilities.IsHttp(host))
                return new Uri(new Uri(host), externalResource).ToString();

            var uri = isAbsolute && absolute!.IsFile ? absolute : new Uri(new Uri(host), externalResource);
            return uri.IsFile ? Path.GetFullPath(uri.LocalPath) : null;
        }
        catch (Exception ex) when (ex is UriFormatException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private async Task<OpenApiDocument?> ParseAsync(string file, string json, CancellationToken cancellationToken)
    {
        if (documents.TryGetValue(file, out var cached))
            return cached;

        OpenApiDocument? document = null;
        if (JsonObjectOf(json) is { } content)
        {
            // Microsoft.OpenApi reads no document without a version, and a referenced file often has none
            if (!content.ContainsKey("openapi") && !content.ContainsKey("swagger") && versionField.Value is not null)
                content[versionField.Key] = versionField.Value.DeepClone();

            var settings = new OpenApiReaderSettings { RuleSet = ValidationRuleSet.GetEmptyRuleSet() };
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content.ToJsonString()));
            document = (await OpenApiDocument.LoadAsync(stream, "json", settings, cancellationToken).ConfigureAwait(false))
                .Document;
        }

        documents[file] = document;
        return document;
    }

    private IOpenApiSchema? ParseSchema(string json)
    {
        if (JsonObjectOf(json) is null)
            return null;

        return OpenApiModelFactory.Parse<OpenApiSchema>(json, version, new OpenApiDocument(), out _, "json");
    }

    private static JsonObject? JsonObjectOf(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static KeyValuePair<string, JsonNode?> ReadVersionField(string rootJson)
    {
        var root = JsonObjectOf(rootJson);
        foreach (var key in new[] { "openapi", "swagger" })
        {
            if (root?[key] is { } value)
                return new KeyValuePair<string, JsonNode?>(key, value);
        }

        return new KeyValuePair<string, JsonNode?>("openapi", null);
    }

    private sealed class ReferenceVisitor : OpenApiVisitorBase
    {
        public List<BaseOpenApiReference> References { get; } = new();

        public override void Visit(IOpenApiReferenceHolder referenceHolder)
        {
            BaseOpenApiReference? reference = referenceHolder switch
            {
                OpenApiSchemaReference holder => holder.Reference,
                OpenApiResponseReference holder => holder.Reference,
                OpenApiParameterReference holder => holder.Reference,
                OpenApiExampleReference holder => holder.Reference,
                OpenApiRequestBodyReference holder => holder.Reference,
                OpenApiHeaderReference holder => holder.Reference,
                OpenApiSecuritySchemeReference holder => holder.Reference,
                OpenApiLinkReference holder => holder.Reference,
                OpenApiCallbackReference holder => holder.Reference,
                OpenApiPathItemReference holder => holder.Reference,
                _ => null,
            };

            if (reference is not null)
                References.Add(reference);
        }
    }
}
