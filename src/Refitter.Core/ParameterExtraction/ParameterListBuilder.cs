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

        parameters.AddRange(RouteParameterExtractor.Extract(operationModel));

        var (queryParameters, dynamicQuerystringCode) = queryExtractor.Extract(
            operationModel,
            settings,
            dynamicQuerystringParameterType);
        parameters.AddRange(queryParameters);

        parameters.AddRange(bodyExtractor.Extract(operationModel, settings));
        parameters.AddRange(headerExtractor.Extract(operationModel, operation, settings));
        parameters.AddRange(formExtractor.Extract(operationModel, operation, settings));

        parameters = OptionalParameterReorderer.Reorder(
            parameters,
            settings,
            operationModel.Parameters);

        var hasAppendedParameter = true;
        if (settings.ApizrSettings?.WithRequestOptions == true)
            parameters.Add("[RequestOptions] IApizrRequestOptions options");
        else if (settings.UseCancellationTokens)
            parameters.Add("CancellationToken cancellationToken = default");
        else
            hasAppendedParameter = false;

        return new ParameterList(
            ParameterNameDeduplicator.Deduplicate(parameters, hasAppendedParameter),
            dynamicQuerystringCode);
    }
}
