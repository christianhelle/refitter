using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;
using Refitter.Core;
using System.Text;

namespace Refitter.Core;

internal sealed class FormParameterExtractor : IParameterTypeExtractor
{
    public bool CanExtract(OpenApiParameterKind kind) => kind == OpenApiParameterKind.FormData;

    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var seenFormParameterNames = new HashSet<string>(StringComparer.Ordinal);
        var formParameters = new List<string>();

        foreach (var p in operationModel.Parameters.Where(p => p.Kind == OpenApiParameterKind.FormData && !p.IsBinaryBodyParameter))
        {
            var variableName = ConvertToVariableName(p.VariableName);
            if (seenFormParameterNames.Add(variableName))
            {
                formParameters.Add($"{JoinAttributes(GetAliasAsAttribute(p.Name, variableName))}{GetParameterType(p, settings)} {variableName}");
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
                        var propertyType = GetCSharpType(propertySchema, settings);
                        var variableName = ConvertToVariableName(property.Key);

                        if (seenFormParameterNames.Add(variableName))
                        {
                            var aliasAttribute = GetAliasAsAttribute(property.Key, variableName);
                            var parameter = $"{JoinAttributes(aliasAttribute)}{propertyType} {variableName}";
                            formParameters.Add(parameter);
                        }
                    }
                }
            }
        }

        return formParameters;
    }

    private static string ConvertToVariableName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return "value";

        var identifier = IdentifierUtils.ToCompilableIdentifier(propertyName);

        if (identifier.Length > 0 && char.IsUpper(identifier[0]))
        {
            return char.ToLowerInvariant(identifier[0]) + identifier.Substring(1);
        }

        return identifier;
    }

    private static string GetAliasAsAttribute(string originalName, string variableName)
    {
        return string.Equals(originalName, variableName, StringComparison.Ordinal)
            ? string.Empty
            : $"AliasAs(\"{EscapeString(originalName)}\")";
    }

    private static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length + 10);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\f': sb.Append("\\f"); break;
                case '\v': sb.Append("\\v"); break;
                case '\b': sb.Append("\\b"); break;
                case '\0': sb.Append("\\0"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static string JoinAttributes(params string[] attributes)
    {
        var filteredAttributes = attributes
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();

        if (filteredAttributes.Count == 0)
            return string.Empty;

        return "[" + string.Join(", ", filteredAttributes) + "] ";
    }

    private static string GetParameterType(
        ParameterModelBase parameterModel,
        RefitGeneratorSettings settings)
    {
        var type = WellKnownNamespaces
            .TrimImportedNamespaces(
                FindSupportedType(
                    parameterModel.Type));

        if (settings.OptionalParameters &&
            !type.EndsWith("?") &&
            (parameterModel.IsNullable || parameterModel.IsOptional || !parameterModel.IsRequired))
            type += "?";

        return type;
    }

    private static string GetCSharpType(JsonSchema propertySchema, RefitGeneratorSettings settings)
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

    private static string GetIntegerTypeName(JsonSchema schema, RefitGeneratorSettings settings)
    {
        if (schema.Format == "int64")
            return "long";
        if (schema.Format == "int32")
            return "int";

        var integerType = settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32;
        return integerType == IntegerType.Int64 ? "long" : "int";
    }

    private static string GetArrayType(JsonSchema arraySchema, RefitGeneratorSettings settings)
    {
        if (arraySchema.Item != null)
        {
            var itemType = GetCSharpType(arraySchema.Item, settings);
            return $"{itemType}[]";
        }
        return "object[]";
    }

    private static string FindSupportedType(string typeName)
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
}
