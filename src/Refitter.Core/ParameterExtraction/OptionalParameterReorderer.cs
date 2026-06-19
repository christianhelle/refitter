using System.Text.RegularExpressions;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class OptionalParameterReorderer
{
    private static readonly Regex NullablePattern = new(
        @"\?\s+@?\w+(\s*=\s*[^,]+)?$",
        RegexOptions.Compiled);

    public static List<string> Reorder(
        List<string> parameters,
        RefitGeneratorSettings settings,
        ICollection<CSharpParameterModel> parameterModels)
    {
        if (!settings.OptionalParameters || settings.ApizrSettings?.WithRequestOptions == true)
            return parameters;

        parameters = parameters.OrderBy(c => NullablePattern.IsMatch(c)).ToList();
        for (int index = 0; index < parameters.Count; index++)
        {
            if (NullablePattern.IsMatch(parameters[index]))
            {
                var parameterString = parameters[index];
                var defaultValue = ParameterShared.GetDefaultValueForParameter(parameterString, parameterModels);
                parameters[index] = parameterString + " = " + defaultValue;
            }
        }

        return parameters;
    }
}
