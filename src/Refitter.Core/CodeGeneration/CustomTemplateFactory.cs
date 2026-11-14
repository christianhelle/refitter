using NJsonSchema.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

/// <summary>
/// Custom template factory for tweaking NJsonSchema's embedded C# templates:
/// <list type="bullet">
/// <item>Adds UnknownDerivedTypeHandling = FallBackToBaseType and IgnoreUnrecognizedTypeDiscriminators = true
/// to the JsonPolymorphic attribute. Remove once NSwag supports setting these.</item>
/// <item>Keeps types deriving from the array base type as classes when native records are generated,
/// since a record cannot inherit from a class (#1398).</item>
/// </list>
/// </summary>
internal class CustomTemplateFactory(
    CSharpGeneratorSettings settings)
    : NSwag.CodeGeneration.DefaultTemplateFactory(
        settings,
        [
            typeof(CSharpGenerator).Assembly,
            typeof(CSharpGeneratorBaseSettings).Assembly,
            typeof(CSharpGeneratorSettings).Assembly
        ])
{
    private const string ClassDeclaration = "partial {{ ClassType }} {{ClassName}}";

    private readonly CSharpGeneratorSettings csharpSettings = settings;

    protected override string GetEmbeddedLiquidTemplate(string language, string template)
    {
        var templateText = base.GetEmbeddedLiquidTemplate(language, template);
        return template switch
        {
            "Class" => templateText
                .Replace(
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\")]",
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\", UnknownDerivedTypeHandling = System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]")
                .Replace(ClassDeclaration, GetClassDeclaration()),
            _ => templateText,
        };
    }

    private string GetClassDeclaration()
    {
        // Liquid template text is loaded when the contracts are generated, after the user settings are applied
        return "partial {% if HasInheritance and BaseClassName contains \"" + csharpSettings.ArrayBaseType + "<\" %}class" +
               "{% else %}{{ ClassType }}{% endif %} {{ClassName}}";
    }
}
