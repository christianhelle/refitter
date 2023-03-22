using System;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

public static class ParameterExtractor
{
    public static IEnumerable<string> GetParameters(CustomCSharpClientGenerator generator, OpenApiOperation operation)
    {
        var operationModel = generator.CreateOperationModel(operation);

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

        var multipartFormParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter)
            .Select(p => $"{JoinAttributes("Body(BodySerializationMethod.UrlEncoded)", GetAliasAsAttribute(p))}Dictionary<string, object> {p.VariableName}")
            .ToList();

        var parameters = new List<string>();
        parameters.AddRange(routeParameters);
        parameters.AddRange(queryParameters);
        parameters.AddRange(bodyParameters);
        parameters.AddRange(multipartFormParameters);
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