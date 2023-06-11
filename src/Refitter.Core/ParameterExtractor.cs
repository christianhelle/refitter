using System;
using System.Collections.Generic;
using System.Linq;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

public static class ParameterExtractor
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
            .Select(p => $"{JoinAttributes("Query(CollectionFormat.Multi)", GetAliasAsAttribute(p))}{GetBodyParameterType(p)} {p.VariableName}")
            .ToList();

        var bodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p => $"{JoinAttributes("Body", GetAliasAsAttribute(p))}{GetBodyParameterType(p)} {p.VariableName}")
            .ToList();

        var headerParameters = new List<string>();

        if (settings.GenerateOperationHeaders)
        {
            headerParameters = operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader)
                .Select(p => $"{JoinAttributes($"Header(\"{p.Name}\")")}{GetBodyParameterType(p)} {p.VariableName}")
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

        if (settings.UseCancellationTokens)
            parameters.Add("CancellationToken cancellationToken = default");

        return parameters;
    }

    private static string GetAliasAsAttribute(CSharpParameterModel parameterModel) =>
        string.Equals(parameterModel.Name, parameterModel.VariableName, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $"AliasAs(\"{parameterModel.Name}\")";

    private static string JoinAttributes(params string[] attributes)
    {
        var filteredAttributes = attributes.Where(a => !string.IsNullOrWhiteSpace(a));

        if (!filteredAttributes.Any())
            return string.Empty;

        return "[" + string.Join(", ", filteredAttributes) + "] ";
    }

    private static string GetBodyParameterType(CSharpParameterModel parameterModel) =>
        WellKnownNamesspaces.TrimImportedNamespaces(FindSupportedType(parameterModel.Type));

    private static string FindSupportedType(string typeName) =>
        typeName == "FileResponse" ? "StreamPart" : typeName;
}