using System.Globalization;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

/// <summary>
/// Custom template factory for tweaking NJsonSchema's embedded C# templates:
/// <list type="bullet">
/// <item>Adds UnknownDerivedTypeHandling = FallBackToBaseType and IgnoreUnrecognizedTypeDiscriminators = true
/// to the JsonPolymorphic attribute. Remove once NSwag supports setting these.</item>
/// <item>Keeps types deriving from a generic base type (the array, dictionary or tuple base type) as classes
/// when native records are generated, since a record cannot inherit from a class (#1398).</item>
/// <item>Renders the schema property names, the discriminator property name and the discriminator mapping keys
/// as escaped verbatim string literals, since NJsonSchema interpolates them raw into C# string literals
/// (GHSA-h924-jfhp-9vjr).</item>
/// </list>
/// </summary>
internal class CustomTemplateFactory(
    CodeGeneratorSettingsBase settings)
    : NSwag.CodeGeneration.DefaultTemplateFactory(
        settings,
        [
            typeof(CSharpGenerator).Assembly,
            typeof(CSharpGeneratorBaseSettings).Assembly,
            typeof(CSharpGeneratorSettings).Assembly
        ])
{
    private const string ClassDeclaration = "partial {{ ClassType }} {{ClassName}}";

    // NJsonSchema caches parsed templates per process, so the template text must not depend on settings.
    // Generated contracts are never generic, so a generic base class is a collection type and never a record.
    private const string ClassDeclarationWithCollectionBaseAsClass =
        "partial {% if HasInheritance and BaseClassName contains \"<\" %}class{% else %}{{ ClassType }}{% endif %} {{ClassName}}";

    // Spec-derived strings that the Class template interpolates raw into C# string literals
    private static readonly string[] StringLiteralVariables =
    [
        "property.Name",
        "Discriminator",
        "derivedClass.Discriminator",
    ];

    protected override string GetEmbeddedLiquidTemplate(string language, string template)
    {
        var templateText = base.GetEmbeddedLiquidTemplate(language, template);
        // A custom template that delegates to the embedded one asks for it with a trailing "!"
        return template.TrimEnd('!') switch
        {
            "Class" => EscapeStringLiterals(templateText
                .Replace(
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\")]",
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\", UnknownDerivedTypeHandling = System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]")
                .Replace(ClassDeclaration, ClassDeclarationWithCollectionBaseAsClass)),
            _ => templateText,
        };
    }

    // C# escape sequences for the characters that end a regular string literal or the line it is on.
    // The backslash goes first, so the backslashes added by the others are not escaped again.
    private static readonly (string Character, string EscapeSequence)[] StringLiteralEscapes =
    [
        ("\\", "\\\\"),
        ("\"", "\\\""),
        ("\r", "\\r"),
        ("\n", "\\n"),
        ("\u0085", "\\u0085"),
        ("\u2028", "\\u2028"),
        ("\u2029", "\\u2029"),
    ];

    private static readonly string EscapeFilters = string.Concat(
        StringLiteralEscapes.Select(
            escape => $" | replace: {ToLiquidString(escape.Character)}, {ToLiquidString(escape.EscapeSequence)}"));

    private static string EscapeStringLiterals(string templateText) =>
        StringLiteralVariables.Aggregate(
            templateText,
            (text, variable) => text.Replace(
                "\"{{ " + variable + " }}\"",
                "\"{{ " + variable + EscapeFilters + " }}\""));

    // Liquid string literals take backslash escapes. Line breaks and other control characters are written
    // as unicode escapes, since Fluid does not read them as they are.
    private static string ToLiquidString(string value) =>
        "'" + string.Concat(value.Select(ToLiquidCharacter)) + "'";

    private static string ToLiquidCharacter(char c) => c switch
    {
        '\\' => "\\\\",
        '\'' => "\\'",
        _ when char.IsControl(c) || (char.IsSeparator(c) && c != ' ') => "\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture),
        _ => c.ToString(),
    };
}
