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

        foreach (var schema in requestBody.Content.ToList().Select(mediaType => mediaType.Value.Schema).OfType<ApiSchema>())
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
            foreach (var schema in response.Content.ToList().Select(mediaType => mediaType.Value.Schema).OfType<ApiSchema>())
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

        if (schema.Reference != null)
            VisitSchema(schema.Reference, null);

        if (schema.AdditionalItemsSchema != null)
            VisitSchema(schema.AdditionalItemsSchema, null);

        if (schema.AdditionalPropertiesSchema != null)
            VisitSchema(schema.AdditionalPropertiesSchema, null);

        if (schema.Item != null)
            VisitSchema(schema.Item, null);

        foreach (var item in schema.Items.ToList())
            VisitSchema(item, null);

        foreach (var item in schema.AllOf.ToList())
            VisitSchema(item, null);

        foreach (var item in schema.AnyOf.ToList())
            VisitSchema(item, null);

        foreach (var item in schema.OneOf.ToList())
            VisitSchema(item, null);

        if (schema.Not != null)
            VisitSchema(schema.Not, null);

        if (schema.DictionaryKey != null)
            VisitSchema(schema.DictionaryKey, null);

        if (schema.DiscriminatorObject != null && schema.DiscriminatorObject.Mapping.Count > 0)
        {
            foreach (var mapping in schema.DiscriminatorObject.Mapping.ToList())
                VisitSchema(mapping.Value, mapping.Key);
        }

        foreach (var property in schema.Properties.ToList())
            VisitSchema(property.Value, property.Key);

        foreach (var property in schema.PatternProperties.ToList())
            VisitSchema(property.Value, null);

        foreach (var definition in schema.Definitions.ToList())
            VisitSchema(definition.Value, definition.Key);

        if (schema is ApiParameter parameter)
        {
            if (parameter.Schema != null)
                VisitSchema(parameter.Schema, "schema");

            if (parameter.CustomSchema != null)
                VisitSchema(parameter.CustomSchema, "x-schema");
        }
    }
}
