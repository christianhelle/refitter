using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class ParameterAggregator(
    IReadOnlyList<IParameterTypeExtractor> extractors) : IParameterExtractor
{
    public ParameterAggregator()
        : this(GetDefaultExtractors())
    {
    }

    public IEnumerable<string> ExtractParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        out string? dynamicQuerystringParameters)
    {
        var allParameters = new List<string>();

        // Route parameters
        allParameters.AddRange(GetExtractor<RouteParameterExtractor>().Extract(operationModel, operation, settings));

        // Query parameters (with dynamic querystring support)
        var queryExtractor = GetExtractor<QueryParameterExtractor>();
        queryExtractor.DynamicQuerystringParameterType = dynamicQuerystringParameterType;
        allParameters.AddRange(queryExtractor.Extract(operationModel, operation, settings));
        dynamicQuerystringParameters = queryExtractor.DynamicQuerystringCode;

        // Body parameters (including binary body)
        allParameters.AddRange(GetExtractor<BodyParameterExtractor>().Extract(operationModel, operation, settings));

        // Header parameters
        allParameters.AddRange(GetExtractor<HeaderParameterExtractor>().Extract(operationModel, operation, settings));

        // Form parameters
        allParameters.AddRange(GetExtractor<FormParameterExtractor>().Extract(operationModel, operation, settings));

        allParameters = OptionalParameterReorderer.Reorder(
            allParameters,
            settings,
            operationModel.Parameters);

        if (settings.ApizrSettings?.WithRequestOptions == true)
            allParameters.Add("[RequestOptions] IApizrRequestOptions options");
        else if (settings.UseCancellationTokens)
            allParameters.Add("CancellationToken cancellationToken = default");

        return allParameters;
    }

    private T GetExtractor<T>() where T : IParameterTypeExtractor
    {
        return extractors.OfType<T>().First();
    }

    private static IReadOnlyList<IParameterTypeExtractor> GetDefaultExtractors()
    {
        return new IParameterTypeExtractor[]
        {
            new RouteParameterExtractor(),
            new QueryParameterExtractor(),
            new BodyParameterExtractor(),
            new HeaderParameterExtractor(),
            new FormParameterExtractor(),
        };
    }
}
