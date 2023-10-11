using System.Diagnostics;

using NJsonSchema.CodeGeneration.CSharp;

using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

internal class CSharpClientGeneratorFactory(RefitGeneratorSettings settings, OpenApiDocument document)
{
    public CustomCSharpClientGenerator Create()
    {
        var generator = new CustomCSharpClientGenerator(
            document,
            new CSharpClientGeneratorSettings
            {
                GenerateClientClasses = false,
                GenerateDtoTypes = true,
                GenerateClientInterfaces = false,
                GenerateExceptionClasses = false,
                CodeGeneratorSettings =
                {
                    PropertyNameGenerator = new CustomCSharpPropertyNameGenerator(),
                },
                CSharpGeneratorSettings =
                {
                    Namespace = settings.Namespace,
                    JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                    TypeAccessModifier = settings.TypeAccessibility.ToString().ToLowerInvariant(),
                }
            });

        MapCSharpGeneratorSettings(
            settings.CodeGeneratorSettings,
            generator.Settings.CSharpGeneratorSettings);

        return generator;
    }

    private static void MapCSharpGeneratorSettings(
        CSharpGeneratorSettings? source,
        CSharpGeneratorSettings destination)
    {
        if (source is null)
        {
            return;
        }

        var defaultInstance = new CSharpGeneratorSettings();
        foreach (var property in source.GetType().GetProperties())
        {
            if (property.PropertyType != typeof(string) &&
                property.PropertyType != typeof(bool))
            {
                continue;
            }

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
            if (settingsProperty == null)
            {
                continue;
            }

            settingsProperty.SetValue(destination, value);
        }
    }
}