using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class BodyParameterExtractor : IParameterTypeExtractor
{
    public bool CanExtract(OpenApiParameterKind kind) =>
        kind == OpenApiParameterKind.Body;

    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var bodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = ParameterShared.GetVariableName(p);
                return $"{ParameterShared.JoinAttributes(ParameterShared.GetBodyAttribute(p, settings), ParameterShared.GetAliasAsAttribute(p.Name, variableName))}{ParameterShared.GetParameterType(p, settings)} {variableName}";
            })
            .ToList();

        var binaryBodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = ParameterShared.GetVariableName(p);
                var aliasAsAttribute = ParameterShared.GetAliasAsAttribute(p.Name, variableName);
                var generatedAliasAsAttribute = string.IsNullOrWhiteSpace(aliasAsAttribute)
                    ? string.Empty
                    : $"[{aliasAsAttribute}]";

                return $"{generatedAliasAsAttribute}StreamPart {variableName}";
            })
            .ToList();

        return bodyParameters
            .Concat(binaryBodyParameters)
            .ToList();
    }
}
