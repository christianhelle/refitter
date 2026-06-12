using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class QueryParameterExtractor
{
    public static IEnumerable<string> ExtractSimple(
        CSharpOperationModel operationModel,
        RefitGeneratorSettings settings)
    {
        return operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .Select(p =>
            {
                var variableName = ParameterShared.GetVariableName(p);
                return $"{ParameterShared.JoinAttributes(ParameterShared.GetQueryAttribute(p, settings), ParameterShared.GetAliasAsAttribute(p.Name, variableName))}{ParameterShared.GetQueryParameterType(p, settings)} {variableName}";
            })
            .ToList();
    }
}
