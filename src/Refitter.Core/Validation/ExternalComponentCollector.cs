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
        if (file is null || reference.Id is null)
            return;

        var type = reference.Type;

        if (!reached.Add(type + "|" + file + "#" + reference.Id))
            return;

        var target = await FindAsync(file, type, reference, cancellationToken).ConfigureAwait(false);
        if (target is null)
            return;

        // Two files can define a component with the same name, so the second one is keyed by its file too
        var name = reference.Id;
        if (Contains(type, name))
            name = file + "#" + reference.Id;

        var item = Add(type, name, target);
        if (item is not null)
            pending.Enqueue((file, item));
    }

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

    private async Task<object?> FindAsync(
        string file,
        ReferenceType type,
        BaseOpenApiReference reference,
        CancellationToken cancellationToken)
    {
        if (!set.TryGetFile(file, out var json) && !set.TryGetUrl(file, out json))
            return null;

        var document = await ParseAsync(file, json, cancellationToken).ConfigureAwait(false);
        var component = Find(document?.Components, type, reference.Id!);
        if (component is not null)
            return component;

        // A reference to a whole file has the file name as its id, and the file is a schema of its own
        if (type == ReferenceType.Schema
            && reference.IsExternal
            && reference.Id == Path.GetFileName(reference.ExternalResource))
        {
            return ParseSchema(json);
        }

        return null;
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

    private static object? Find(OpenApiComponents? components, ReferenceType type, string id) =>
        components is null
            ? null
            : type switch
            {
                ReferenceType.Schema => Find(components.Schemas, id),
                ReferenceType.Response => Find(components.Responses, id),
                ReferenceType.Parameter => Find(components.Parameters, id),
                ReferenceType.Example => Find(components.Examples, id),
                ReferenceType.RequestBody => Find(components.RequestBodies, id),
                ReferenceType.Header => Find(components.Headers, id),
                ReferenceType.SecurityScheme => Find(components.SecuritySchemes, id),
                ReferenceType.Link => Find(components.Links, id),
                ReferenceType.Callback => Find(components.Callbacks, id),
                ReferenceType.PathItem => Find(components.PathItems, id),
                _ => null,
            };

    private static object? Find<T>(IDictionary<string, T>? components, string id) where T : class =>
        components is not null && components.TryGetValue(id, out var component) ? component : null;

    private bool Contains(ReferenceType type, string name) => type switch
    {
        ReferenceType.Schema => Components.Schemas!.ContainsKey(name),
        ReferenceType.Response => Components.Responses!.ContainsKey(name),
        ReferenceType.Parameter => Components.Parameters!.ContainsKey(name),
        ReferenceType.Example => Components.Examples!.ContainsKey(name),
        ReferenceType.RequestBody => Components.RequestBodies!.ContainsKey(name),
        ReferenceType.Header => Components.Headers!.ContainsKey(name),
        ReferenceType.SecurityScheme => Components.SecuritySchemes!.ContainsKey(name),
        ReferenceType.Link => Components.Links!.ContainsKey(name),
        ReferenceType.Callback => Components.Callbacks!.ContainsKey(name),
        ReferenceType.PathItem => Components.PathItems!.ContainsKey(name),
        _ => false,
    };

    // Adds the component, and returns a document holding only it, to walk for the references it holds
    private OpenApiDocument? Add(ReferenceType type, string name, object target)
    {
        var item = new OpenApiComponents();
        switch (type, target)
        {
            case (ReferenceType.Schema, IOpenApiSchema schema):
                Components.Schemas![name] = schema;
                item.Schemas = new Dictionary<string, IOpenApiSchema> { [name] = schema };
                break;
            case (ReferenceType.Response, IOpenApiResponse response):
                Components.Responses![name] = response;
                item.Responses = new Dictionary<string, IOpenApiResponse> { [name] = response };
                break;
            case (ReferenceType.Parameter, IOpenApiParameter parameter):
                Components.Parameters![name] = parameter;
                item.Parameters = new Dictionary<string, IOpenApiParameter> { [name] = parameter };
                break;
            case (ReferenceType.Example, IOpenApiExample example):
                Components.Examples![name] = example;
                item.Examples = new Dictionary<string, IOpenApiExample> { [name] = example };
                break;
            case (ReferenceType.RequestBody, IOpenApiRequestBody requestBody):
                Components.RequestBodies![name] = requestBody;
                item.RequestBodies = new Dictionary<string, IOpenApiRequestBody> { [name] = requestBody };
                break;
            case (ReferenceType.Header, IOpenApiHeader header):
                Components.Headers![name] = header;
                item.Headers = new Dictionary<string, IOpenApiHeader> { [name] = header };
                break;
            case (ReferenceType.SecurityScheme, IOpenApiSecurityScheme securityScheme):
                Components.SecuritySchemes![name] = securityScheme;
                item.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme> { [name] = securityScheme };
                break;
            case (ReferenceType.Link, IOpenApiLink link):
                Components.Links![name] = link;
                item.Links = new Dictionary<string, IOpenApiLink> { [name] = link };
                break;
            case (ReferenceType.Callback, IOpenApiCallback callback):
                Components.Callbacks![name] = callback;
                item.Callbacks = new Dictionary<string, IOpenApiCallback> { [name] = callback };
                break;
            case (ReferenceType.PathItem, IOpenApiPathItem pathItem):
                Components.PathItems![name] = pathItem;
                item.PathItems = new Dictionary<string, IOpenApiPathItem> { [name] = pathItem };
                break;
            default:
                return null;
        }

        return new OpenApiDocument { Components = item };
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
