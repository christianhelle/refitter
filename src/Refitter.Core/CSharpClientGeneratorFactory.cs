using System.Reflection;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

internal class CSharpClientGeneratorFactory(RefitGeneratorSettings settings, OpenApiDocument document)
{
    public CustomCSharpClientGenerator Create()
    {
        if (!settings.GenerateDefaultAdditionalProperties)
        {
            foreach (var kvp in document.Components.Schemas)
            {
                kvp.Value.ActualSchema.AllowAdditionalProperties = false;
            }
        }

        var csharpClientGeneratorSettings = new CSharpClientGeneratorSettings
        {
            GenerateClientClasses = false,
            GenerateDtoTypes = true,
            GenerateClientInterfaces = false,
            GenerateExceptionClasses = false,
            CodeGeneratorSettings = { PropertyNameGenerator = settings.CodeGeneratorSettings?.PropertyNameGenerator ?? new CustomCSharpPropertyNameGenerator() },
            CSharpGeneratorSettings =
            {
                Namespace = settings.ContractsNamespace ?? settings.Namespace,
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                JsonPolymorphicSerializationStyle = settings.UsePolymorphicSerialization ? CSharpJsonPolymorphicSerializationStyle.SystemTextJson : CSharpJsonPolymorphicSerializationStyle.NJsonSchema,
                TypeAccessModifier = settings.TypeAccessibility.ToString().ToLowerInvariant(),
                ClassStyle =
                    settings.ImmutableRecords ||
                    settings.CodeGeneratorSettings?.GenerateNativeRecords is true
                    ? CSharpClassStyle.Record
                    : CSharpClassStyle.Poco,
                GenerateNativeRecords =
                    settings.ImmutableRecords ||
                    settings.CodeGeneratorSettings?.GenerateNativeRecords is true,
            }
        };

        if (settings.ParameterNameGenerator != default)
        {
            csharpClientGeneratorSettings.ParameterNameGenerator = settings.ParameterNameGenerator;
        }

        csharpClientGeneratorSettings.CSharpGeneratorSettings.TemplateFactory = new CustomTemplateFactory(csharpClientGeneratorSettings.CSharpGeneratorSettings);

        var generator = new CustomCSharpClientGenerator(
            document,
            csharpClientGeneratorSettings);

        MapCSharpGeneratorSettings(
            settings.CodeGeneratorSettings,
            generator.Settings.CSharpGeneratorSettings);

        return generator;
    }

    private static void MapCSharpGeneratorSettings(
        CodeGeneratorSettings? source,
        CSharpGeneratorSettings destination)
    {
        if (source is null)
        {
            return;
        }

        var defaultInstance = new CodeGeneratorSettings();
        foreach (var property in source.GetType().GetProperties())
        {
            var value = property.GetValue(source);
            if (value == null)
            {
                continue;
            }

            if (value.Equals(property.GetValue(defaultInstance)))
            {
                continue;
            }

            var settingsProperty = destination.GetType().GetProperty(property.Name);
            if (settingsProperty == null ||
                !settingsProperty.PropertyType.IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            settingsProperty.SetValue(destination, value);
        }
    }

    /// <summary>
    /// custom template factory
    /// solely for the purpose of tweaking the JsonPolymorphic attribute with UnknownDerivedTypeHandling = FallBackToBaseType and IgnoreUnrecognizedTypeDiscriminators = true
    /// This class should be removed if NSwag eventually supports setting UnknownDerivedTypeHandling and IgnoreUnrecognizedTypeDiscriminators.
    /// </summary>
    private class CustomTemplateFactory : NSwag.CodeGeneration.DefaultTemplateFactory
    {
        /// <summary>Initializes a new instance of the <see cref="CustomTemplateFactory" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public CustomTemplateFactory(CodeGeneratorSettingsBase settings)
            : base(settings, [typeof(CSharpGenerator).Assembly, typeof(CSharpGeneratorBaseSettings).Assembly])
        {
        }

        /// <inheritdoc />
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
}
