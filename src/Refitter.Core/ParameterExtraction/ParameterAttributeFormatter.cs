using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

/// <summary>
/// Builds the Refit attributes (AliasAs, Query, Body) emitted alongside generated parameters.
/// </summary>
internal static class ParameterAttributeFormatter
{
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

    public static string GetBodyAttribute(
        CSharpParameterModel parameter,
        string contentType,
        RefitGeneratorSettings settings)
    {
        // Refit serializes bodies with the content serializer (JSON) unless told otherwise
        var mediaType = contentType.Split(';')[0].Trim();
        if (string.Equals(mediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return "Body(BodySerializationMethod.UrlEncoded)";
        }

        var anyType = settings.CodeGeneratorSettings?.AnyType ?? "object";
        var parameterType = ParameterTypeResolver.ResolveType(parameter.Type);

        if (parameterType.Equals(anyType, StringComparison.OrdinalIgnoreCase) ||
            parameterType.Contains("JsonElement", StringComparison.OrdinalIgnoreCase))
        {
            return "Body(BodySerializationMethod.Serialized)";
        }

        return "Body";
    }

    // An OpenAPI 3 style/explode declared on the parameter takes precedence over the global setting
    private static CollectionFormat GetCollectionFormat(CSharpParameterModel parameter, RefitGeneratorSettings settings) =>
        (parameter.Style, parameter.Explode) switch
        {
            (OpenApiParameterStyle.PipeDelimited, _) => CollectionFormat.Pipes,
            (OpenApiParameterStyle.SpaceDelimeted, _) => CollectionFormat.Ssv,
            (OpenApiParameterStyle.Form or OpenApiParameterStyle.Undefined, false) => CollectionFormat.Csv,
            (OpenApiParameterStyle.Form, _) or (OpenApiParameterStyle.Undefined, true) => CollectionFormat.Multi,
            _ => settings.CollectionFormat,
        };

    public static string GetQueryAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        return (parameter, settings) switch
        {
            { parameter.IsArray: true }
                => $"Query(CollectionFormat.{GetCollectionFormat(parameter, settings)})",
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
