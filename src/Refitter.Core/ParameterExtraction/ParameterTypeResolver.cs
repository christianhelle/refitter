
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
        OperationParameterModel parameterModel,
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

    // OpenAPI header parameters always use style: simple (comma-separated values), and Refit sends
    // header values with ToString(), so arrays are exposed as the already-joined string
    public static string GetHeaderParameterType(
        OperationParameterModel parameterModel,
        RefitGeneratorSettings settings)
    {
        var type = GetParameterType(parameterModel, settings);
        if (!parameterModel.IsArray)
            return type;

        return type.EndsWith("?") ? "string?" : "string";
    }

    public static string GetQueryParameterType(
        OperationParameterModel parameterModel,
        RefitGeneratorSettings settings)
    {
        var type = GetParameterType(parameterModel, settings);

        if (parameterModel.IsQuery &&
            parameterModel.Type.Equals("object", StringComparison.OrdinalIgnoreCase))
            type = "string";

        return type;
    }

    public static string GetCSharpType(ApiSchema propertySchema, RefitGeneratorSettings settings)
    {
        var type = propertySchema.Type switch
        {
            ApiObjectTypes.String => "string",
            ApiObjectTypes.Integer => GetIntegerTypeName(propertySchema, settings),
            ApiObjectTypes.Number => "double",
            ApiObjectTypes.Boolean => "bool",
            ApiObjectTypes.Array => GetArrayType(propertySchema, settings),
            ApiObjectTypes.Object => "object",
            _ => "object"
        };

        if (settings.OptionalParameters && propertySchema.IsNullable(ApiSchemaType.OpenApi3))
        {
            type += "?";
        }

        return type;
    }

    public static string GetIntegerTypeName(ApiSchema schema, RefitGeneratorSettings settings)
    {
        if (schema.Format == "int64")
            return "long";
        if (schema.Format == "int32")
            return "int";

        var integerType = settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32;
        return integerType == IntegerType.Int64 ? "long" : "int";
    }

    public static string GetArrayType(ApiSchema arraySchema, RefitGeneratorSettings settings)
    {
        if (arraySchema.Item != null)
        {
            var itemType = GetCSharpType(arraySchema.Item, settings);
            return $"{itemType}[]";
        }

        return "object[]";
    }
}
