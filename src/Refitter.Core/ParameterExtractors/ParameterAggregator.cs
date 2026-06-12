using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;
using Refitter.Core;
using System.Text;

namespace Refitter.Core;

internal sealed class ParameterAggregator : IParameterExtractor
{
    private readonly List<IParameterTypeExtractor> _extractors;
    private readonly RefitGeneratorSettings _settings;

    public ParameterAggregator(RefitGeneratorSettings settings)
    {
        _settings = settings;
        _extractors = new List<IParameterTypeExtractor>
        {
            new RouteParameterExtractor(),
            new QueryParameterExtractor(),
            new BodyParameterExtractor(),
            new HeaderParameterExtractor(settings),
            new FormParameterExtractor()
        };
    }

    public IEnumerable<string> ExtractParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        out string? dynamicQuerystringParameters)
    {
        var parameters = new List<string>();

        foreach (var extractor in _extractors)
        {
            var kind = GetKindForExtractor(extractor);
            if (kind == null)
                continue;

            var hasParameters = operationModel.Parameters.Any(p => p.Kind == kind);
            if (!hasParameters)
                continue;

            var extracted = extractor.Extract(operationModel, operation, settings);
            parameters.AddRange(extracted);
        }

        // Extract dynamic querystring parameters code
        var queryParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .ToList();

        dynamicQuerystringParameters = ExtractDynamicQuerystringCode(
            operationModel,
            settings,
            dynamicQuerystringParameterType,
            queryParameters);

        // Reorder optional parameters
        if (_settings.OptionalParameters || settings.OptionalParameters)
        {
            parameters = (List<string>)OptionalParameterReorderer.Reorder(
                parameters,
                _settings,
                operationModel.Parameters);
        }

        // Add request options or cancellation token
        if (_settings.ApizrSettings?.WithRequestOptions == true)
            parameters.Add("[RequestOptions] IApizrRequestOptions options");
        else if (_settings.UseCancellationTokens)
            parameters.Add("CancellationToken cancellationToken = default");

        return parameters;
    }

    private static OpenApiParameterKind? GetKindForExtractor(IParameterTypeExtractor extractor)
    {
        return extractor switch
        {
            RouteParameterExtractor => OpenApiParameterKind.Path,
            QueryParameterExtractor => OpenApiParameterKind.Query,
            BodyParameterExtractor => OpenApiParameterKind.Body,
            HeaderParameterExtractor => OpenApiParameterKind.Header,
            FormParameterExtractor => OpenApiParameterKind.FormData,
            _ => null
        };
    }

    private static string ExtractDynamicQuerystringCode(
        CSharpOperationModel operationModel,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        IList<CSharpParameterModel> queryParameters)
    {
        if (!settings.UseDynamicQuerystringParameters || queryParameters.Count < 2)
            return string.Empty;

        var csharpQueryParameters = queryParameters.ToList();
        var codeBuilder = new StringBuilder();
        var result = DynamicQuerystringParameterBuilder.Build(
            operationModel,
            csharpQueryParameters,
            settings,
            dynamicQuerystringParameterType);

        // Build the dynamic querystring class code
        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        var isRecord = settings.ImmutableRecords ||
                       settings.CodeGeneratorSettings?.GenerateNativeRecords is true;
        var classStyle = isRecord ? "record" : "class";
        var setterStyle = isRecord ? "init" : "set";

        var injectedParametersCodeBuilder = new StringBuilder();
        var initializedParametersCodeBuilder = new StringBuilder();
        var propertiesCodeBuilder = new StringBuilder();
        var allNullable = true;

        foreach (var operationParameter in csharpQueryParameters)
        {
            var propertyType = GetQueryParameterType(operationParameter, settings);
            allNullable = allNullable && propertyType.EndsWith("?");
            var variableName = GetVariableName(operationParameter);
            var attributes = $"{JoinAttributes(GetQueryAttribute(operationParameter, settings), GetAliasAsAttribute(operationParameter.Name, variableName))}";
            var propertyName = variableName.CapitalizeFirstCharacter();

            if (operationParameter.IsRequired)
            {
                injectedParametersCodeBuilder.Append(injectedParametersCodeBuilder.Length == 0
                    ? $"{propertyType} {variableName}"
                    : $", {propertyType} {variableName}");

                initializedParametersCodeBuilder.AppendLine();
                initializedParametersCodeBuilder.Append(
                    $"        this.{propertyName} = {variableName};\n");
            }

            propertiesCodeBuilder.AppendLine();
            if (settings.GenerateXmlDocCodeComments && !string.IsNullOrWhiteSpace(operationParameter.Description))
            {
                var escapedDescription = XmlDocumentationGenerator.SanitizeResponseDescription(operationParameter.Description);
                AppendXmlDocComment(escapedDescription, propertiesCodeBuilder);
            }

            propertiesCodeBuilder.Append(
                $"\n        {attributes}\n        {modifier} {propertyType} {propertyName} {{ get; {setterStyle}; }}");
            var defaultValue = operationParameter.Schema?.Default;
            if (defaultValue != null)
            {
                var formattedDefaultValue = FormatDefaultValue(defaultValue, propertyType);
                propertiesCodeBuilder.Append($" = {formattedDefaultValue};");
            }

            propertiesCodeBuilder.AppendLine();
        }

        codeBuilder.AppendLine(
            $"\n    {modifier} {classStyle} {dynamicQuerystringParameterType}");
        codeBuilder.AppendLine("    {");

        if (injectedParametersCodeBuilder.Length > 0)
        {
            codeBuilder.AppendLine(
                $"\n        {modifier} {dynamicQuerystringParameterType}({injectedParametersCodeBuilder})");
            codeBuilder.AppendLine("        {");
            codeBuilder.Append(initializedParametersCodeBuilder);
            codeBuilder.AppendLine("        }");
        }

        codeBuilder.Append(propertiesCodeBuilder);
        codeBuilder.AppendLine("    }");

        return codeBuilder.ToString();
    }

    private static string GetVariableName(ParameterModelBase parameterModel)
    {
        return IdentifierUtils.ToCompilableIdentifier(parameterModel.VariableName);
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
                => $"Query(Format = \"{settings.CodeGeneratorSettings!.DateFormat}\")",
            {
                parameter.IsDateOrDateTime: true, parameter.Schema.Format: "date-time",
                settings.CodeGeneratorSettings.DateTimeFormat: not null
            } => $"Query(Format = \"{settings.CodeGeneratorSettings!.DateTimeFormat}\")",
            _ => "Query",
        };
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

    private static string FormatNumericValue(object defaultValue, string type)
    {
        var numericString = defaultValue is IFormattable formattable
            ? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
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
        if (numericString.Contains('.') || numericString.Contains('e') || numericString.Contains('E'))
            return numericString;

        return numericString + ".0";
    }

    private static bool IsNumericType(string type)
    {
        return type is "int" or "Int32" or "long" or "Int64" or "short" or "Int16"
            or "byte" or "Byte" or "decimal" or "Decimal" or "float" or "Single"
            or "double" or "Double" or "sbyte" or "SByte" or "uint" or "UInt32"
            or "ulong" or "UInt64" or "ushort" or "UInt16";
    }

    private static void AppendXmlDocComment(string description, StringBuilder codeBuilder)
    {
        codeBuilder.Append(
            "\n        /// <summary>");

        var lines = description.Split(
            ["\r\n", "\r", "\n"],
            StringSplitOptions.None);

        foreach (var line in lines)
        {
            codeBuilder.AppendLine();
            codeBuilder.Append(
                $"        /// {line.Trim()}");
        }

        codeBuilder.AppendLine();
        codeBuilder.Append(
            "        /// </summary>");
        codeBuilder.AppendLine();
    }
}
