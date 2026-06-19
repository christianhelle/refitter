using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

/// <summary>
/// Custom template factory solely for tweaking the JsonPolymorphic attribute
/// with UnknownDerivedTypeHandling = FallBackToBaseType and IgnoreUnrecognizedTypeDiscriminators = true.
/// This class should be removed if NSwag eventually supports setting UnknownDerivedTypeHandling
/// and IgnoreUnrecognizedTypeDiscriminators.
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

    protected override string GetEmbeddedLiquidTemplate(string language, string template)
    {
        var templateText = base.GetEmbeddedLiquidTemplate(language, template);
        return template switch
        {
            "Class" => templateText
                .Replace(
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\")]",
                    "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"{{ Discriminator }}\", UnknownDerivedTypeHandling = System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]"),
            _ => templateText,
        };
    }
}
