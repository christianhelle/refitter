using System.Text.Json;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Resolves the <c>$ref</c> references of a document read by <see cref="ApiJsonReader"/>.
/// </summary>
/// <remarks>
/// First, named schemas that only alias another schema are replaced by the schema they reference. Then every
/// other reference is resolved: JSON pointers into the document, and references into other files or URLs.
/// Schemas loaded from other documents are added to the document's schemas, so that they are generated.
/// </remarks>
internal sealed class ApiReferenceResolver
{
    private static readonly char[] PathSeparators = ['/', '\\'];

    private readonly ApiDocument document;
    private readonly Func<string, string> loadUrl;
    private readonly Dictionary<string, ExternalDocument> externalDocuments = new(StringComparer.Ordinal);
    private readonly SchemaTypeNameGenerator typeNameGenerator = new();

    public ApiReferenceResolver(ApiDocument document, Func<string, string> loadUrl)
    {
        this.document = document;
        this.loadUrl = loadUrl;
    }

    public void Resolve()
    {
        ReplaceSchemaAliases(document.Definitions, document, document.DocumentPath);
        var visited = new HashSet<object>();
        VisitDocument(document, visited);
    }

    private void ReplaceSchemaAliases(ApiSchemaDictionary definitions, object root, string? documentPath)
    {
        foreach (var definition in definitions.ToArray())
        {
            var schema = definition.Value;
            if (schema.ReferencePath != null && schema.Reference == null)
            {
                var target = ResolveReference(root, documentPath, schema.ReferencePath, ReferenceTarget.Schema, append: false);
                definitions[definition.Key] = (ApiSchema)target;
            }
        }
    }

    private void VisitDocument(ApiDocument apiDocument, HashSet<object> visited)
    {
        foreach (var path in apiDocument.Paths.Values.ToArray())
        {
            VisitPathItem(path, visited, apiDocument);
        }

        foreach (var schema in apiDocument.Definitions.Values.ToArray())
        {
            VisitSchema(schema, visited, apiDocument, apiDocument.DocumentPath);
        }

        foreach (var requestBody in apiDocument.Components.RequestBodies.Values.ToArray())
        {
            VisitRequestBody(requestBody, visited, apiDocument, apiDocument.DocumentPath);
        }

        foreach (var response in apiDocument.Components.Responses.Values.ToArray())
        {
            VisitResponse(response, visited, apiDocument, apiDocument.DocumentPath);
        }

        foreach (var parameter in apiDocument.Components.Parameters.Values.ToArray())
        {
            VisitSchema(parameter, visited, apiDocument, apiDocument.DocumentPath);
        }

        foreach (var header in apiDocument.Components.Headers.Values.ToArray())
        {
            VisitSchema(header, visited, apiDocument, apiDocument.DocumentPath);
        }
    }

    private void VisitPathItem(ApiPathItem pathItem, HashSet<object> visited, ApiDocument root)
    {
        if (pathItem.ReferencePath != null && pathItem.Reference == null)
        {
            pathItem.Reference = (ApiPathItem)ResolveReference(
                root,
                root.DocumentPath,
                pathItem.ReferencePath,
                ReferenceTarget.PathItem,
                append: true);
        }

        foreach (var operation in pathItem.Values.ToArray())
        {
            VisitOperation(operation, visited, root);
        }

        foreach (var parameter in pathItem.Parameters.ToArray())
        {
            VisitSchema(parameter, visited, root, root.DocumentPath);
        }
    }

    private void VisitOperation(ApiOperation operation, HashSet<object> visited, ApiDocument root)
    {
        foreach (var parameter in operation.Parameters.ToArray())
        {
            VisitSchema(parameter, visited, root, root.DocumentPath);
        }

        if (operation.RequestBody != null)
        {
            VisitRequestBody(operation.RequestBody, visited, root, root.DocumentPath);
        }

        foreach (var response in operation.Responses.Values.ToArray())
        {
            VisitResponse(response, visited, root, root.DocumentPath);
        }
    }

    private void VisitRequestBody(ApiRequestBody requestBody, HashSet<object> visited, object root, string? documentPath)
    {
        if (requestBody.ReferencePath != null && requestBody.Reference == null)
        {
            requestBody.Reference = (ApiRequestBody)ResolveReference(
                root,
                documentPath,
                requestBody.ReferencePath,
                ReferenceTarget.RequestBody,
                append: true);
        }

        VisitSchemas(requestBody.Content.Values.Select(mediaType => mediaType.Schema).OfType<ApiSchema>().ToList(), visited, root, documentPath);
    }

    private void VisitResponse(ApiResponse response, HashSet<object> visited, object root, string? documentPath)
    {
        if (response.ReferencePath != null && response.Reference == null)
        {
            response.Reference = (ApiResponse)ResolveReference(
                root,
                documentPath,
                response.ReferencePath,
                ReferenceTarget.Response,
                append: true);
        }

        VisitSchemas(response.Headers.Values.ToArray(), visited, root, documentPath);

        VisitSchemas(response.Content.Values.Select(mediaType => mediaType.Schema).OfType<ApiSchema>().ToList(), visited, root, documentPath);
    }

    private void VisitSchema(ApiSchema schema, HashSet<object> visited, object root, string? documentPath)
    {
        if (!visited.Add(schema))
            return;

        if (schema.ReferencePath != null && schema.Reference == null)
        {
            var target = schema is ApiParameter ? ReferenceTarget.Parameter : ReferenceTarget.Schema;
            schema.Reference = (ApiSchema)ResolveReference(root, documentPath, schema.ReferencePath, target, append: true);
        }

        // Each collection is copied when it is reached, because visiting a schema can resolve references
        VisitOptionalSchema(schema.AdditionalItemsSchema, visited, root, documentPath);
        VisitOptionalSchema(schema.AdditionalPropertiesSchema, visited, root, documentPath);
        VisitOptionalSchema(schema.Item, visited, root, documentPath);
        VisitSchemas(schema.Items.ToArray(), visited, root, documentPath);
        VisitSchemas(schema.AllOf.ToArray(), visited, root, documentPath);
        VisitSchemas(schema.AnyOf.ToArray(), visited, root, documentPath);
        VisitSchemas(schema.OneOf.ToArray(), visited, root, documentPath);
        VisitOptionalSchema(schema.Not, visited, root, documentPath);
        VisitOptionalSchema(schema.DictionaryKey, visited, root, documentPath);

        if (schema.DiscriminatorObject != null)
            VisitSchemas(schema.DiscriminatorObject.Mapping.Values.ToArray(), visited, root, documentPath);

        VisitSchemas(schema.Properties.Values.ToArray(), visited, root, documentPath);
        VisitSchemas(schema.PatternProperties.Values.ToArray(), visited, root, documentPath);
        VisitSchemas(schema.Definitions.Values.ToArray(), visited, root, documentPath);

        if (schema is ApiParameter parameter)
        {
            VisitOptionalSchema(parameter.Schema, visited, root, documentPath);
            VisitOptionalSchema(parameter.CustomSchema, visited, root, documentPath);
        }
    }

    private void VisitOptionalSchema(ApiSchema? schema, HashSet<object> visited, object root, string? documentPath)
    {
        if (schema != null)
            VisitSchema(schema, visited, root, documentPath);
    }

    private void VisitSchemas(IEnumerable<ApiSchema> schemas, HashSet<object> visited, object root, string? documentPath)
    {
        foreach (var schema in schemas)
            VisitSchema(schema, visited, root, documentPath);
    }

    private object ResolveReference(object root, string? documentPath, string referencePath, ReferenceTarget target, bool append)
    {
        if (referencePath == "#")
            return root;

        if (referencePath.StartsWith("#/", StringComparison.Ordinal))
        {
            var resolved = ResolvePointer(root, referencePath)
                ?? throw new InvalidOperationException("Could not resolve the path '" + referencePath + "'.");

            return resolved is RawJsonObject rawObject
                ? ReadRawObject(rawObject, target)
                : resolved;
        }

        var location = GetExternalLocation(documentPath, referencePath);
        try
        {
            var resolved = ResolveExternalReference(location, target);
            if (append && resolved is ApiSchema schema && !document.Definitions.Values.Contains(schema))
            {
                var typeNameHint = GetTypeNameHint(referencePath);
                var key = typeNameGenerator.Generate(schema, typeNameHint, document.Definitions.Keys);
                document.Definitions[key] = schema;
            }

            return resolved;
        }
        catch (Exception exception) when (exception is not ReferenceResolutionException)
        {
            throw new InvalidOperationException(
                "Could not resolve the JSON path '" + referencePath + "' within the file path '" + location + "'.",
                exception);
        }
    }

    /// <summary>The URL or full file path (with any JSON pointer) of a reference to another document.</summary>
    private static string GetExternalLocation(string? documentPath, string referencePath)
    {
        if (IsHttp(referencePath))
            return referencePath;

        if (documentPath != null && IsHttp(documentPath))
            return new Uri(new Uri(documentPath), referencePath).ToString();

        if (documentPath == null)
        {
            throw new NotSupportedException(
                "Could not resolve the JSON path '" + referencePath + "' because no document path is available.");
        }

        var parts = Regex.Split(referencePath, "(?=#)", RegexOptions.None, TimeSpan.FromSeconds(1));
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(documentPath) ?? string.Empty, parts[0])) +
               (parts.Length > 1 ? parts[1] : string.Empty);
    }

    private static bool IsHttp(string path) =>
        path.StartsWith("http://", StringComparison.Ordinal) ||
        path.StartsWith("https://", StringComparison.Ordinal);

    /// <summary>Reads a JSON object that was kept as-is (e.g. extension data) as the referenced kind of object.</summary>
    private object ReadRawObject(RawJsonObject rawObject, ReferenceTarget target)
    {
        using var json = JsonDocument.Parse(RawJson.ToIndentedString(rawObject), ApiDocumentLoader.JsonOptions);
        var reader = new ApiJsonReader(document.SchemaType);
        object result = target switch
        {
            ReferenceTarget.Parameter => reader.ReadParameter(json.RootElement),
            ReferenceTarget.Response => reader.ReadResponse(json.RootElement),
            ReferenceTarget.RequestBody => reader.ReadRequestBody(json.RootElement),
            ReferenceTarget.PathItem => reader.ReadPathItem(json.RootElement),
            _ => reader.ReadSchema(json.RootElement),
        };

        var visited = new HashSet<object>();
        switch (result)
        {
            case ApiSchema schema:
                VisitSchema(schema, visited, document, document.DocumentPath);
                break;
            case ApiResponse response:
                VisitResponse(response, visited, document, document.DocumentPath);
                break;
            case ApiRequestBody requestBody:
                VisitRequestBody(requestBody, visited, document, document.DocumentPath);
                break;
            case ApiPathItem pathItem:
                VisitPathItem(pathItem, visited, document);
                break;
        }

        return result;
    }

    private object ResolveExternalReference(string location, ReferenceTarget target)
    {
        var hashIndex = location.IndexOf('#');
        var documentLocation = hashIndex >= 0 ? location.Substring(0, hashIndex) : location;
        var pointer = hashIndex >= 0 ? location.Substring(hashIndex) : "#";

        if (!externalDocuments.TryGetValue(documentLocation, out var externalDocument))
        {
            var isHttp = documentLocation.StartsWith("http://", StringComparison.Ordinal) ||
                         documentLocation.StartsWith("https://", StringComparison.Ordinal);
            var content = isHttp ? loadUrl(documentLocation) : File.ReadAllText(documentLocation);
            var isYaml = PathUtilities.IsYaml(documentLocation) || !content.TrimStart().StartsWith("{", StringComparison.Ordinal);
            content = ApiDocumentLoader.PrepareJson(content, isYaml);

            externalDocument = new ExternalDocument(documentLocation, JsonDocument.Parse(content, ApiDocumentLoader.JsonOptions));
            externalDocuments[documentLocation] = externalDocument;
        }

        return externalDocument.Resolve(pointer, target, this);
    }

    /// <summary>Resolves a JSON pointer against the objects of a document.</summary>
    private static object? ResolvePointer(object root, string pointer)
    {
        var segments = pointer
            .Split('/')
            .Skip(1)
            .Select(s => Uri.UnescapeDataString(s).Replace("~1", "/").Replace("~0", "~"))
            .ToList();

        return ResolvePointer(root, segments, new HashSet<object>());
    }

    private static object? ResolvePointer(object? node, List<string> segments, HashSet<object> checkedNodes)
    {
        if (node == null || node is string || checkedNodes.Contains(node))
            return null;

        var reference = node switch
        {
            ApiSchema { Reference: not null } schema => (object)schema.Reference,
            ApiResponse { Reference: not null } response => response.Reference,
            ApiRequestBody { Reference: not null } requestBody => requestBody.Reference,
            ApiPathItem { Reference: not null } pathItem => pathItem.Reference,
            _ => null,
        };

        if (reference != null)
        {
            return ResolvePointerWithoutDereferencing(reference, segments, checkedNodes)
                ?? ResolvePointerWithoutDereferencing(node, segments, checkedNodes);
        }

        return ResolvePointerWithoutDereferencing(node, segments, checkedNodes);
    }

    private static object? ResolvePointerWithoutDereferencing(object node, List<string> segments, HashSet<object> checkedNodes)
    {
        if (segments.Count == 0)
            return node;

        checkedNodes.Add(node);
        var child = GetChild(node, segments[0]);
        return ResolvePointer(child, segments.Skip(1).ToList(), checkedNodes);
    }

    private static object? GetChild(object node, string segment)
    {
        // Like any other object with extension data, extension data is looked at first
        var extensionData = node switch
        {
            ApiDocument apiDocument => GetExtensionData(apiDocument.ExtensionData, segment),
            ApiOperation apiOperation => GetExtensionData(apiOperation.ExtensionData, segment),
            ApiResponse response => GetExtensionData(response.ExtensionData, segment),
            _ => null,
        };

        if (extensionData != null)
            return extensionData;

        return node switch
        {
            ApiDocument apiDocument => GetDocumentChild(apiDocument, segment),
            ApiComponents components => GetComponentsChild(components, segment),
            ApiPathItem pathItem => GetPathItemChild(pathItem, segment),
            ApiOperation apiOperation => GetOperationChild(apiOperation, segment),
            ApiResponse response => GetResponseChild(response, segment),
            ApiSchema schema => GetSchemaChild(schema, segment),
            _ => GetPartChild(node, segment),
        };
    }

    private static object? GetComponentsChild(ApiComponents components, string segment) =>
        segment switch
        {
            "schemas" => components.Schemas,
            "requestBodies" => components.RequestBodies,
            "responses" => components.Responses,
            OpenApiKeywords.Parameters => components.Parameters,
            "headers" => components.Headers,
            "securitySchemes" => components.SecuritySchemes,
            _ => null,
        };

    private static object? GetPathItemChild(ApiPathItem pathItem, string segment)
    {
        if (pathItem.TryGetValue(segment, out var operation))
            return operation;

        return segment == OpenApiKeywords.Parameters ? pathItem.Parameters : GetExtensionData(pathItem.ExtensionData, segment);
    }

    private static object? GetOperationChild(ApiOperation apiOperation, string segment) =>
        segment switch
        {
            OpenApiKeywords.Parameters => apiOperation.Parameters,
            "requestBody" => apiOperation.RequestBody,
            "responses" => apiOperation.Responses,
            _ => GetExtensionData(apiOperation.ExtensionData, segment),
        };

    private static object? GetResponseChild(ApiResponse response, string segment) =>
        segment switch
        {
            "content" => response.Content,
            "schema" => response.Schema,
            "headers" => response.Headers,
            _ => GetExtensionData(response.ExtensionData, segment),
        };

    /// <summary>The child of the smaller parts of a document, or of a collection.</summary>
    private static object? GetPartChild(object node, string segment) =>
        node switch
        {
            ApiRequestBody requestBody => segment == "content" ? requestBody.Content : null,
            ApiRequestBodyContent content => content.TryGetValue(segment, out var mediaType) ? mediaType : null,
            ApiMediaType mediaType => segment == "schema" ? mediaType.Schema : null,
            ApiDiscriminator discriminator => segment == "mapping" ? discriminator.Mapping : null,
            _ => GetCollectionChild(node, segment),
        };

    private static object? GetCollectionChild(object node, string segment) =>
        node switch
        {
            RawJsonObject rawObject => rawObject.TryGetValue(segment, out var rawValue) ? rawValue : null,
            RawJsonArray rawArray => GetItem(rawArray.Items, segment),
            ApiSchemaDictionary schemaDictionary => GetValue(schemaDictionary, segment),
            ApiSchemaPropertyDictionary propertyDictionary => GetValue(propertyDictionary, segment),
            System.Collections.IDictionary dictionary => dictionary.Contains(segment) ? dictionary[segment] : null,
            System.Collections.IEnumerable enumerable => GetItem(enumerable.Cast<object?>().ToArray(), segment),
            _ => null,
        };

    private static object? GetValue<T>(IDictionary<string, T> dictionary, string segment) =>
        dictionary.TryGetValue(segment, out var value) ? value : null;

    private static object? GetItem(IList<object?> items, string segment) =>
        int.TryParse(segment, out var index) && index < items.Count ? items[index] : null;

    private static object? GetDocumentChild(ApiDocument apiDocument, string segment)
    {
        switch (segment)
        {
            case "paths":
                return apiDocument.Paths;
            case "components":
                return apiDocument.Components;
            case "definitions":
                return apiDocument.Definitions;
            case OpenApiKeywords.Parameters:
                return apiDocument.Parameters;
            case "responses":
                return apiDocument.Responses;
            case "securityDefinitions":
                return apiDocument.SecurityDefinitions;
            case "tags":
                return apiDocument.Tags;
            default:
                return GetExtensionData(apiDocument.ExtensionData, segment);
        }
    }

    private static object? GetSchemaChild(ApiSchema schema, string segment)
    {
        var extensionData = GetExtensionData(schema.ExtensionData, segment);
        if (extensionData != null)
            return extensionData;

        if (schema is ApiParameter parameter && segment is "schema" or "x-schema")
            return segment == "schema" ? parameter.Schema : parameter.CustomSchema;

        switch (segment)
        {
            case "properties":
                return NonEmpty(schema.Properties);
            case "patternProperties":
                return NonEmpty(schema.PatternProperties);
            case "definitions":
                return NonEmpty(schema.Definitions);
            case "items":
                return (object?)schema.Item ?? NonEmpty(schema.Items);
            case "additionalProperties":
                return schema.AdditionalPropertiesSchema;
            case "additionalItems":
                return schema.AdditionalItemsSchema;
            case "allOf":
                return NonEmpty(schema.AllOf);
            case "anyOf":
                return NonEmpty(schema.AnyOf);
            case "oneOf":
                return NonEmpty(schema.OneOf);
            case "not":
                return schema.Not;
            case "x-dictionaryKey":
                return schema.DictionaryKey;
            case "discriminator":
                return schema.DiscriminatorObject;
            default:
                return null;
        }
    }

    /// <summary>The last segment of a path or pointer, without its file extension.</summary>
    private static string GetTypeNameHint(string path)
    {
        var segments = path.Split(PathSeparators);
        return segments[segments.Length - 1].Split('.')[0];
    }

    private static object? NonEmpty<T>(ICollection<T> collection) => collection.Count > 0 ? collection : null;

    private static object? GetExtensionData(Dictionary<string, object?>? extensionData, string segment) =>
        extensionData != null && extensionData.TryGetValue(segment, out var value) ? value : null;

    internal enum ReferenceTarget
    {
        Schema,
        Parameter,
        Response,
        RequestBody,
        PathItem,
    }

    /// <summary>Another document that references point into. Its objects are read on demand.</summary>
    private sealed class ExternalDocument(string location, JsonDocument json)
    {
        private readonly Dictionary<string, object> resolvedObjects = new(StringComparer.Ordinal);

        public object Resolve(string pointer, ReferenceTarget target, ApiReferenceResolver resolver)
        {
            var key = pointer + "|" + target;
            if (resolvedObjects.TryGetValue(key, out var resolved))
                return resolved;

            var element = json.RootElement;
            foreach (var segment in pointer.Split('/').Skip(1))
            {
                var name = Uri.UnescapeDataString(segment).Replace("~1", "/").Replace("~0", "~");
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var child))
                {
                    element = child;
                }
                else if (element.ValueKind == JsonValueKind.Array &&
                         int.TryParse(name, out var index) &&
                         index < element.GetArrayLength())
                {
                    element = element[index];
                }
                else
                {
                    throw new InvalidOperationException("Could not resolve the path '" + pointer + "'.");
                }
            }

            // Other documents are read as plain JSON schemas, like before
            var reader = new ApiJsonReader(ApiSchemaType.JsonSchema);
            object result = target switch
            {
                ReferenceTarget.Parameter => reader.ReadParameter(element),
                ReferenceTarget.Response => throw new NotSupportedException("References to responses in other documents are not supported."),
                ReferenceTarget.RequestBody => throw new NotSupportedException("References to request bodies in other documents are not supported."),
                ReferenceTarget.PathItem => throw new NotSupportedException("References to path items in other documents are not supported."),
                _ => reader.ReadSchema(element),
            };

            resolvedObjects[key] = result;
            if (result is ApiSchema schema)
            {
                if (pointer == "#")
                    schema.DocumentPath = location;

                // References inside the other document are relative to it
                var root = new ExternalRoot(json.RootElement, this, resolver);
                resolver.ResolveSchemaReferences(schema, root, location);
            }

            return result;
        }
    }

    /// <summary>The root of another document, which local pointers of its schemas resolve against.</summary>
    private sealed record ExternalRoot(JsonElement Element, ExternalDocument Document, ApiReferenceResolver Resolver);

    private void ResolveSchemaReferences(ApiSchema schema, ExternalRoot root, string location)
    {
        var visited = new HashSet<object>();
        VisitExternalSchema(schema, visited, root, location);
    }

    private void VisitExternalSchema(ApiSchema schema, HashSet<object> visited, ExternalRoot root, string location)
    {
        if (!visited.Add(schema))
            return;

        if (schema.ReferencePath != null && schema.Reference == null)
        {
            var target = schema is ApiParameter ? ReferenceTarget.Parameter : ReferenceTarget.Schema;
            var referencePath = schema.ReferencePath;
            schema.Reference = referencePath.StartsWith("#", StringComparison.Ordinal)
                ? ResolveExternalLocal(root, referencePath, target)
                : (ApiSchema)ResolveReference(document, location, referencePath, target, append: true);
        }

        foreach (var child in GetChildSchemas(schema))
        {
            VisitExternalSchema(child, visited, root, location);
        }
    }

    private ApiSchema ResolveExternalLocal(ExternalRoot root, string pointer, ReferenceTarget target)
    {
        var resolved = (ApiSchema)root.Document.Resolve(pointer, target, root.Resolver);
        if (!document.Definitions.Values.Contains(resolved))
        {
            var typeNameHint = GetTypeNameHint(pointer);
            var key = typeNameGenerator.Generate(resolved, typeNameHint, document.Definitions.Keys);
            document.Definitions[key] = resolved;
        }

        return resolved;
    }

    private static IEnumerable<ApiSchema> GetChildSchemas(ApiSchema schema) =>
        EnumerateChildSchemas(schema).OfType<ApiSchema>();

    private static IEnumerable<ApiSchema?> EnumerateChildSchemas(ApiSchema schema)
    {
        yield return schema.AdditionalItemsSchema;
        yield return schema.AdditionalPropertiesSchema;
        yield return schema.Item;
        foreach (var item in schema.Items.Concat(schema.AllOf).Concat(schema.AnyOf).Concat(schema.OneOf))
            yield return item;
        yield return schema.Not;
        yield return schema.DictionaryKey;
        foreach (var mapping in schema.DiscriminatorObject?.Mapping.Values ?? Enumerable.Empty<ApiSchema>())
            yield return mapping;
        foreach (var property in schema.Properties.Values.Concat<ApiSchema>(schema.PatternProperties.Values))
            yield return property;
        foreach (var definition in schema.Definitions.Values)
            yield return definition;

        var parameter = schema as ApiParameter;
        yield return parameter?.Schema;
        yield return parameter?.CustomSchema;
    }
}
