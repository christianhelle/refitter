using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

/// <summary>
/// Builds the ordered parameter list for a generated Refit method from an OpenAPI operation.
/// Owns all parameter rules: route, query, body, header, and form extraction, optional-parameter
/// reordering, and any trailing request-options or cancellation-token argument.
/// </summary>
internal sealed class ParameterListBuilder(RefitGeneratorSettings settings)
{
    private readonly RouteParameterExtractor routeExtractor = new();
    private readonly QueryParameterExtractor queryExtractor = new();
    private readonly BodyParameterExtractor bodyExtractor = new();
    private readonly HeaderParameterExtractor headerExtractor = new();
    private readonly FormParameterExtractor formExtractor = new();

    public ParameterList Build(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType)
    {
        var parameters = new List<string>();

        parameters.AddRange(routeExtractor.Extract(operationModel, operation, settings));

        var (queryParameters, dynamicQuerystringCode) = queryExtractor.Extract(
            operationModel,
            operation,
            settings,
            dynamicQuerystringParameterType);
        parameters.AddRange(queryParameters);

        parameters.AddRange(bodyExtractor.Extract(operationModel, operation, settings));
        parameters.AddRange(headerExtractor.Extract(operationModel, operation, settings));
        parameters.AddRange(formExtractor.Extract(operationModel, operation, settings));

        parameters = OptionalParameterReorderer.Reorder(
            parameters,
            settings,
            operationModel.Parameters);

        if (settings.ApizrSettings?.WithRequestOptions == true)
            parameters.Add("[RequestOptions] IApizrRequestOptions options");
        else if (settings.UseCancellationTokens)
            parameters.Add("CancellationToken cancellationToken = default");

        return new ParameterList(parameters, dynamicQuerystringCode);
    }
}
