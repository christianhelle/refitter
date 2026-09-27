
namespace Refitter.Core;

internal sealed class BodyParameterExtractor
{
    public IEnumerable<string> Extract(
        OperationModel operationModel,
        RefitGeneratorSettings settings)
    {
        var bodyParameters = operationModel.Parameters
            .Where(p => p.Kind == ApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                return $"{ParameterAttributeFormatter.JoinAttributes(ParameterAttributeFormatter.GetBodyAttribute(p, operationModel.Consumes, settings), ParameterAttributeFormatter.GetAliasAsAttribute(p.Name, variableName))}{ParameterTypeResolver.GetParameterType(p, settings)} {variableName}";
            })
            .ToList();

        var binaryBodyParameters = operationModel.Parameters
            .Where(p => p.Kind == ApiParameterKind.Body && p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                var aliasAsAttribute = ParameterAttributeFormatter.GetAliasAsAttribute(p.Name, variableName);
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
