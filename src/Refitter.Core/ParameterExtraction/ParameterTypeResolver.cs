using NJsonSchema;
using NSwag.CodeGeneration.Models;

namespace Refitter.Core;

/// <summary>
/// Resolves the C# type rendered for a generated parameter from its OpenAPI schema.
/// </summary>
internal static class ParameterTypeResolver
{
    public static string FindSupportedType(string typeName)
    {
        if (typeName is "FileResponse" or "FileParameter")
            return "StreamPart";

        if (typeName.Contains("FileParameter") || typeName.Contains("FileResponse"))
        {
            return typeName
                .Replace("FileParameter", "StreamPart")
                .Replace("FileResponse", "StreamPart");
        }

        return typeName;
    }

    private static string TrimImportedNamespaces(string returnTypeParameter) =>
        returnTypeParameter.StartsWith("System.Collections.Generic.", StringComparison.OrdinalIgnoreCase)
            ? returnTypeParameter.Replace("System.Collections.Generic.", string.Empty)
            : returnTypeParameter;

    public static string ResolveType(string typeName) =>
        TrimImportedNamespaces(FindSupportedType(typeName));

    public static string GetParameterType(
        ParameterModelBase parameterModel,
        RefitGeneratorSettings settings)
    {
        var type = TrimImportedNamespaces(
                FindSupportedType(
                    parameterModel.Type));

        if (settings.OptionalParameters &&
            !type.EndsWith("?") &&
            (parameterModel.IsNullable || parameterModel.IsOptional || !parameterModel.IsRequired))
            type += "?";

        return type;
    }

    public static string GetQueryParameterType(
        ParameterModelBase parameterModel,
        RefitGeneratorSettings settings)
    {
        var type = GetParameterType(parameterModel, settings);

        if (parameterModel.IsQuery &&
            parameterModel.Type.Equals("object", StringComparison.OrdinalIgnoreCase))
            type = "string";

        return type;
    }

    public static string GetCSharpType(JsonSchema propertySchema, RefitGeneratorSettings settings)
    {
        var type = propertySchema.Type switch
        {
            JsonObjectType.String => "string",
            JsonObjectType.Integer => GetIntegerTypeName(propertySchema, settings),
            JsonObjectType.Number => "double",
            JsonObjectType.Boolean => "bool",
            JsonObjectType.Array => GetArrayType(propertySchema, settings),
            JsonObjectType.Object => "object",
            _ => "object"
        };

        if (settings.OptionalParameters && propertySchema.IsNullable(SchemaType.OpenApi3))
        {
            type += "?";
        }

        return type;
    }

    public static string GetIntegerTypeName(JsonSchema schema, RefitGeneratorSettings settings)
    {
        if (schema.Format == "int64")
            return "long";
        if (schema.Format == "int32")
            return "int";

        var integerType = settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32;
        return integerType == IntegerType.Int64 ? "long" : "int";
    }

    public static string GetArrayType(JsonSchema arraySchema, RefitGeneratorSettings settings)
    {
        if (arraySchema.Item != null)
        {
            var itemType = GetCSharpType(arraySchema.Item, settings);
            return $"{itemType}[]";
        }

        return "object[]";
    }
}
