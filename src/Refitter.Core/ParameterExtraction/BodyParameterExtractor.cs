using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class BodyParameterExtractor
{
    public static IEnumerable<MethodParameter> Extract(
        CSharpOperationModel operationModel,
        RefitGeneratorSettings settings)
    {
        var bodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                return new MethodParameter(ParameterTypeResolver.GetParameterType(p, settings), variableName)
                {
                    Attributes = [ParameterAttributeFormatter.GetBodyAttribute(p, operationModel.Consumes, settings)],
                    Alias = ParameterAttributeFormatter.GetAlias(p.Name, variableName),
                };
            })
            .ToList();

        var binaryBodyParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                return new MethodParameter("StreamPart", variableName)
                {
                    Alias = ParameterAttributeFormatter.GetAlias(p.Name, variableName),
                };
            })
            .ToList();

        return bodyParameters
            .Concat(binaryBodyParameters)
            .ToList();
    }
}
