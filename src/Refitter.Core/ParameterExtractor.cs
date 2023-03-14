using System.Collections.Generic;
using System.Linq;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

public static class ParameterExtractor
{
    public static IEnumerable<string> GetParameters(CSharpClientGenerator generator, OpenApiOperation operation)
    {
        var routeParameters = operation.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Path)
            .Select(p => $"{generator.GetTypeName(p.ActualTypeSchema, true, null)} {p.Name}")
            .ToList();
        
        var queryParameters = operation.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .Select(p => $"[Query]{generator.GetTypeName(p.ActualTypeSchema, true, null)} {p.Name}")
            .ToList();

        var bodyParameters = operation.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body)
            .Select(p => $"[Body]{GetBodyParameterType(generator, p)} {p.Name}")
            .ToList();

        var parameters = new List<string>();
        parameters.AddRange(routeParameters);
        parameters.AddRange(queryParameters);
        parameters.AddRange(bodyParameters);
        return parameters;
    }

    private static string GetBodyParameterType(IClientGenerator generator, JsonSchema schema) =>
        WellKnownNamesspaces.TrimImportedNamespaces(
            FindSupportedType(
                generator.GetTypeName(
                    schema.ActualTypeSchema,
                    true,
                    null)));

    private static string FindSupportedType(string typeName) =>
        typeName == "FileResponse" ? "StreamPart" : typeName;
}