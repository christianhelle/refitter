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

        csharpClientGeneratorSettings.CSharpGeneratorSettings.TemplateFactory = new CustomTemplateFactory(
            csharpClientGeneratorSettings.CSharpGeneratorSettings,
            [
                typeof(CSharpGenerator).Assembly,
                typeof(CSharpGeneratorBaseSettings).Assembly,
                typeof(CustomTemplateFactory).Assembly,
            ]);

        var generator = new CustomCSharpClientGenerator(
            document,
            csharpClientGeneratorSettings,
            settings.UsePolymorphicSerialization);

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
    /// solely for the purpose of supporting UsePolymorphicSerialization
    /// This class and its templates should be removed when NSwag supports this feature.
    /// </summary>
    private class CustomTemplateFactory : NSwag.CodeGeneration.DefaultTemplateFactory
    {
        /// <summary>Initializes a new instance of the <see cref="DefaultTemplateFactory" /> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="assemblies">The assemblies.</param>
        public CustomTemplateFactory(CodeGeneratorSettingsBase settings, Assembly[] assemblies)
            : base(settings, assemblies)
        {
        }

        /// <summary>Tries to load an embedded Liquid template.</summary>
        /// <param name="language">The language.</param>
        /// <param name="template">The template name.</param>
        /// <returns>The template.</returns>
        protected override string GetEmbeddedLiquidTemplate(string language, string template)
        {
            template = template.TrimEnd('!');
            var assembly = Assembly.GetExecutingAssembly(); // this code is running in Refitter.Core and Refitter.SourceGenerator
            var resourceName = $"{assembly.GetName().Name}.Templates.{template}.liquid";

            var resource = assembly.GetManifestResourceStream(resourceName);
            if (resource != null)
            {
                using (var reader = new StreamReader(resource))
                {
                    return reader.ReadToEnd();
                }
            }

            return base.GetEmbeddedLiquidTemplate(language, template);
        }
    }
}
