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

        foreach (var mediaType in requestBody.Content.Values.ToArray())
        {
            if (mediaType.Schema != null)
                VisitSchema(mediaType.Schema, visited, root, documentPath);
        }
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

        foreach (var header in response.Headers.Values.ToArray())
        {
            VisitSchema(header, visited, root, documentPath);
        }

        foreach (var mediaType in response.Content.Values.ToArray())
        {
            if (mediaType.Schema != null)
                VisitSchema(mediaType.Schema, visited, root, documentPath);
        }
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

        if (schema.AdditionalItemsSchema != null)
            VisitSchema(schema.AdditionalItemsSchema, visited, root, documentPath);

        if (schema.AdditionalPropertiesSchema != null)
            VisitSchema(schema.AdditionalPropertiesSchema, visited, root, documentPath);

        if (schema.Item != null)
            VisitSchema(schema.Item, visited, root, documentPath);

        foreach (var item in schema.Items.ToArray())
            VisitSchema(item, visited, root, documentPath);

        foreach (var item in schema.AllOf.ToArray())
            VisitSchema(item, visited, root, documentPath);

        foreach (var item in schema.AnyOf.ToArray())
            VisitSchema(item, visited, root, documentPath);

        foreach (var item in schema.OneOf.ToArray())
            VisitSchema(item, visited, root, documentPath);

        if (schema.Not != null)
            VisitSchema(schema.Not, visited, root, documentPath);

        if (schema.DictionaryKey != null)
            VisitSchema(schema.DictionaryKey, visited, root, documentPath);

        if (schema.DiscriminatorObject != null)
        {
            foreach (var mapping in schema.DiscriminatorObject.Mapping.Values.ToArray())
                VisitSchema(mapping, visited, root, documentPath);
        }

        foreach (var property in schema.Properties.Values.ToArray())
            VisitSchema(property, visited, root, documentPath);

        foreach (var property in schema.PatternProperties.Values.ToArray())
            VisitSchema(property, visited, root, documentPath);

        foreach (var definition in schema.Definitions.Values.ToArray())
            VisitSchema(definition, visited, root, documentPath);

        if (schema is ApiParameter parameter)
        {
            if (parameter.Schema != null)
                VisitSchema(parameter.Schema, visited, root, documentPath);

            if (parameter.CustomSchema != null)
                VisitSchema(parameter.CustomSchema, visited, root, documentPath);
        }
    }

    private object ResolveReference(object root, string? documentPath, string referencePath, ReferenceTarget target, bool append)
    {
        if (referencePath == "#")
            return root;

        if (referencePath.StartsWith("#/", StringComparison.Ordinal))
        {
            var resolved = ResolvePointer(root, referencePath, target)
                ?? throw new InvalidOperationException("Could not resolve the path '" + referencePath + "'.");

            return resolved is RawJsonObject rawObject
                ? ReadRawObject(rawObject, target)
                : resolved;
        }

        string location;
        if (referencePath.StartsWith("http://", StringComparison.Ordinal) ||
            referencePath.StartsWith("https://", StringComparison.Ordinal))
        {
            location = referencePath;
        }
        else if (documentPath != null &&
                 (documentPath.StartsWith("http://", StringComparison.Ordinal) ||
                  documentPath.StartsWith("https://", StringComparison.Ordinal)))
        {
            location = new Uri(new Uri(documentPath), referencePath).ToString();
        }
        else if (documentPath != null)
        {
            var parts = Regex.Split(referencePath, "(?=#)", RegexOptions.None, TimeSpan.FromSeconds(1));
            location = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(documentPath) ?? string.Empty, parts[0])) +
                       (parts.Length > 1 ? parts[1] : string.Empty);
        }
        else
        {
            throw new NotSupportedException(
                "Could not resolve the JSON path '" + referencePath + "' because no document path is available.");
        }

        try
        {
            var resolved = ResolveExternalReference(location, target);
            if (append && resolved is ApiSchema schema && !document.Definitions.Values.Contains(schema))
            {
                var typeNameHint = referencePath.Split('/', '\\').Last().Split('.').First();
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
    private static object? ResolvePointer(object root, string pointer, ReferenceTarget target)
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

        switch (node)
        {
            case ApiDocument apiDocument:
                return segment switch
                {
                    "paths" => apiDocument.Paths,
                    "components" => apiDocument.Components,
                    "definitions" => apiDocument.Definitions,
                    "parameters" => apiDocument.Parameters,
                    "responses" => apiDocument.Responses,
                    "securityDefinitions" => apiDocument.SecurityDefinitions,
                    "tags" => apiDocument.Tags,
                    _ => GetExtensionData(apiDocument.ExtensionData, segment),
                };
            case ApiComponents components:
                return segment switch
                {
                    "schemas" => components.Schemas,
                    "requestBodies" => components.RequestBodies,
                    "responses" => components.Responses,
                    "parameters" => components.Parameters,
                    "headers" => components.Headers,
                    "securitySchemes" => components.SecuritySchemes,
                    _ => null,
                };
            case ApiPathItem pathItem:
                if (pathItem.TryGetValue(segment, out var operation))
                    return operation;

                return segment == "parameters" ? pathItem.Parameters : GetExtensionData(pathItem.ExtensionData, segment);
            case ApiOperation apiOperation:
                return segment switch
                {
                    "parameters" => apiOperation.Parameters,
                    "requestBody" => apiOperation.RequestBody,
                    "responses" => apiOperation.Responses,
                    _ => GetExtensionData(apiOperation.ExtensionData, segment),
                };
            case ApiRequestBody requestBody:
                return segment == "content" ? requestBody.Content : null;
            case ApiRequestBodyContent content:
                return content.TryGetValue(segment, out var requestMediaType) ? requestMediaType : null;
            case ApiMediaType mediaType:
                return segment == "schema" ? mediaType.Schema : null;
            case ApiResponse response:
                return segment switch
                {
                    "content" => response.Content,
                    "schema" => response.Schema,
                    "headers" => response.Headers,
                    _ => GetExtensionData(response.ExtensionData, segment),
                };
            case ApiDiscriminator discriminator:
                return segment == "mapping" ? discriminator.Mapping : null;
            case ApiSchema schema:
                return GetSchemaChild(schema, segment);
            case RawJsonObject rawObject:
                return rawObject.TryGetValue(segment, out var rawValue) ? rawValue : null;
            case RawJsonArray rawArray:
                return int.TryParse(segment, out var rawIndex) && rawIndex < rawArray.Items.Count ? rawArray.Items[rawIndex] : null;
            case ApiSchemaDictionary schemaDictionary:
                return schemaDictionary.TryGetValue(segment, out var namedSchema) ? namedSchema : null;
            case ApiSchemaPropertyDictionary propertyDictionary:
                return propertyDictionary.TryGetValue(segment, out var property) ? property : null;
            case System.Collections.IDictionary dictionary:
                return dictionary.Contains(segment) ? dictionary[segment] : null;
            case System.Collections.IEnumerable enumerable:
                if (int.TryParse(segment, out var index))
                {
                    var items = enumerable.Cast<object>().ToArray();
                    return items.Length > index ? items[index] : null;
                }

                return null;
            default:
                return null;
        }
    }

    private static object? GetSchemaChild(ApiSchema schema, string segment)
    {
        var extensionData = GetExtensionData(schema.ExtensionData, segment);
        if (extensionData != null)
            return extensionData;

        if (schema is ApiParameter parameter)
        {
            switch (segment)
            {
                case "schema":
                    return parameter.Schema;
                case "x-schema":
                    return parameter.CustomSchema;
            }
        }

        return segment switch
        {
            "properties" => schema.Properties.Count > 0 ? schema.Properties : null,
            "patternProperties" => schema.PatternProperties.Count > 0 ? schema.PatternProperties : null,
            "definitions" => schema.Definitions.Count > 0 ? schema.Definitions : null,
            "items" => (object?)schema.Item ?? (schema.Items.Count > 0 ? schema.Items : null),
            "additionalProperties" => schema.AdditionalPropertiesSchema,
            "additionalItems" => schema.AdditionalItemsSchema,
            "allOf" => schema.AllOf.Count > 0 ? schema.AllOf : null,
            "anyOf" => schema.AnyOf.Count > 0 ? schema.AnyOf : null,
            "oneOf" => schema.OneOf.Count > 0 ? schema.OneOf : null,
            "not" => schema.Not,
            "x-dictionaryKey" => schema.DictionaryKey,
            "discriminator" => schema.DiscriminatorObject,
            _ => null,
        };
    }

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
                ? ResolveExternalLocal(root, location, referencePath, target)
                : (ApiSchema)ResolveReference(document, location, referencePath, target, append: true);
        }

        foreach (var child in GetChildSchemas(schema))
        {
            VisitExternalSchema(child, visited, root, location);
        }
    }

    private ApiSchema ResolveExternalLocal(ExternalRoot root, string location, string pointer, ReferenceTarget target)
    {
        var resolved = (ApiSchema)root.Document.Resolve(pointer, target, root.Resolver);
        if (!document.Definitions.Values.Contains(resolved))
        {
            var typeNameHint = pointer.Split('/', '\\').Last().Split('.').First();
            var key = typeNameGenerator.Generate(resolved, typeNameHint, document.Definitions.Keys);
            document.Definitions[key] = resolved;
        }

        return resolved;
    }

    private static IEnumerable<ApiSchema> GetChildSchemas(ApiSchema schema)
    {
        if (schema.AdditionalItemsSchema != null)
            yield return schema.AdditionalItemsSchema;
        if (schema.AdditionalPropertiesSchema != null)
            yield return schema.AdditionalPropertiesSchema;
        if (schema.Item != null)
            yield return schema.Item;
        foreach (var item in schema.Items)
            yield return item;
        foreach (var item in schema.AllOf)
            yield return item;
        foreach (var item in schema.AnyOf)
            yield return item;
        foreach (var item in schema.OneOf)
            yield return item;
        if (schema.Not != null)
            yield return schema.Not;
        if (schema.DictionaryKey != null)
            yield return schema.DictionaryKey;
        if (schema.DiscriminatorObject != null)
        {
            foreach (var mapping in schema.DiscriminatorObject.Mapping.Values)
                yield return mapping;
        }

        foreach (var property in schema.Properties.Values)
            yield return property;
        foreach (var property in schema.PatternProperties.Values)
            yield return property;
        foreach (var definition in schema.Definitions.Values)
            yield return definition;
        if (schema is ApiParameter parameter)
        {
            if (parameter.Schema != null)
                yield return parameter.Schema;
            if (parameter.CustomSchema != null)
                yield return parameter.CustomSchema;
        }
    }
}
