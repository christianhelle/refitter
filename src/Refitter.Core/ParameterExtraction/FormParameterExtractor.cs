
namespace Refitter.Core;

internal sealed class FormParameterExtractor
{
    public IEnumerable<string> Extract(
        OperationModel operationModel,
        ApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var seenFormParameterNames = new HashSet<string>(StringComparer.Ordinal);
        var formParameters = new List<string>();
        var operationFormParameters = operationModel.Parameters
            .Where(p => p.Kind == ApiParameterKind.FormData && !p.IsBinaryBodyParameter)
            .ToList();

        void AddOperationParameter(OperationParameterModel p)
        {
            var variableName = ParameterNaming.ConvertToVariableName(p.VariableName);
            if (seenFormParameterNames.Add(variableName))
            {
                formParameters.Add($"{ParameterAttributeFormatter.JoinAttributes(ParameterAttributeFormatter.GetAliasAsAttribute(p.Name, variableName))}{ParameterTypeResolver.GetParameterType(p, settings)} {variableName}");
            }
        }

        if (operation.RequestBody?.Content.TryGetValue("multipart/form-data", out var multipartContent) == true &&
            multipartContent.Schema != null)
        {
            // NSwag only creates parameters for the schema's own properties, so properties that come
            // from allOf members (e.g. a referenced schema) are added here, in schema order (#1277)
            var visitedSchemas = new HashSet<ApiSchema>();
            foreach (var property in GetProperties(multipartContent.Schema, visitedSchemas))
            {
                var operationParameter = operationFormParameters.FirstOrDefault(p => p.Name == property.Key);
                if (operationParameter != null)
                {
                    AddOperationParameter(operationParameter);
                    continue;
                }

                var variableName = ParameterNaming.ConvertToVariableName(property.Key);
                if (seenFormParameterNames.Add(variableName))
                {
                    var aliasAttribute = ParameterAttributeFormatter.GetAliasAsAttribute(property.Key, variableName);
                    var propertyType = GetPropertyType(property.Value, settings);
                    formParameters.Add($"{ParameterAttributeFormatter.JoinAttributes(aliasAttribute)}{propertyType} {variableName}");
                }
            }
        }

        foreach (var p in operationFormParameters)
        {
            AddOperationParameter(p);
        }

        return formParameters;
    }

    private static IEnumerable<KeyValuePair<string, ApiSchemaProperty>> GetProperties(
        ApiSchema schema,
        HashSet<ApiSchema> visitedSchemas)
    {
        var actualSchema = schema.ActualSchema;
        if (!visitedSchemas.Add(actualSchema))
            yield break;

        foreach (var allOfSchema in actualSchema.AllOf)
        {
            foreach (var property in GetProperties(allOfSchema, visitedSchemas))
                yield return property;
        }

        foreach (var property in actualSchema.Properties)
            yield return property;
    }

    private static string GetPropertyType(ApiSchema propertySchema, RefitGeneratorSettings settings)
    {
        if (IsBinary(propertySchema))
            return "StreamPart";

        // Arrays use IEnumerable<T>, like the multipart parameters NSwag creates
        if (propertySchema.Type == ApiObjectType.Array)
        {
            var itemType = propertySchema.Item is { } itemSchema
                ? GetPropertyType(itemSchema, settings)
                : "object";
            return $"IEnumerable<{itemType}>";
        }

        return ParameterTypeResolver.GetCSharpType(propertySchema, settings);
    }

    private static bool IsBinary(ApiSchema schema) =>
        schema.Type == ApiObjectType.String && schema.Format == "binary";
}
