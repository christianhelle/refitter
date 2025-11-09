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
            .Select(p => $"{JoinAttributes(GetAliasAsAttribute(p))}{p.Type} {p.VariableName}")
            .ToList();

        var queryParameters =
            GetQueryParameters(operationModel, settings, dynamicQuerystringParameterType, out dynamicQuerystringParameters);

        var bodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p =>
                $"{JoinAttributes("Body", GetAliasAsAttribute(p))}{GetParameterType(p, settings)} {p.VariableName}")
            .ToList();

        var headerParameters = new List<string>();

        if (settings.GenerateOperationHeaders)
        {
            headerParameters = operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader)
                .Select(p =>
                    $"{JoinAttributes($"Header(\"{p.Name}\")")}{GetParameterType(p, settings)} {p.VariableName}")
                .ToList();
        }

        if (settings.GenerateAuthenticationHeader)
        {
            var document = operation.Parent.Parent;
            foreach (var securitySchemeName in operationModel.Security.SelectMany(x => x.Keys))
            {
                if (!document.SecurityDefinitions.TryGetValue(securitySchemeName, out var securityScheme))
                {
                    continue;
                }

                if (securityScheme.Type == OpenApiSecuritySchemeType.ApiKey
                    && securityScheme.In == OpenApiSecurityApiKeyLocation.Header
                    && !operationModel.Parameters.Any(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader && p.Name == securityScheme.Name))
                {
                    headerParameters.Add($"[Header(\"{securityScheme.Name}\")] string {ReplaceUnsafeCharacters(securityScheme.Name)}");
                }
            }
        }

        var formParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.FormData && !p.IsBinaryBodyParameter)
            .Select(p =>
                $"{GetParameterType(p, settings)} {p.VariableName}")
            .ToList();

        var binaryBodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var generatedAliasAsAttribute = string.IsNullOrWhiteSpace(GetAliasAsAttribute(p))
                    ? string.Empty
                    : $"[{GetAliasAsAttribute(p)}]";

                return $"{generatedAliasAsAttribute}StreamPart {p.VariableName}";
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
        var safeText = new StringBuilder(unsafeText.Length);
        foreach (var character in unsafeText)
        {
            var safeCharacter = character;
            if (char.GetUnicodeCategory(character) == UnicodeCategory.OtherPunctuation)
            {
                safeCharacter = '_';
            }

            safeText.Append(safeCharacter);
        }

        return safeText.ToString();
    }

    private static List<string> ReOrderNullableParameters(
        List<string> parameters,
        RefitGeneratorSettings settings,
        ICollection<CSharpParameterModel> parameterModels)
    {
        if (!settings.OptionalParameters || settings.ApizrSettings?.WithRequestOptions == true)
            return parameters;

        parameters = parameters.OrderBy(c => c.Contains("?")).ToList();
        for (int index = 0; index < parameters.Count; index++)
        {
            if (parameters[index].Contains("?"))
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
        var sb = new StringBuilder(value.Length * 2);
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
            _ => numericString
        };
    }

    private static bool IsNumericType(string type)
    {
        return type is "int" or "Int32" or "long" or "Int64" or "short" or "Int16"
            or "byte" or "Byte" or "decimal" or "Decimal" or "float" or "Single"
            or "double" or "Double" or "sbyte" or "SByte" or "uint" or "UInt32"
            or "ulong" or "UInt64" or "ushort" or "UInt16";
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
            : $"AliasAs(\"{parameterModel.Name}\")";

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
        var type = WellKnownNamesspaces
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
                    var attributes = $"{JoinAttributes(GetQueryAttribute(operationParameter, settings), GetAliasAsAttribute(operationParameter))}";
                    var propertyType = GetQueryParameterType(operationParameter, settings);
                    allNullable = allNullable && propertyType.EndsWith("?");
                    var propertyName = operationParameter.VariableName.CapitalizeFirstCharacter();
                    if (operationParameter.IsRequired)
                    {
                        injectedParametersCodeBuilder.Append(injectedParametersCodeBuilder.Length == 0
                            ? $$"""{{propertyType}} {{operationParameter.VariableName}}"""
                            : $$""", {{propertyType}} {{operationParameter.VariableName}}""");

                        initializedParametersCodeBuilder.AppendLine();
                        initializedParametersCodeBuilder.Append(
            $$"""
                        {{propertyName}} = {{operationParameter.VariableName}};
            """);
                    }

                    propertiesCodeBuilder.AppendLine();
                    if (settings.GenerateXmlDocCodeComments && !string.IsNullOrWhiteSpace(operationParameter.Description))
                    {
                        AppendXmlDocComment(operationParameter.Description, propertiesCodeBuilder);
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
                $"{JoinAttributes(GetQueryAttribute(p, settings), GetAliasAsAttribute(p))}{GetQueryParameterType(p, settings)} {p.VariableName}")
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
}
