using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal sealed class FormParameterExtractor
{
    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var seenFormParameterNames = new HashSet<string>(StringComparer.Ordinal);
        var formParameters = new List<string>();

        foreach (var p in operationModel.Parameters.Where(p => p.Kind == OpenApiParameterKind.FormData && !p.IsBinaryBodyParameter))
        {
            var variableName = ParameterNaming.ConvertToVariableName(p.VariableName);
            if (seenFormParameterNames.Add(variableName))
            {
                formParameters.Add($"{ParameterAttributeFormatter.JoinAttributes(ParameterAttributeFormatter.GetAliasAsAttribute(p.Name, variableName))}{ParameterTypeResolver.GetParameterType(p, settings)} {variableName}");
            }
        }

        if (operation.RequestBody?.Content?.TryGetValue("multipart/form-data", out var multipartContent) == true)
        {
            var schema = multipartContent.Schema;
            if (schema?.Properties != null)
            {
                foreach (var property in schema.Properties)
                {
                    var propertySchema = property.Value;

                    var isBinary = (propertySchema.Type == JsonObjectType.String &&
                                   propertySchema.Format == "binary") ||
                                  (propertySchema.Type == JsonObjectType.Array &&
                                   propertySchema.Item?.Type == JsonObjectType.String &&
                                   propertySchema.Item?.Format == "binary");

                    if (!isBinary)
                    {
                        var propertyType = ParameterTypeResolver.GetCSharpType(propertySchema, settings);
                        var variableName = ParameterNaming.ConvertToVariableName(property.Key);

                        if (seenFormParameterNames.Add(variableName))
                        {
                            var aliasAttribute = ParameterAttributeFormatter.GetAliasAsAttribute(property.Key, variableName);
                            var parameter = $"{ParameterAttributeFormatter.JoinAttributes(aliasAttribute)}{propertyType} {variableName}";
                            formParameters.Add(parameter);
                        }
                    }
                }
            }
        }

        return formParameters;
    }
}
