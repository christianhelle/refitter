using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;

namespace Refitter.Core;

internal static class ParameterExtractor
{
    public static IEnumerable<string> GetParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        out string? dynamicQuerystringParameters)
    {
        var routeParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Path)
            .Select(p =>
            {
                var variableName = GetVariableName(p);
                return $"{JoinAttributes(GetAliasAsAttribute(p.Name, variableName))}{p.Type} {variableName}";
            })
            .ToList();

        var queryParameters =
            GetQueryParameters(operationModel, settings, dynamicQuerystringParameterType, out dynamicQuerystringParameters);

        var bodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = GetVariableName(p);
                return $"{JoinAttributes(GetBodyAttribute(p, settings), GetAliasAsAttribute(p.Name, variableName))}{GetParameterType(p, settings)} {variableName}";
            })
            .ToList();

        var headerParameters = new List<string>();

        if (settings.GenerateOperationHeaders)
        {
            var ignoredHeaders = settings.IgnoredOperationHeaders
                .Select(h => h.Trim())
                .Where(h => !string.IsNullOrEmpty(h))
                .ToArray();

            var anyIgnoredHeaders = ignoredHeaders.Any();

            headerParameters = operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader)
                .Where(p => !anyIgnoredHeaders || !ignoredHeaders.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .Select(p =>
                {
                    var variableName = GetVariableName(p);
                    return $"{JoinAttributes($"Header(\"{p.Name}\")")}{GetParameterType(p, settings)} {variableName}";
                })
                .ToList();
        }

        if (settings.AuthenticationHeaderStyle == AuthenticationHeaderStyle.Parameter)
        {
            var document = operation.Parent.Parent;
            foreach (var securitySchemeName in operationModel.Security.SelectMany(x => x.Keys))
            {
                if ((settings.SecurityScheme != null && securitySchemeName != settings.SecurityScheme) ||
                    !document.SecurityDefinitions.TryGetValue(securitySchemeName, out var securityScheme))
                {
                    continue;
                }

                if (securityScheme.Type == OpenApiSecuritySchemeType.ApiKey
                    && securityScheme.In == OpenApiSecurityApiKeyLocation.Header
                    && !operationModel.Parameters.Any(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader && p.Name == securityScheme.Name))
                {
                    headerParameters.Add($"[Header(\"{securityScheme.Name}\")] string {ReplaceUnsafeCharacters(securityScheme.Name)}");
                }
                else if (securityScheme is { Type: OpenApiSecuritySchemeType.Http }
                    && string.Equals(securityScheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase))
                {
                    headerParameters.Add($@"[Header(""Authorization: Bearer"")] string bearerToken");
                }
            }
        }

        // Deduplicate form parameters by sanitized identifier (#1018)
        // Use ConvertToVariableName for both paths to ensure consistent deduplication
        var seenFormParameterNames = new HashSet<string>(StringComparer.Ordinal);
        var formParameters = new List<string>();

        foreach (var p in operationModel.Parameters.Where(p => p.Kind == OpenApiParameterKind.FormData && !p.IsBinaryBodyParameter))
        {
            // Use VariableName (NSwag's processed name) not Name (original OpenAPI name)
            var variableName = ConvertToVariableName(p.VariableName);
            // Only add if this sanitized identifier hasn't been seen
            if (seenFormParameterNames.Add(variableName))
            {
                formParameters.Add($"{JoinAttributes(GetAliasAsAttribute(p.Name, variableName))}{GetParameterType(p, settings)} {variableName}");
            }
        }

        // Manually extract non-binary properties from multipart/form-data in OpenAPI 3.x
        // NSwag doesn't populate these in operationModel.Parameters
        if (operation.RequestBody?.Content?.TryGetValue("multipart/form-data", out var multipartContent) == true)
        {
            var schema = multipartContent.Schema;
            if (schema?.Properties != null)
            {
                foreach (var property in schema.Properties)
                {
                    var propertySchema = property.Value;

                    // Skip binary fields (files) as they're already handled as StreamPart
                    var isBinary = (propertySchema.Type == JsonObjectType.String &&
                                   propertySchema.Format == "binary") ||
                                  (propertySchema.Type == JsonObjectType.Array &&
                                   propertySchema.Item?.Type == JsonObjectType.String &&
                                   propertySchema.Item?.Format == "binary");

                    if (!isBinary)
                    {
                        // Generate proper C# type for the property
                        var propertyType = GetCSharpType(propertySchema, settings);
                        var variableName = ConvertToVariableName(property.Key);

                        // Deduplicate by sanitized C# identifier, not original OpenAPI name (#1018)
                        // HashSet.Add returns true if item was added (first occurrence), false if already present
                        if (seenFormParameterNames.Add(variableName))
                        {
                            // First occurrence of this sanitized identifier - add the parameter
                            var aliasAttribute = GetAliasAsAttribute(property.Key, variableName);
                            var parameter = $"{JoinAttributes(aliasAttribute)}{propertyType} {variableName}";
                            formParameters.Add(parameter);
                        }
                        // else: duplicate sanitized identifier - skip this parameter
                    }
                }
            }
        }
        var binaryBodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = GetVariableName(p);
                var aliasAsAttribute = GetAliasAsAttribute(p.Name, variableName);
                var generatedAliasAsAttribute = string.IsNullOrWhiteSpace(aliasAsAttribute)
                    ? string.Empty
                    : $"[{aliasAsAttribute}]";

                return $"{generatedAliasAsAttribute}StreamPart {variableName}";
            })
            .ToList();

        var parameters = new List<string>();
        parameters.AddRange(routeParameters);
        parameters.AddRange(queryParameters);
        parameters.AddRange(bodyParameters);
        parameters.AddRange(headerParameters);
        parameters.AddRange(formParameters);
        parameters.AddRange(binaryBodyParameters);

        parameters = ReOrderNullableParameters(parameters, settings, operationModel.Parameters);

        if (settings.ApizrSettings?.WithRequestOptions == true)
            parameters.Add("[RequestOptions] IApizrRequestOptions options");
        else if (settings.UseCancellationTokens)
            parameters.Add("CancellationToken cancellationToken = default");

        return parameters;
    }

    private static string ReplaceUnsafeCharacters(
        string unsafeText)
    {
        return IdentifierUtils.ToCompilableIdentifier(unsafeText);
    }

    private static List<string> ReOrderNullableParameters(
        List<string> parameters,
        RefitGeneratorSettings settings,
        ICollection<CSharpParameterModel> parameterModels)
    {
        if (!settings.OptionalParameters || settings.ApizrSettings?.WithRequestOptions == true)
            return parameters;

        // Use regex to check for nullable type marker at the end of the type declaration
        // Matches "?" followed by whitespace and parameter name (and optional default value)
        var nullablePattern = new System.Text.RegularExpressions.Regex(@"\?\s+@?\w+(\s*=\s*[^,]+)?$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        parameters = parameters.OrderBy(c => nullablePattern.IsMatch(c)).ToList();
        for (int index = 0; index < parameters.Count; index++)
        {
            if (nullablePattern.IsMatch(parameters[index]))
            {
                var parameterString = parameters[index];
                var defaultValue = GetDefaultValueForParameter(parameterString, parameterModels);
                parameters[index] = parameterString + " = " + defaultValue;
            }
        }

        return parameters;
    }

    private static string GetDefaultValueForParameter(string parameterString, ICollection<CSharpParameterModel> parameterModels)
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

    private static string FormatDefaultValue(object? defaultValue, string parameterType)
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


    private static string EscapeString(string value)
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

    private static string FormatNumericValue(object defaultValue, string type)
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

    private static string FormatDoubleLiteral(string numericString)
    {
        // If the string already contains a decimal point or exponent, return as-is
        if (numericString.Contains('.') || numericString.Contains('e') || numericString.Contains('E'))
            return numericString;

        // Otherwise, append .0 to make it a double literal
        return numericString + ".0";
    }

    private static bool IsNumericType(string type)
    {
        return type is "int" or "Int32" or "long" or "Int64" or "short" or "Int16"
            or "byte" or "Byte" or "decimal" or "Decimal" or "float" or "Single"
            or "double" or "Double" or "sbyte" or "SByte" or "uint" or "UInt32"
            or "ulong" or "UInt64" or "ushort" or "UInt16";
    }

    private static string GetBodyAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        var anyType = settings.CodeGeneratorSettings?.AnyType ?? "object";
        var parameterType = WellKnownNamespaces.TrimImportedNamespaces(FindSupportedType(parameter.Type));

        // Check if the parameter type matches AnyType (e.g., "object" or custom type like "System.Text.Json.JsonElement")
        if (parameterType.Equals(anyType, StringComparison.OrdinalIgnoreCase) ||
            parameterType.Contains("JsonElement", StringComparison.OrdinalIgnoreCase))
        {
            return "Body(BodySerializationMethod.Serialized)";
        }

        return "Body";
    }

    private static string GetQueryAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
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

    private static string GetAliasAsAttribute(CSharpParameterModel parameterModel) =>
        string.Equals(parameterModel.Name, parameterModel.VariableName)
            ? string.Empty
            : $"AliasAs(\"{EscapeString(parameterModel.Name)}\")";

    private static string GetAliasAsAttribute(string originalName, string variableName) =>
        string.Equals(originalName, variableName, StringComparison.Ordinal)
            ? string.Empty
            : $"AliasAs(\"{EscapeString(originalName)}\")";

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

    private static string GetQueryParameterType(
        ParameterModelBase parameterModel,
        RefitGeneratorSettings settings)
    {
        var type = GetParameterType(parameterModel, settings);

        if (parameterModel.IsQuery &&
            parameterModel.Type.Equals("object", StringComparison.OrdinalIgnoreCase))
            type = "string";

        return type;
    }

    private static string FindSupportedType(string typeName)
    {
        if (typeName is "FileResponse" or "FileParameter")
            return "StreamPart";

        // Handle collections of FileParameter/FileResponse
        if (typeName.Contains("FileParameter") || typeName.Contains("FileResponse"))
        {
            return typeName
                .Replace("FileParameter", "StreamPart")
                .Replace("FileResponse", "StreamPart");
        }

        return typeName;
    }

    private static List<string> GetQueryParameters(CSharpOperationModel operationModel, RefitGeneratorSettings settings, string dynamicQuerystringParameterType, out string? dynamicQuerystringParameters)
    {
        List<string>? parameters = null;
        var dynamicQuerystringParametersCodeBuilder = new StringBuilder();

        if (settings.UseDynamicQuerystringParameters)
        {
            var operationParameters = operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Query)
                .ToList();

            if (operationParameters.Count >= 2)
            {
                var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
                var isRecord = settings.ImmutableRecords ||
                               settings.CodeGeneratorSettings?.GenerateNativeRecords is true;
                var classStyle = isRecord
                    ? "record"
                    : "class";
                var setterStyle = isRecord
                    ? "init"
                    : "set";

                var injectedParametersCodeBuilder = new StringBuilder();
                var initializedParametersCodeBuilder = new StringBuilder();
                var propertiesCodeBuilder = new StringBuilder();
                var allNullable = true;
                foreach (var operationParameter in operationParameters)
                {
                    var propertyType = GetQueryParameterType(operationParameter, settings);
                    allNullable = allNullable && propertyType.EndsWith("?");
                    var variableName = GetVariableName(operationParameter);
                    var attributes = $"{JoinAttributes(GetQueryAttribute(operationParameter, settings), GetAliasAsAttribute(operationParameter.Name, variableName))}";
                    var propertyName = variableName.CapitalizeFirstCharacter();
                    if (operationParameter.IsRequired)
                    {
                        injectedParametersCodeBuilder.Append(injectedParametersCodeBuilder.Length == 0
                            ? $$"""{{propertyType}} {{variableName}}"""
                            : $$""", {{propertyType}} {{variableName}}""");

                        initializedParametersCodeBuilder.AppendLine();
                        initializedParametersCodeBuilder.Append(
            $$"""
                        this.{{propertyName}} = {{variableName}};
            """);
                    }

                    propertiesCodeBuilder.AppendLine();
                    if (settings.GenerateXmlDocCodeComments && !string.IsNullOrWhiteSpace(operationParameter.Description))
                    {
                        var escapedDescription = XmlDocumentationGenerator.SanitizeResponseDescription(operationParameter.Description);
                        AppendXmlDocComment(escapedDescription, propertiesCodeBuilder);
                    }

                    propertiesCodeBuilder.Append(
            $$"""
                    {{attributes}}
                    {{modifier}} {{propertyType}} {{propertyName}} { get; {{setterStyle}}; }
            """);
                    var defaultValue = operationParameter.Schema.Default;
                    if (defaultValue != null)
                    {
                        var formattedDefaultValue = FormatDefaultValue(defaultValue, propertyType);
                        propertiesCodeBuilder.Append($" = {formattedDefaultValue};");
                    }
                    propertiesCodeBuilder.AppendLine();
                    operationModel.Parameters.Remove(operationParameter);
                }

                dynamicQuerystringParametersCodeBuilder.AppendLine(
            $$"""
                {{modifier}} {{classStyle}} {{dynamicQuerystringParameterType}}
                {
            """);

                if (injectedParametersCodeBuilder.Length > 0)
                {
                    dynamicQuerystringParametersCodeBuilder.AppendLine(
            $$"""
                    {{modifier}} {{dynamicQuerystringParameterType}}({{injectedParametersCodeBuilder}})
                    {
                        {{initializedParametersCodeBuilder}}
                    }
            """);
                }

                dynamicQuerystringParametersCodeBuilder.AppendLine(
            $$"""
                    {{propertiesCodeBuilder}}
                }
            """);

                var dynamicQuerystringParameter = $"[Query] {dynamicQuerystringParameterType}";
                if (allNullable)
                    dynamicQuerystringParameter += "?";
                dynamicQuerystringParameter += " queryParams";
                parameters = [dynamicQuerystringParameter];
            }
        }

        dynamicQuerystringParameters = dynamicQuerystringParametersCodeBuilder.ToString();

        parameters ??= operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .Select(p =>
            {
                var variableName = GetVariableName(p);
                return $"{JoinAttributes(GetQueryAttribute(p, settings), GetAliasAsAttribute(p.Name, variableName))}{GetQueryParameterType(p, settings)} {variableName}";
            })
            .ToList();

        return parameters;
    }

    private static void AppendXmlDocComment(string description, StringBuilder codeBuilder)
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

        // Add nullable modifier if needed
        if (settings.OptionalParameters && propertySchema.IsNullable(SchemaType.OpenApi3))
        {
            type += "?";
        }

        return type;
    }

    private static string GetIntegerTypeName(JsonSchema schema, RefitGeneratorSettings settings)
    {
        // Check the format first
        if (schema.Format == "int64")
            return "long";
        if (schema.Format == "int32")
            return "int";

        // Fall back to settings
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

    private static string ConvertToVariableName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return "value";

        // Use ToCompilableIdentifier to handle reserved keywords and invalid characters
        var identifier = IdentifierUtils.ToCompilableIdentifier(propertyName);

        // Convert first character to lowercase for camelCase, if it's a letter
        if (identifier.Length > 0 && char.IsUpper(identifier[0]))
        {
            return char.ToLowerInvariant(identifier[0]) + identifier.Substring(1);
        }

        return identifier;
    }

    private static string GetVariableName(ParameterModelBase parameterModel)
    {
        return IdentifierUtils.ToCompilableIdentifier(parameterModel.VariableName);
    }
}
