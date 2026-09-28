namespace Refitter.Core;

/// <summary>
/// Finds the schemas that inherit from a schema, in document order, together with the name they were found
/// under (a schema or property name). The order is the order of the derived types in the discriminator
/// attributes of the base type. See THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class DerivedSchemaFinder
{
    private readonly ApiSchema baseSchema;
    private readonly Dictionary<ApiSchema, string?> derivedSchemas = new();
    private readonly List<ApiSchema> order = new();
    private readonly HashSet<object> visited = new();
    private bool isSwagger2;

    private DerivedSchemaFinder(ApiSchema baseSchema)
    {
        this.baseSchema = baseSchema;
    }

    public static IReadOnlyList<KeyValuePair<ApiSchema, string?>> Find(ApiSchema baseSchema, ApiDocument document)
    {
        var finder = new DerivedSchemaFinder(baseSchema);
        finder.VisitDocument(document);
        return finder.order.Select(s => new KeyValuePair<ApiSchema, string?>(s, finder.derivedSchemas[s])).ToList();
    }

    private void VisitDocument(ApiDocument document)
    {
        isSwagger2 = document.SchemaType == ApiSchemaType.Swagger2;
        foreach (var path in document.Paths.ToList())
        {
            VisitPathItem(path.Value);
        }

        if (document.SchemaType == ApiSchemaType.Swagger2)
        {
            VisitSchemaDictionary(document.Definitions);
            foreach (var parameter in document.Parameters.ToList())
                VisitSchema(parameter.Value, parameter.Key);
            foreach (var response in document.Responses.ToList())
                VisitResponse(response.Value);
        }
        else
        {
            VisitSchemaDictionary(document.Components.Schemas);
            foreach (var requestBody in document.Components.RequestBodies.ToList())
                VisitRequestBody(requestBody.Value);
            foreach (var response in document.Components.Responses.ToList())
                VisitResponse(response.Value);
            foreach (var parameter in document.Components.Parameters.ToList())
                VisitSchema(parameter.Value, parameter.Key);
            foreach (var header in document.Components.Headers.ToList())
                VisitSchema(header.Value, header.Key);
        }
    }

    private void VisitPathItem(ApiPathItem pathItem)
    {
        if (!visited.Add(pathItem))
            return;

        if (pathItem.Reference != null)
            VisitPathItem(pathItem.Reference);

        foreach (var operation in pathItem.ToList())
        {
            VisitOperation(operation.Value);
        }

        foreach (var parameter in pathItem.Parameters.ToList())
        {
            VisitSchema(parameter, null);
        }
    }

    private void VisitOperation(ApiOperation operation)
    {
        foreach (var parameter in operation.Parameters.ToList())
        {
            VisitSchema(parameter, null);
        }

        if (!isSwagger2 && operation.RequestBody != null)
            VisitRequestBody(operation.RequestBody);

        foreach (var response in operation.Responses.ToList())
        {
            VisitResponse(response.Value);
        }
    }

    private void VisitRequestBody(ApiRequestBody requestBody)
    {
        if (!visited.Add(requestBody))
            return;

        if (requestBody.Reference != null)
            VisitRequestBody(requestBody.Reference);

        foreach (var schema in requestBody.Content.Select(mediaType => mediaType.Value.Schema).OfType<ApiSchema>().ToList())
            VisitSchema(schema, "schema");
    }

    private void VisitResponse(ApiResponse response)
    {
        if (!visited.Add(response))
            return;

        if (response.Reference != null)
            VisitResponse(response.Reference);

        // Swagger 2.0 responses have their schema before their headers, OpenAPI content comes after them
        if (isSwagger2 && response.Schema != null)
            VisitSchema(response.Schema, "schema");

        foreach (var header in response.Headers.ToList())
        {
            VisitSchema(header.Value, header.Key);
        }

        if (!isSwagger2)
        {
            foreach (var schema in response.Content.Select(mediaType => mediaType.Value.Schema).OfType<ApiSchema>().ToList())
                VisitSchema(schema, "schema");
        }
    }

    private void VisitSchemaDictionary(IDictionary<string, ApiSchema> schemas)
    {
        foreach (var schema in schemas.ToList())
        {
            VisitSchema(schema.Value, schema.Key);
        }
    }

    private void VisitSchema(ApiSchema schema, string? typeNameHint)
    {
        if (!visited.Add(schema))
            return;

        if (schema.Inherits(baseSchema) && baseSchema != schema && !derivedSchemas.ContainsKey(schema))
        {
            derivedSchemas.Add(schema, typeNameHint);
            order.Add(schema);
        }

        // Each collection is copied when it is reached, like the schemas themselves are visited
        VisitOptionalSchema(schema.Reference, null);
        VisitOptionalSchema(schema.AdditionalItemsSchema, null);
        VisitOptionalSchema(schema.AdditionalPropertiesSchema, null);
        VisitOptionalSchema(schema.Item, null);
        VisitUnnamedSchemas(schema.Items.ToList());
        VisitUnnamedSchemas(schema.AllOf.ToList());
        VisitUnnamedSchemas(schema.AnyOf.ToList());
        VisitUnnamedSchemas(schema.OneOf.ToList());
        VisitOptionalSchema(schema.Not, null);
        VisitOptionalSchema(schema.DictionaryKey, null);

        if (schema.DiscriminatorObject != null)
            VisitNamedSchemas(schema.DiscriminatorObject.Mapping.ToList());

        VisitNamedSchemas(schema.Properties.ToList());
        VisitUnnamedSchemas(schema.PatternProperties.Select(property => property.Value).ToList());
        VisitNamedSchemas(schema.Definitions.ToList());

        if (schema is ApiParameter parameter)
        {
            VisitOptionalSchema(parameter.Schema, "schema");
            VisitOptionalSchema(parameter.CustomSchema, "x-schema");
        }
    }

    private void VisitOptionalSchema(ApiSchema? schema, string? typeNameHint)
    {
        if (schema != null)
            VisitSchema(schema, typeNameHint);
    }

    private void VisitUnnamedSchemas(IEnumerable<ApiSchema> schemas)
    {
        foreach (var schema in schemas)
            VisitSchema(schema, null);
    }

    private void VisitNamedSchemas<T>(IEnumerable<KeyValuePair<string, T>> schemas)
        where T : ApiSchema
    {
        foreach (var schema in schemas)
            VisitSchema(schema.Value, schema.Key);
    }
}
