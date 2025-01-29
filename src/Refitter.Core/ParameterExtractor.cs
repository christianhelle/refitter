using System.Text;
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
            { parameter.IsDate: true, settings.CodeGeneratorSettings.DateFormat: not null } => $"Query(Format = \"{settings.CodeGeneratorSettings?.DateFormat}\")",
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
        typeName is "FileResponse" or "FileParameter" ? "StreamPart" : typeName;

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
                        propertiesCodeBuilder.Append(
            $$"""
                    /// <summary>
                    /// {{operationParameter.Description}}
                    /// </summary>
            """);
                        propertiesCodeBuilder.AppendLine();
                    }

                    propertiesCodeBuilder.Append(
            $$"""
                    {{attributes}}
                    {{modifier}} {{propertyType}} {{propertyName}} { get; {{setterStyle}}; }
            """);
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
}
