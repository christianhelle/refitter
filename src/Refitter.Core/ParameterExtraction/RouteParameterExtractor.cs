using System;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal sealed class RouteParameterExtractor
{
    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var path = operationModel.Path;

        return operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Path)
            .OrderBy(p => GetUrlPosition(path, p.Name))
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                return $"{ParameterAttributeFormatter.JoinAttributes(ParameterAttributeFormatter.GetAliasAsAttribute(p.Name, variableName))}{p.Type} {variableName}";
            })
            .ToList();
    }

    private static int GetUrlPosition(string path, string parameterName)
    {
        var placeholder = $"{{{parameterName}}}";
        var index = path.IndexOf(placeholder, StringComparison.Ordinal);
        return index >= 0 ? index : int.MaxValue;
    }
}
