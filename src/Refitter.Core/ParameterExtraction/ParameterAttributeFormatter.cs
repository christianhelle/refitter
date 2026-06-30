using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

/// <summary>
/// Builds the Refit attributes (AliasAs, Query, Body) emitted alongside generated parameters.
/// </summary>
internal static class ParameterAttributeFormatter
{
    public static string GetAliasAsAttribute(CSharpParameterModel parameterModel) =>
        string.Equals(parameterModel.Name, parameterModel.VariableName)
            ? string.Empty
            : $"AliasAs(\"{ParameterNaming.EscapeString(parameterModel.Name)}\")";

    public static string GetAliasAsAttribute(string originalName, string variableName) =>
        string.Equals(originalName, variableName, StringComparison.Ordinal)
            ? string.Empty
            : $"AliasAs(\"{ParameterNaming.EscapeString(originalName)}\")";

    public static string JoinAttributes(params string[] attributes)
    {
        var filteredAttributes = attributes
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();

        if (filteredAttributes.Count == 0)
            return string.Empty;

        return "[" + string.Join(", ", filteredAttributes) + "] ";
    }

    public static string GetBodyAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        var anyType = settings.CodeGeneratorSettings?.AnyType ?? "object";
        var parameterType = ParameterTypeResolver.ResolveType(parameter.Type);

        if (parameterType.Equals(anyType, StringComparison.OrdinalIgnoreCase) ||
            parameterType.Contains("JsonElement", StringComparison.OrdinalIgnoreCase))
        {
            return "Body(BodySerializationMethod.Serialized)";
        }

        return "Body";
    }

    public static string GetQueryAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        return (parameter, settings) switch
        {
            { parameter.IsArray: true }
                => $"Query(CollectionFormat.{settings.CollectionFormat})",
            { parameter.IsDate: true, settings.UseIsoDateFormat: true }
                => "Query(Format = \"yyyy-MM-dd\")",
            { parameter.IsDate: true, settings.CodeGeneratorSettings.DateFormat: not null }
                => $"Query(Format = \"{settings.CodeGeneratorSettings?.DateFormat}\")",
            {
                parameter.IsDateOrDateTime: true, parameter.Schema.Format: "date-time",
                settings.CodeGeneratorSettings.DateTimeFormat: not null
            } => $"Query(Format = \"{settings.CodeGeneratorSettings?.DateTimeFormat}\")",
            _ => "Query",
        };
    }
}
