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

    public ParameterList Build(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType,
        string? formObjectParameterType = null)
    {
        var extracted = new List<MethodParameter>();

        extracted.AddRange(RouteParameterExtractor.Extract(operationModel));

        var (queryParameters, dynamicQuerystringCode) = queryExtractor.Extract(
            operationModel,
            settings,
            dynamicQuerystringParameterType);
        extracted.AddRange(queryParameters);

        extracted.AddRange(bodyExtractor.Extract(operationModel, settings));
        extracted.AddRange(headerExtractor.Extract(operationModel, operation, settings));
        var (formParameters, formObjectCode) = FormParameterExtractor.Extract(
            operationModel,
            operation,
            settings,
            formObjectParameterType ?? ParameterNaming.GetFormObjectParameterType(dynamicQuerystringParameterType));
        extracted.AddRange(formParameters);

        var parameters = OptionalParameterReorderer.Reorder(
            extracted,
            settings,
            operationModel.Parameters);

        var hasAppendedParameter = true;
        if (settings.ApizrSettings?.WithRequestOptions == true)
            parameters.Add(new MethodParameter("IApizrRequestOptions", "options") { Attributes = ["RequestOptions"] });
        else if (settings.UseCancellationTokens)
            parameters.Add(new MethodParameter("CancellationToken", "cancellationToken") { DefaultValue = "default" });
        else
            hasAppendedParameter = false;

        var (path, uniqueParameters) = RouteTemplateDisambiguator.Disambiguate(
            operationModel.Path,
            ParameterNameDeduplicator.Deduplicate(parameters, hasAppendedParameter),
            operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Path)
                .Select(p => p.Name)
                .ToList());

        return new ParameterList(path, uniqueParameters.Select(parameter => parameter.Render()).ToList(), dynamicQuerystringCode)
        {
            FormObjectCode = formObjectCode,
        };
    }
}
