using System.Text.Json;
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
    private readonly KeyValuePair<string, JsonNode?> versionField;
    private static readonly OpenApiReaderSettings Settings = new() { RuleSet = ValidationRuleSet.GetEmptyRuleSet() };

    private readonly Dictionary<string, JsonObject?> nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OpenApiDocument?> documents = new(StringComparer.Ordinal);
    private readonly HashSet<string> reached = new(StringComparer.Ordinal);
    private readonly Queue<(string Host, OpenApiDocument Item)> pending = new();

    private ExternalComponentCollector(DocumentSet set, string rootKey)
    {
        this.set = set;
        this.rootKey = rootKey;
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
    public static OpenApiComponents Collect(
        DocumentSet set,
        OpenApiDocument document,
        CancellationToken cancellationToken = default)
    {
        var collector = new ExternalComponentCollector(set, set.RootDocumentPath!);
        collector.pending.Enqueue((collector.rootKey, document));

        while (collector.pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (host, item) = collector.pending.Dequeue();
            var visitor = new ReferenceVisitor();
            new OpenApiWalker(visitor).Walk(item);

            foreach (var reference in visitor.References)
                collector.Reach(host, reference);
        }

        return collector.Components;
    }

    private void Reach(string host, BaseOpenApiReference reference)
    {
        // The components of the OpenAPI document itself are walked with it
        if (!reference.IsExternal && host == rootKey)
            return;

        var file = reference.IsExternal ? Resolve(host, reference.ExternalResource) : host;
        // A reference back into the OpenAPI document reaches a component that is walked with it
        if (file is null || file == rootKey || reference.Id is null)
            return;

        if (!Kinds.TryGetValue(reference.Type, out var kind) || !reached.Add(reference.Type + "|" + file + "#" + reference.Id))
            return;

        if (!set.TryGetFile(file, out var json) && !set.TryGetUrl(file, out json))
            return;

        var node = NodeOf(file, json);
        var components = DocumentOf(file, node)?.Components;
        var id = reference.Id;

        // A reference to a whole file has the file name as its id, followed by '#' for an empty fragment, and
        // the file is the component itself
        var name = Path.GetFileName(reference.ExternalResource);
        var whole = reference.IsExternal && (id == name || id == name + "#") ? node : null;

        var item = kind.Add(this, components, file, id, whole);
        if (item is not null)
            pending.Enqueue((file, item));
    }

    // A whole file is read as the only component of a document of its own, so that a file holding only a
    // reference stays a reference, to follow, rather than being read as an empty component
    private OpenApiComponents? WholeFile(string file, JsonObject node, string section, string swaggerSection)
    {
        var components = new JsonObject { [WholeFileComponent] = node.DeepClone() };
        var document = new JsonObject { [versionField.Key] = versionField.Value?.DeepClone() };
        if (versionField.Key == SwaggerField)
            document[swaggerSection] = components;
        else
            document["components"] = new JsonObject { [section] = components };

        return new OpenApiJsonReader().Read(document, new Uri(file), Settings).Document?.Components;
    }

    // A reference is resolved against the file it is written in. A remote document is keyed by its URL
    // as written, and a local file by its full path, as the reader keyed them
    private static string? Resolve(string host, string? externalResource)
    {
        try
        {
            // An external reference always names a resource
            var isAbsolute = Uri.TryCreate(externalResource, UriKind.Absolute, out var absolute);
            if (isAbsolute && absolute!.Scheme is "http" or "https")
                return externalResource;

            if (PathUtilities.IsHttp(host))
                return new Uri(new Uri(host), externalResource!).ToString();

            var uri = isAbsolute ? absolute! : new Uri(new Uri(host), externalResource!);
            return uri.IsFile ? Path.GetFullPath(uri.LocalPath) : null;
        }
        catch (Exception ex) when (ex is UriFormatException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private JsonObject? NodeOf(string file, string json)
    {
        if (!nodes.TryGetValue(file, out var node))
            nodes[file] = node = JsonObjectOf(json);

        return node;
    }

    private OpenApiDocument? DocumentOf(string file, JsonObject? node)
    {
        if (documents.TryGetValue(file, out var cached))
            return cached;

        OpenApiDocument? document = null;
        if (node?.DeepClone() is JsonObject content)
        {
            // Microsoft.OpenApi reads no document without a version, and a referenced file often has none
            if (!content.ContainsKey(OpenApiField) && !content.ContainsKey(SwaggerField))
                content[versionField.Key] = versionField.Value?.DeepClone();

            document = new OpenApiJsonReader().Read(content, new Uri(file), Settings).Document;
        }

        documents[file] = document;
        return document;
    }

    // Read with the depth the reader accepts, rather than the lower default of System.Text.Json, and handed to
    // Microsoft.OpenApi as it is, so that a file the reader read is never skipped for its depth
    private static JsonObject? JsonObjectOf(string json)
    {
        try
        {
            return JsonNode.Parse(
                json,
                documentOptions: new JsonDocumentOptions { MaxDepth = DocumentNestingGuard.MaxNestingDepth }) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // The OpenAPI document was read, so it is an object with a version
    private static KeyValuePair<string, JsonNode?> ReadVersionField(string rootJson)
    {
        var root = JsonObjectOf(rootJson)!;
        var key = root.ContainsKey(SwaggerField) ? SwaggerField : OpenApiField;
        return new KeyValuePair<string, JsonNode?>(key, root[key]);
    }

    private const string OpenApiField = "openapi";
    private const string SwaggerField = "swagger";
    private const string WholeFileComponent = "whole";

    // Each kind of component with its section in an OpenAPI document, and in a Swagger one where it has one
    private static readonly Dictionary<ReferenceType, IKind> Kinds = new()
    {
        [ReferenceType.Schema] = new Kind<IOpenApiSchema>(
            "schemas", "definitions", c => c.Schemas, (c, d) => c.Schemas = d),
        [ReferenceType.Response] = new Kind<IOpenApiResponse>(
            "responses", "responses", c => c.Responses, (c, d) => c.Responses = d),
        [ReferenceType.Parameter] = new Kind<IOpenApiParameter>(
            "parameters", "parameters", c => c.Parameters, (c, d) => c.Parameters = d),
        [ReferenceType.Example] = new Kind<IOpenApiExample>(
            "examples", "examples", c => c.Examples, (c, d) => c.Examples = d),
        [ReferenceType.RequestBody] = new Kind<IOpenApiRequestBody>(
            "requestBodies", "requestBodies", c => c.RequestBodies, (c, d) => c.RequestBodies = d),
        [ReferenceType.Header] = new Kind<IOpenApiHeader>(
            "headers", "headers", c => c.Headers, (c, d) => c.Headers = d),
        [ReferenceType.SecurityScheme] = new Kind<IOpenApiSecurityScheme>(
            "securitySchemes", "securityDefinitions", c => c.SecuritySchemes, (c, d) => c.SecuritySchemes = d),
        [ReferenceType.Link] = new Kind<IOpenApiLink>(
            "links", "links", c => c.Links, (c, d) => c.Links = d),
        [ReferenceType.Callback] = new Kind<IOpenApiCallback>(
            "callbacks", "callbacks", c => c.Callbacks, (c, d) => c.Callbacks = d),
        [ReferenceType.PathItem] = new Kind<IOpenApiPathItem>(
            "pathItems", "pathItems", c => c.PathItems, (c, d) => c.PathItems = d),
    };

    /// <summary>
    /// A kind of component, and where the components of that kind are kept.
    /// </summary>
    private interface IKind
    {
        /// <summary>
        /// Adds the component the file defines under the id, or the whole file, and returns a document holding
        /// only it, to walk for the references it holds.
        /// </summary>
        OpenApiDocument? Add(
            ExternalComponentCollector collector,
            OpenApiComponents? defined,
            string file,
            string id,
            JsonObject? whole);
    }

    private sealed class Kind<TComponent>(
        string section,
        string swaggerSection,
        Func<OpenApiComponents, IDictionary<string, TComponent>?> get,
        Action<OpenApiComponents, IDictionary<string, TComponent>> set) : IKind
        where TComponent : class
    {
        public OpenApiDocument? Add(
            ExternalComponentCollector collector,
            OpenApiComponents? defined,
            string file,
            string id,
            JsonObject? whole)
        {
            TComponent? component = null;
            if (defined is not null && get(defined) is { } components)
                components.TryGetValue(id, out component);

            if (component is null && whole is not null
                && collector.WholeFile(file, whole, section, swaggerSection) is { } wrapped)
            {
                get(wrapped)?.TryGetValue(WholeFileComponent, out component);
            }

            if (component is null)
                return null;

            // Two files can define a component with the same name, so the second one is keyed by its file too
            var collected = get(collector.Components)!;
            var name = collected.ContainsKey(id) ? file + "#" + id : id;
            collected[name] = component;

            var item = new OpenApiComponents();
            set(item, new Dictionary<string, TComponent> { [name] = component });
            return new OpenApiDocument { Components = item };
        }
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
