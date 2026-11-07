using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class OptionalParameterReorderer
{
    /// <summary>
    /// Moves nullable parameters after the others and gives them a default value, when optional parameters are
    /// enabled. Required parameters keep their order, and so do the nullable ones.
    /// </summary>
    public static List<MethodParameter> Reorder(
        List<MethodParameter> parameters,
        RefitGeneratorSettings settings,
        ICollection<CSharpParameterModel> parameterModels)
    {
        if (!settings.OptionalParameters || settings.ApizrSettings?.WithRequestOptions == true)
            return parameters;

        return parameters
            .OrderBy(parameter => parameter.IsNullable)
            .Select(parameter => parameter.IsNullable
                ? parameter with
                {
                    DefaultValue = parameter.DefaultValue ??
                                   ParameterDefaultValueFormatter.GetDefaultValueForParameter(parameter.Name, parameterModels),
                }
                : parameter)
            .ToList();
    }
}
