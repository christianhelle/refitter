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

    protected override string GetEmbeddedLiquidTemplate(string language, string template)
    {
        var templateText = base.GetEmbeddedLiquidTemplate(language, template);
        return template switch
        {
            "Class" => templateText
                .Replace(
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\")]",
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\", UnknownDerivedTypeHandling = System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]")
                .Replace(ClassDeclaration, ClassDeclarationWithCollectionBaseAsClass),
            _ => templateText,
        };
    }
}
