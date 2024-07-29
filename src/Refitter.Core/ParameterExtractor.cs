using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;

using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;

using YamlDotNet.Serialization;

namespace Refitter.Core;

internal static class ParameterExtractor
{
    public static IEnumerable<string> GetParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string operationName, 
        out string? dynamicQuerystringParameters)
    {
        var routeParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Path)
            .Select(p => $"{JoinAttributes(GetAliasAsAttribute(p))}{p.Type} {p.VariableName}")
            .ToList();

        var queryParameters =
            GetQueryParameters(operationModel, settings, operationName, out dynamicQuerystringParameters);

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

        var binaryBodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter || p.IsFile)
            .Select(p =>
            {
                var generatedAliasAsAttribute = string.IsNullOrWhiteSpace(GetAliasAsAttribute(p))
                    ? string.Empty
                    : $"[{GetAliasAsAttribute(p)}]";
                
                return $"{generatedAliasAsAttribute} StreamPart {p.VariableName}";
            })
            .ToList();

        var parameters = new List<string>();
        parameters.AddRange(routeParameters);
        parameters.AddRange(queryParameters);
        parameters.AddRange(bodyParameters);
        parameters.AddRange(headerParameters);
        parameters.AddRange(binaryBodyParameters);

        parameters = ReOrderNullableParameters(parameters, settings);

        if (settings.ApizrSettings?.WithRequestOptions == true)
            parameters.Add("[RequestOptions] IApizrRequestOptions options");
        else if (settings.UseCancellationTokens)
            parameters.Add("CancellationToken cancellationToken = default");

        return parameters;
    }

    private static List<string> ReOrderNullableParameters(
        List<string> parameters,
        RefitGeneratorSettings settings)
    {
        if (!settings.OptionalParameters || settings.ApizrSettings?.WithRequestOptions == true)
            return parameters;

        parameters = parameters.OrderBy(c => c.Contains("?")).ToList();
        for (int index = 0; index < parameters.Count; index++)
        {
            if (parameters[index].Contains("?"))
                parameters[index] += " = default";
        }

        return parameters;
    }

    private static string GetQueryAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        return (parameter, settings) switch
        {
            { parameter.IsArray: true } => "Query(CollectionFormat.Multi)",
            { parameter.IsDate: true, settings.UseIsoDateFormat: true } => "Query(Format = \"yyyy-MM-dd\")",
            _ => "Query",
        };
    }

    private static string GetAliasAsAttribute(CSharpParameterModel parameterModel) =>
        string.Equals(parameterModel.Name, parameterModel.VariableName)
            ? string.Empty
            : $"AliasAs(\"{parameterModel.Name}\")";

    private static string JoinAttributes(params string[] attributes)
    {
        var filteredAttributes = attributes.Where(a => !string.IsNullOrWhiteSpace(a));

        if (!filteredAttributes.Any())
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

    private static string FindSupportedType(string typeName) =>
        typeName == "FileResponse" ? "StreamPart" : typeName;

    private static List<string> GetQueryParameters(CSharpOperationModel operationModel, RefitGeneratorSettings settings, string operationName, out string? dynamicQuerystringParameters)
    {
        List<string>? parameters = null;
        var dynamicQuerystringParametersCodeBuilder = new StringBuilder();

        if (settings.DynamicQuerystringParametersThreshold >= 2)
        {
            var operationParameters = operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Query)
                .ToList();

            if (operationParameters.Count >= settings.DynamicQuerystringParametersThreshold)
            {
                var dynamicQuerystringParameterType = $"{operationName}QueryParams";

                var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
                var isRecord = settings.ImmutableRecords ||
                               settings.CodeGeneratorSettings?.GenerateNativeRecords is true;
                var classStyle = isRecord
                    ? "record"
                    : "class";
                var setterStyle = isRecord
                    ? "init"
                    : "set";

                var propertiesCodeBuilder = new StringBuilder();
                var allNullable = true;
                foreach (var operationParameter in operationParameters)
                {
                    var attributes = $"{JoinAttributes(GetQueryAttribute(operationParameter, settings), GetAliasAsAttribute(operationParameter))}";
                    var propertyType = GetQueryParameterType(operationParameter, settings);
                    allNullable = allNullable && propertyType.EndsWith("?");
                    var propertyName = operationParameter.VariableName.CapitalizeFirstCharacter();
                    propertiesCodeBuilder.AppendLine();
                    propertiesCodeBuilder.Append(
            $$"""
                    /// <summary>
                    /// {{operationParameter.Description ?? propertyName}}
                    /// </summary>
                    {{attributes}}
                    {{modifier}} {{propertyType}} {{propertyName}} { get; {{setterStyle}}; }
            """);
                    propertiesCodeBuilder.AppendLine();
                    operationModel.Parameters.Remove(operationParameter);
                }

                dynamicQuerystringParametersCodeBuilder.AppendLine();
                dynamicQuerystringParametersCodeBuilder.Append(
            $$"""               
                {{modifier}} {{classStyle}} {{dynamicQuerystringParameterType}}
                {
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
}