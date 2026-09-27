
namespace Refitter.Core;

internal static class RouteParameterExtractor
{
    public static IEnumerable<string> Extract(OperationModel operationModel)
    {
        var path = operationModel.Path!;

        return operationModel.Parameters
            .Where(p => p.Kind == ApiParameterKind.Path)
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
