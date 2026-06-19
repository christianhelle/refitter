using System.Globalization;
using System.Text;
using NJsonSchema;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;

namespace Refitter.Core;

internal static class ParameterShared
{
    public static string ReplaceUnsafeCharacters(string unsafeText)
    {
        return IdentifierUtils.ToCompilableIdentifier(unsafeText);
    }

    public static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length + 10);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\v':
                    sb.Append("\\v");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\0':
                    sb.Append("\\0");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    public static bool IsNumericType(string type)
    {
        return type is "int" or "Int32" or "long" or "Int64" or "short" or "Int16"
            or "byte" or "Byte" or "decimal" or "Decimal" or "float" or "Single"
            or "double" or "Double" or "sbyte" or "SByte" or "uint" or "UInt32"
            or "ulong" or "UInt64" or "ushort" or "UInt16";
    }

    private static string FormatDoubleLiteral(string numericString)
    {
        if (numericString.Contains('.') || numericString.Contains('e') || numericString.Contains('E'))
            return numericString;

        return numericString + ".0";
    }

    public static string FormatNumericValue(object defaultValue, string type)
    {
        var numericString = defaultValue is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : (defaultValue.ToString() ?? "default");

        return type switch
        {
            "float" or "Single" => $"{numericString}f",
            "decimal" or "Decimal" => $"{numericString}m",
            "double" or "Double" => FormatDoubleLiteral(numericString),
            "long" or "Int64" => $"{numericString}L",
            "ulong" or "UInt64" => $"{numericString}UL",
            "uint" or "UInt32" => $"{numericString}U",
            _ => numericString
        };
    }

    public static string FormatDefaultValue(object? defaultValue, string parameterType)
    {
        if (defaultValue == null)
            return "default";

        var type = parameterType.TrimEnd('?').Trim();

        return type switch
        {
            "bool" => defaultValue.ToString()?.ToLowerInvariant() ?? "default",
            "string" => $"\"{EscapeString(defaultValue.ToString() ?? string.Empty)}\"",
            _ when IsNumericType(type) => FormatNumericValue(defaultValue, type),
            _ => "default"
        };
    }

    public static string GetDefaultValueForParameter(
        string parameterString,
        ICollection<CSharpParameterModel> parameterModels)
    {
        var parts = parameterString.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "default";

        var variableName = parts[parts.Length - 1].TrimEnd(';', ',');

        var parameterModel = parameterModels.FirstOrDefault(p => p.VariableName == variableName);
        parameterModel ??= parameterModels.FirstOrDefault(p => GetVariableName(p) == variableName);
        if (parameterModel?.Schema?.Default != null && !string.IsNullOrEmpty(parameterModel.Type))
        {
            return FormatDefaultValue(parameterModel.Schema.Default, parameterModel.Type);
        }

        return "default";
    }

    public static string GetAliasAsAttribute(CSharpParameterModel parameterModel) =>
        string.Equals(parameterModel.Name, parameterModel.VariableName)
            ? string.Empty
            : $"AliasAs(\"{EscapeString(parameterModel.Name)}\")";

    public static string GetAliasAsAttribute(string originalName, string variableName) =>
        string.Equals(originalName, variableName, StringComparison.Ordinal)
            ? string.Empty
            : $"AliasAs(\"{EscapeString(originalName)}\")";

    public static string JoinAttributes(params string[] attributes)
    {
        var filteredAttributes = attributes
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();

        if (filteredAttributes.Count == 0)
            return string.Empty;

        return "[" + string.Join(", ", filteredAttributes) + "] ";
    }

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

    public static string GetBodyAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        var anyType = settings.CodeGeneratorSettings?.AnyType ?? "object";
        var parameterType = TrimImportedNamespaces(FindSupportedType(parameter.Type));

        if (parameterType.Equals(anyType, StringComparison.OrdinalIgnoreCase) ||
            parameterType.Contains("JsonElement", StringComparison.OrdinalIgnoreCase))
        {
            return "Body(BodySerializationMethod.Serialized)";
        }

        return "Body";
    }

    public static string GetQueryAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        return (parameter, settings) switch
        {
            { parameter.IsArray: true }
                => $"Query(CollectionFormat.{settings.CollectionFormat})",
            { parameter.IsDate: true, settings.UseIsoDateFormat: true }
                => "Query(Format = \"yyyy-MM-dd\")",
            { parameter.IsDate: true, settings.CodeGeneratorSettings.DateFormat: not null }
                => $"Query(Format = \"{settings.CodeGeneratorSettings?.DateFormat}\")",
            {
                parameter.IsDateOrDateTime: true, parameter.Schema.Format: "date-time",
                settings.CodeGeneratorSettings.DateTimeFormat: not null
            } => $"Query(Format = \"{settings.CodeGeneratorSettings?.DateTimeFormat}\")",
            _ => "Query",
        };
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

    public static string ConvertToVariableName(string propertyName)
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

    public static string GetVariableName(ParameterModelBase parameterModel)
    {
        return IdentifierUtils.ToCompilableIdentifier(parameterModel.VariableName);
    }

    public static void AppendXmlDocComment(string description, StringBuilder codeBuilder)
    {
        codeBuilder.Append(
"""
                /// <summary>
""");

        var lines = description.Split(
            ["\r\n", "\r", "\n"],
            StringSplitOptions.None);

        foreach (var line in lines)
        {
            codeBuilder.AppendLine();
            codeBuilder.Append(
$$"""
                /// {{line.Trim()}}
""");
        }

        codeBuilder.AppendLine();
        codeBuilder.Append(
"""
                /// </summary>
""");
        codeBuilder.AppendLine();
    }
}
