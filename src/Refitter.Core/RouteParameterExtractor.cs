using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class RouteParameterExtractor : IParameterTypeExtractor
{
    public bool CanExtract(OpenApiParameterKind kind) =>
        kind == OpenApiParameterKind.Path;

    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        return operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Path)
            .Select(p =>
            {
                var variableName = ParameterShared.GetVariableName(p);
                return $"{ParameterShared.JoinAttributes(ParameterShared.GetAliasAsAttribute(p.Name, variableName))}{p.Type} {variableName}";
            })
            .ToList();
    }
}
