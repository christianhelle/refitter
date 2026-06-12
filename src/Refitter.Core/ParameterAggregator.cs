using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class ParameterAggregator : IParameterExtractor
{
    private readonly IReadOnlyList<IParameterTypeExtractor> extractors;

    public ParameterAggregator()
        : this(GetDefaultExtractors())
    {
    }

    public ParameterAggregator(IReadOnlyList<IParameterTypeExtractor> extractors)
    {
        this.extractors = extractors;
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
        var queryParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .ToList();

        allParameters.AddRange(ExtractQueryParameters(
            operationModel,
            settings,
            dynamicQuerystringParameterType,
            queryParameters,
            out dynamicQuerystringParameters));

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

    private IParameterTypeExtractor GetExtractor<T>() where T : IParameterTypeExtractor
    {
        return extractors.OfType<T>().First();
    }

    private static List<string> ExtractQueryParameters(
        CSharpOperationModel operationModel,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        List<CSharpParameterModel> queryParameters,
        out string? dynamicQuerystringParameters)
    {
        List<string>? parameters = null;
        var dynamicQuerystringParametersCodeBuilder = string.Empty;

        if (settings.UseDynamicQuerystringParameters && queryParameters.Count >= 2)
        {
            var allNullable = queryParameters.All(p =>
                ParameterShared.GetQueryParameterType(p, settings).EndsWith("?"));

            var dynamicQuerystringCode = DynamicQuerystringParameterBuilder.Build(
                queryParameters,
                dynamicQuerystringParameterType,
                settings);

            if (!string.IsNullOrWhiteSpace(dynamicQuerystringCode))
            {
                dynamicQuerystringParametersCodeBuilder = dynamicQuerystringCode;
            }

            var dynamicQuerystringParameter = $"[Query] {dynamicQuerystringParameterType}";
            if (allNullable)
                dynamicQuerystringParameter += "?";
            dynamicQuerystringParameter += " queryParams";
            parameters = [dynamicQuerystringParameter];
        }

        dynamicQuerystringParameters = dynamicQuerystringParametersCodeBuilder;

        parameters ??= QueryParameterExtractor.ExtractSimple(operationModel, settings).ToList();

        return parameters;
    }

    private static IReadOnlyList<IParameterTypeExtractor> GetDefaultExtractors()
    {
        return new IParameterTypeExtractor[]
        {
            new RouteParameterExtractor(),
            new BodyParameterExtractor(),
            new HeaderParameterExtractor(),
            new FormParameterExtractor(),
        };
    }
}
