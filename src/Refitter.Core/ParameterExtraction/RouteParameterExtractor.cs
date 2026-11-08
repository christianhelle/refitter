using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class RouteParameterExtractor
{
    public static IEnumerable<MethodParameter> Extract(CSharpOperationModel operationModel)
    {
        var path = operationModel.Path;

        return operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Path)
            .OrderBy(p => GetUrlPosition(path, p.Name))
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                return new MethodParameter(p.Type, variableName)
                {
                    Alias = ParameterAttributeFormatter.GetAlias(p.Name, variableName),
                };
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
