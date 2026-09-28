namespace Refitter.Core;

/// <summary>
/// Finds the JSON path where each object of a document is first found when the document is written.
/// </summary>
/// <remarks>
/// The document is searched depth first in the order its members are written, and keys are not escaped,
/// like references have always been written (see THIRD-PARTY-NOTICES.md).
/// </remarks>
internal sealed class ApiJsonPathFinder
{
    private const char PathSeparator = '/';

    private readonly bool isSwagger2;
    private readonly Dictionary<object, string> paths = new();

    private ApiJsonPathFinder(ApiDocument document)
    {
        isSwagger2 = document.SchemaType == ApiSchemaType.Swagger2;
    }

    public static Dictionary<object, string> FindReferencePaths(ApiDocument document)
    {
        var finder = new ApiJsonPathFinder(document);
        finder.VisitDocument(document);
        return finder.paths;
    }

    private bool Add(object obj, string path)
    {
        if (paths.ContainsKey(obj))
            return false;

        paths[obj] = path;
        return true;
    }

    private void VisitDocument(ApiDocument document)
    {
        foreach (var pathItem in document.Paths)
            VisitPathItem(pathItem.Value, "#/paths/" + pathItem.Key);

        if (isSwagger2)
        {
            VisitSchemas(document.Definitions, "#/definitions");
            foreach (var parameter in document.Parameters)
                VisitSchema(parameter.Value, "#/parameters/" + parameter.Key);
            foreach (var response in document.Responses)
                VisitResponse(response.Value, "#/responses/" + response.Key);
        }
        else
        {
            var components = document.Components;
            VisitSchemas(components.Schemas, "#/components/schemas");
            foreach (var requestBody in components.RequestBodies)
                VisitRequestBody(requestBody.Value, "#/components/requestBodies/" + requestBody.Key);
            foreach (var response in components.Responses)
                VisitResponse(response.Value, "#/components/responses/" + response.Key);
            foreach (var parameter in components.Parameters)
                VisitSchema(parameter.Value, "#/components/parameters/" + parameter.Key);
            foreach (var header in components.Headers)
                VisitSchema(header.Value, "#/components/headers/" + header.Key);
        }
    }

    private void VisitPathItem(ApiPathItem pathItem, string path)
    {
        if (!Add(pathItem, path))
            return;

        // Only the operations of a path item are searched
        foreach (var operation in pathItem)
            VisitOperation(operation.Value, path + PathSeparator + operation.Key);
    }

    private void VisitOperation(ApiOperation operation, string path)
    {
        if (!Add(operation, path))
            return;

        // The body parameter of an OpenAPI 3 operation is its request body
        var parameters = isSwagger2
            ? operation.Parameters.ToList()
            : operation.Parameters.Where(p => p.Kind != ApiParameterKind.Body).ToList();
        for (var i = 0; i < parameters.Count; i++)
            VisitSchema(parameters[i], path + "/parameters/" + i);

        if (!isSwagger2 && operation.RequestBody != null)
            VisitRequestBody(operation.RequestBody, path + "/requestBody");

        foreach (var response in operation.Responses)
            VisitResponse(response.Value, path + "/responses/" + response.Key);
    }

    private void VisitRequestBody(ApiRequestBody requestBody, string path)
    {
        if (!Add(requestBody, path))
            return;

        foreach (var mediaType in requestBody.Content)
            VisitMediaType(mediaType.Value, path + "/content/" + mediaType.Key);
    }

    private void VisitMediaType(ApiMediaType mediaType, string path)
    {
        if (!Add(mediaType, path))
            return;

        if (mediaType.Schema != null)
            VisitSchema(mediaType.Schema, path + "/schema");
    }

    private void VisitResponse(ApiResponse response, string path)
    {
        if (!Add(response, path))
            return;

        if (isSwagger2 && response.Schema != null)
            VisitSchema(response.Schema, path + "/schema");

        foreach (var header in response.Headers)
            VisitSchema(header.Value, path + "/headers/" + header.Key);

        if (!isSwagger2)
        {
            foreach (var mediaType in response.Content)
                VisitMediaType(mediaType.Value, path + "/content/" + mediaType.Key);
        }
    }

    private void VisitSchemas<T>(IEnumerable<KeyValuePair<string, T>> schemas, string path)
        where T : ApiSchema
    {
        foreach (var schema in schemas)
            VisitSchema(schema.Value, path + PathSeparator + schema.Key);
    }

    private void VisitSchemaList(IList<ApiSchema> schemas, string path)
    {
        for (var i = 0; i < schemas.Count; i++)
            VisitSchema(schemas[i], path + PathSeparator + i);
    }

    private void VisitOptionalSchema(ApiSchema? schema, string path)
    {
        if (schema != null)
            VisitSchema(schema, path);
    }

    private void VisitSchema(ApiSchema schema, string path)
    {
        if (!Add(schema, path))
            return;

        if (!isSwagger2 && schema.DiscriminatorObject != null)
        {
            foreach (var mapping in schema.DiscriminatorObject.Mapping)
                VisitSchema(mapping.Value, path + "/discriminator/mapping/" + mapping.Key);
        }

        // The members of a parameter come before the members of its schema
        if (schema is ApiParameter parameter)
        {
            VisitOptionalSchema(parameter.Schema, path + "/schema");
            VisitOptionalSchema(parameter.CustomSchema, path + "/x-schema");
        }

        VisitOptionalSchema(schema.DictionaryKey, path + "/x-dictionaryKey");
        VisitOptionalSchema(schema.Not, path + "/not");
        VisitOptionalSchema(schema.AdditionalItemsSchema, path + "/additionalItems");
        VisitOptionalSchema(schema.AdditionalPropertiesSchema, path + "/additionalProperties");

        if (schema.Item != null)
            VisitSchema(schema.Item, path + "/items");
        else
            VisitSchemaList(schema.Items, path + "/items");

        VisitSchemas(schema.Properties, path + "/properties");
        VisitSchemas(schema.PatternProperties, path + "/patternProperties");
        VisitSchemas(schema.Definitions, path + "/definitions");
        VisitSchemaList(schema.AllOf, path + "/allOf");
        VisitSchemaList(schema.AnyOf, path + "/anyOf");
        VisitSchemaList(schema.OneOf, path + "/oneOf");
    }
}
