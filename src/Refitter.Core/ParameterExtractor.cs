using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;

namespace Refitter.Core;

internal static class ParameterExtractor
{
    public static IEnumerable<string> GetParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var routeParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Path)
            .Select(p => $"{JoinAttributes(GetAliasAsAttribute(p))}{p.Type} {p.VariableName}")
            .ToList();

        var queryParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .Select(p => $"{JoinAttributes(GetQueryAttribute(p, settings), GetAliasAsAttribute(p))}{GetQueryParameterType(p, settings)} {p.VariableName}")
            .ToList();

        var bodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p => $"{JoinAttributes("Body", GetAliasAsAttribute(p))}{GetParameterType(p, settings)} {p.VariableName}")
            .ToList();

        var headerParameters = new List<string>();

        if (settings.GenerateOperationHeaders)
        {
            headerParameters = operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader)
                .Select(p => $"{JoinAttributes($"Header(\"{p.Name}\")")}{GetParameterType(p, settings)} {p.VariableName}")
                .ToList();
        }

        var binaryBodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter || p.IsFile)
            .Select(p => $"{GetAliasAsAttribute(p)}StreamPart {p.VariableName}")
            .ToList();

        var parameters = new List<string>();
        parameters.AddRange(routeParameters);
        parameters.AddRange(queryParameters);
        parameters.AddRange(bodyParameters);
        parameters.AddRange(headerParameters);
        parameters.AddRange(binaryBodyParameters);

        parameters = ReOrderNullableParameters(parameters, settings);

        if (settings.UseCancellationTokens)
            parameters.Add("CancellationToken cancellationToken = default");

        return parameters;
    }

    private static List<string> ReOrderNullableParameters(
        List<string> parameters,
        RefitGeneratorSettings settings)
    {
        if (!settings.OptionalParameters)
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
}