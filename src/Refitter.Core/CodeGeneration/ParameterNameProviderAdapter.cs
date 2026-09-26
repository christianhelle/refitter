using NSwag;
using NSwag.CodeGeneration;

namespace Refitter.Core;

/// <summary>
/// Exposes an <see cref="IParameterNameProvider"/> to NSwag as an <see cref="IParameterNameGenerator"/>.
/// </summary>
internal sealed class ParameterNameProviderAdapter(IParameterNameProvider provider) : IParameterNameGenerator
{
    internal IParameterNameProvider Provider => provider;

    public string Generate(OpenApiParameter parameter, IEnumerable<OpenApiParameter> allParameters) =>
        provider.GetParameterName(
            new ParameterNameContext(
                parameter.Name,
                ToSource(parameter.Kind),
                parameter.IsRequired,
                allParameters.Select(p => p.Name).ToList()));

    private static ParameterSource ToSource(OpenApiParameterKind kind) =>
        kind switch
        {
            OpenApiParameterKind.Path => ParameterSource.Path,
            OpenApiParameterKind.Query => ParameterSource.Query,
            OpenApiParameterKind.Header => ParameterSource.Header,
            OpenApiParameterKind.Cookie => ParameterSource.Cookie,
            OpenApiParameterKind.Body => ParameterSource.Body,
            OpenApiParameterKind.FormData => ParameterSource.Form,
            _ => ParameterSource.Other,
        };
}
