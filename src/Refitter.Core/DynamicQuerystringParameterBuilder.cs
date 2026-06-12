using System.Text;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class DynamicQuerystringParameterBuilder
{
    public static string Build(
        List<CSharpParameterModel> queryParameters,
        string dynamicQuerystringParameterType,
        RefitGeneratorSettings settings)
    {
        var codeBuilder = new StringBuilder();
        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        var isRecord = settings.ImmutableRecords ||
                       settings.CodeGeneratorSettings?.GenerateNativeRecords is true;
        var classStyle = isRecord
            ? "record"
            : "class";
        var setterStyle = isRecord
            ? "init"
            : "set";

        var injectedParametersCodeBuilder = new StringBuilder();
        var initializedParametersCodeBuilder = new StringBuilder();
        var propertiesCodeBuilder = new StringBuilder();

        foreach (var operationParameter in queryParameters)
        {
            var propertyType = ParameterShared.GetQueryParameterType(operationParameter, settings);
            var variableName = ParameterShared.GetVariableName(operationParameter);
            var attributes = $"{ParameterShared.JoinAttributes(ParameterShared.GetQueryAttribute(operationParameter, settings), ParameterShared.GetAliasAsAttribute(operationParameter.Name, variableName))}";
            var propertyName = variableName.CapitalizeFirstCharacter();
            if (operationParameter.IsRequired)
            {
                injectedParametersCodeBuilder.Append(injectedParametersCodeBuilder.Length == 0
                    ? $$"""{{propertyType}} {{variableName}}"""
                    : $$""", {{propertyType}} {{variableName}}""");

                initializedParametersCodeBuilder.AppendLine();
                initializedParametersCodeBuilder.Append(
    $$"""
                    this.{{propertyName}} = {{variableName}};
        """);
            }

            propertiesCodeBuilder.AppendLine();
            if (settings.GenerateXmlDocCodeComments && !string.IsNullOrWhiteSpace(operationParameter.Description))
            {
                var escapedDescription = XmlDocumentationGenerator.SanitizeResponseDescription(operationParameter.Description);
                ParameterShared.AppendXmlDocComment(escapedDescription, propertiesCodeBuilder);
            }

            propertiesCodeBuilder.Append(
    $$"""
                {{attributes}}
                {{modifier}} {{propertyType}} {{propertyName}} { get; {{setterStyle}}; }
        """);
            var defaultValue = operationParameter.Schema.Default;
            if (defaultValue != null)
            {
                var formattedDefaultValue = ParameterShared.FormatDefaultValue(defaultValue, propertyType);
                propertiesCodeBuilder.Append($" = {formattedDefaultValue};");
            }

            propertiesCodeBuilder.AppendLine();
        }

        codeBuilder.AppendLine(
    $$"""
            {{modifier}} {{classStyle}} {{dynamicQuerystringParameterType}}
            {
        """);

        if (injectedParametersCodeBuilder.Length > 0)
        {
            codeBuilder.AppendLine(
    $$"""
                {{modifier}} {{dynamicQuerystringParameterType}}({{injectedParametersCodeBuilder}})
                {
                    {{initializedParametersCodeBuilder}}
                }
        """);
        }

        codeBuilder.AppendLine(
    $$"""
                {{propertiesCodeBuilder}}
            }
        """);

        return codeBuilder.ToString();
    }
}
