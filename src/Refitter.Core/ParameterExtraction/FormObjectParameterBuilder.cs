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

        // A member cannot share its enclosing type's name (CS0542), and [AliasAs] keeps the wire name
        var knownPropertyNames = new HashSet<string>(StringComparer.Ordinal) { formObjectParameterType };

        foreach (var formField in formFields)
        {
            var propertyName = IdentifierUtils.Counted(
                knownPropertyNames,
                formField.VariableName.TrimStart('@').CapitalizeFirstCharacter());
            knownPropertyNames.Add(propertyName);

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

            AppendProperty(formField, propertyName, setterStyle, settings, propertiesCodeBuilder);
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

    private static void AppendProperty(
        FormField formField,
        string propertyName,
        string setterStyle,
        RefitGeneratorSettings settings,
        StringBuilder propertiesCodeBuilder)
    {
        // Refit joins a [FormObject] collection into one comma-separated part unless told to repeat it,
        // whereas a separate collection parameter is sent as one part per item
        var attributes = ParameterAttributeFormatter.JoinAttributes(
            ParameterAttributeFormatter.GetAliasAsAttribute(formField.Name, propertyName),
            formField.IsCollection ? "Query(CollectionFormat.Multi)" : string.Empty).TrimEnd();

        propertiesCodeBuilder.AppendLine();
        if (settings.GenerateXmlDocCodeComments &&
            formField.Description is { } description &&
            !string.IsNullOrWhiteSpace(description))
        {
            var escapedDescription = XmlDocumentationGenerator.SanitizeResponseDescription(description);
            DynamicQuerystringParameterBuilder.AppendXmlDocComment(escapedDescription, propertiesCodeBuilder);
        }

        if (attributes.Length > 0)
        {
            propertiesCodeBuilder.AppendLine(
    $$"""
            {{attributes}}
    """);
        }

        // Refit only reads the public properties of a [FormObject], whatever the type's accessibility
        propertiesCodeBuilder.Append(
    $$"""
            public {{formField.Type}} {{propertyName}} { get; {{setterStyle}}; }
    """);
        propertiesCodeBuilder.AppendLine();
    }
}
