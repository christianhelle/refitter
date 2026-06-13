using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

/// <summary>
/// Custom template factory solely for the purpose of tweaking the JsonPolymorphic attribute
/// with UnknownDerivedTypeHandling = FallBackToBaseType and IgnoreUnrecognizedTypeDiscriminators = true.
/// This class should be removed if NSwag eventually supports setting UnknownDerivedTypeHandling
/// and IgnoreUnrecognizedTypeDiscriminators.
/// </summary>
internal class CustomTemplateFactory : NSwag.CodeGeneration.DefaultTemplateFactory
{
    public CustomTemplateFactory(CodeGeneratorSettingsBase settings)
        : base(settings, [typeof(CSharpGenerator).Assembly, typeof(CSharpGeneratorBaseSettings).Assembly, typeof(NJsonSchema.CodeGeneration.CSharp.CSharpGeneratorSettings).Assembly])
    {
    }

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
