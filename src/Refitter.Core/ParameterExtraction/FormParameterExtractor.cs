using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class FormParameterExtractor
{
    public static (IReadOnlyList<string> Parameters, string? FormObjectCode) Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string formObjectParameterType)
    {
        var formFields = GetFormFields(operationModel, operation, settings);

        var nonFileFields = formFields.Where(f => !f.IsFile).ToList();
        if (!settings.UseMultipartFormObject ||
            !operationModel.Consumes.Contains("multipart/form-data") ||
            nonFileFields.Count == 0)
        {
            return (formFields.Select(f => f.ToParameter()).ToList(), null);
        }

        var formObjectCode = FormObjectParameterBuilder.Build(nonFileFields, formObjectParameterType, settings);
        var parameters = new List<string> { $"[FormObject] {formObjectParameterType} formData" };
        parameters.AddRange(formFields.Where(f => f.IsFile).Select(f => f.ToParameter()));
        return (parameters, formObjectCode);
    }

    private static List<FormField> GetFormFields(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var seenFormParameterNames = new HashSet<string>(StringComparer.Ordinal);
        var formFields = new List<FormField>();
        var operationFormParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.FormData && !p.IsBinaryBodyParameter)
            .ToList();

        void AddOperationParameter(CSharpParameterModel p, bool isRequired)
        {
            var variableName = ParameterNaming.ConvertToVariableName(p.VariableName);
            if (seenFormParameterNames.Add(variableName))
            {
                formFields.Add(new FormField(
                    p.Name,
                    variableName,
                    ParameterTypeResolver.GetParameterType(p, settings),
                    isRequired,
                    p.Description));
            }
        }

        if (operation.RequestBody?.Content.TryGetValue("multipart/form-data", out var multipartContent) == true &&
            multipartContent.Schema != null)
        {
            // NSwag only creates parameters for the schema's own properties, so properties that come
            // from allOf members (e.g. a referenced schema) are added here, in schema order (#1277)
            var visitedSchemas = new HashSet<JsonSchema>();
            foreach (var property in GetProperties(multipartContent.Schema, visitedSchemas))
            {
                var operationParameter = operationFormParameters.FirstOrDefault(p => p.Name == property.Key);
                if (operationParameter != null)
                {
                    // NSwag does not carry the body schema's required list over to form parameters
                    AddOperationParameter(operationParameter, property.Value.IsRequired);
                    continue;
                }

                var variableName = ParameterNaming.ConvertToVariableName(property.Key);
                if (seenFormParameterNames.Add(variableName))
                {
                    formFields.Add(new FormField(
                        property.Key,
                        variableName,
                        GetPropertyType(property.Value, settings),
                        property.Value.IsRequired,
                        property.Value.Description));
                }
            }
        }

        foreach (var p in operationFormParameters)
        {
            AddOperationParameter(p, p.IsRequired);
        }

        return formFields;
    }

    private static IEnumerable<KeyValuePair<string, JsonSchemaProperty>> GetProperties(
        JsonSchema schema,
        HashSet<JsonSchema> visitedSchemas)
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

    private static string GetPropertyType(JsonSchema propertySchema, RefitGeneratorSettings settings)
    {
        if (IsBinary(propertySchema))
            return "StreamPart";

        // Arrays use IEnumerable<T>, like the multipart parameters NSwag creates
        if (propertySchema.Type == JsonObjectType.Array)
        {
            var itemType = propertySchema.Item is { } itemSchema
                ? GetPropertyType(itemSchema, settings)
                : "object";
            return $"IEnumerable<{itemType}>";
        }

        return ParameterTypeResolver.GetCSharpType(propertySchema, settings);
    }

    private static bool IsBinary(JsonSchema schema) =>
        schema.Type == JsonObjectType.String && schema.Format == "binary";
}

/// <summary>
/// A single field of a form request body, rendered either as its own method parameter or as a
/// property of a <c>[FormObject]</c> wrapper type.
/// </summary>
#pragma warning disable S1186 // Positional record constructor is compiler-generated
internal sealed record FormField(
#pragma warning restore S1186
    string Name,
    string VariableName,
    string Type,
    bool IsRequired,
    string? Description)
{
#pragma warning disable S2325 // Uses the positional record properties, so it cannot be static
    public bool IsFile => Type.Contains("StreamPart");

    public string ToParameter() =>
        $"{ParameterAttributeFormatter.JoinAttributes(ParameterAttributeFormatter.GetAliasAsAttribute(Name, VariableName))}{Type} {VariableName}";
#pragma warning restore S2325
}
