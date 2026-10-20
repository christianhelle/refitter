using System.Text;

namespace Refitter.Core;

/// <summary>
/// Builds the source of the <c>[FormObject]</c> wrapper type that carries the non-file fields of a
/// multipart/form-data request body, so Refit writes each property as its own text part.
/// </summary>
internal static class FormObjectParameterBuilder
{
    public static string Build(
        IReadOnlyList<FormField> formFields,
        string formObjectParameterType,
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

        foreach (var formField in formFields)
        {
            var propertyName = formField.VariableName.TrimStart('@').CapitalizeFirstCharacter();
            var attributes = ParameterAttributeFormatter.JoinAttributes(
                ParameterAttributeFormatter.GetAliasAsAttribute(formField.Name, propertyName)).TrimEnd();

            if (formField.IsRequired)
            {
                injectedParametersCodeBuilder.Append(injectedParametersCodeBuilder.Length == 0
                    ? $$"""{{formField.Type}} {{formField.VariableName}}"""
                    : $$""", {{formField.Type}} {{formField.VariableName}}""");

                initializedParametersCodeBuilder.AppendLine();
                initializedParametersCodeBuilder.Append(
    $$"""
                    this.{{propertyName}} = {{formField.VariableName}};
        """);
            }

            propertiesCodeBuilder.AppendLine();
            if (settings.GenerateXmlDocCodeComments && !string.IsNullOrWhiteSpace(formField.Description))
            {
                var escapedDescription = XmlDocumentationGenerator.SanitizeResponseDescription(formField.Description!);
                DynamicQuerystringParameterBuilder.AppendXmlDocComment(escapedDescription, propertiesCodeBuilder);
            }

            if (attributes.Length > 0)
            {
                propertiesCodeBuilder.AppendLine(
    $$"""
                {{attributes}}
        """);
            }

            propertiesCodeBuilder.Append(
    $$"""
                {{modifier}} {{formField.Type}} {{propertyName}} { get; {{setterStyle}}; }
        """);
            propertiesCodeBuilder.AppendLine();
        }

        codeBuilder.AppendLine(
    $$"""
            {{modifier}} {{classStyle}} {{formObjectParameterType}}
            {
        """);

        if (injectedParametersCodeBuilder.Length > 0)
        {
            codeBuilder.AppendLine(
    $$"""
                {{modifier}} {{formObjectParameterType}}({{injectedParametersCodeBuilder}})
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
