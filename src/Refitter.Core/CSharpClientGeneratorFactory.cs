using System.Reflection;
using NJsonSchema;
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

        var customIntegerType = settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32;
        var applyIntegerType = customIntegerType != IntegerType.Int32;

        ProcessSchemas(document.Components.Schemas, applyIntegerType);

        foreach (var path in document.Paths)
        {
            if (path.Value == null) continue;

            foreach (var operation in path.Value.Values)
            {
                if (operation == null) continue;

                foreach (var parameter in operation.Parameters)
                {
                    FixSchemaTypeFromFormat(parameter.ActualSchema);
                    if (applyIntegerType &&
                        parameter.ActualSchema.Type == JsonObjectType.Integer &&
                        string.IsNullOrEmpty(parameter.ActualSchema.Format))
                    {
                        parameter.ActualSchema.Format = "int64";
                    }
                }

                if (operation.RequestBody?.Content != null)
                {
                    foreach (var content in operation.RequestBody.Content.Values)
                    {
                        ProcessSchema(content.Schema, applyIntegerType);
                    }
                }

                foreach (var response in operation.Responses.Values)
                {
                    if (response.Content != null)
                    {
                        foreach (var content in response.Content.Values)
                        {
                            ProcessSchema(content.Schema, applyIntegerType);
                        }
                    }
                }
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
                TemplateDirectory = settings.CustomTemplateDirectory,
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

    private void ProcessSchemas(IDictionary<string, JsonSchema> schemas, bool applyIntegerType)
    {
        foreach (var schema in schemas.Values)
        {
            ProcessSchema(schema, applyIntegerType);
        }
    }

    private void ProcessSchema(JsonSchema? schema, bool applyIntegerType)
    {
        if (schema == null) return;

        var actualSchema = schema.ActualSchema;

        FixSchemaTypeFromFormat(actualSchema);

        if (applyIntegerType &&
            actualSchema.Type == JsonObjectType.Integer &&
            string.IsNullOrEmpty(actualSchema.Format))
        {
            actualSchema.Format = "int64";
        }

        foreach (var property in actualSchema.Properties.Values)
        {
            ProcessSchema(property, applyIntegerType);
        }

        if (actualSchema.Item != null)
        {
            ProcessSchema(actualSchema.Item, applyIntegerType);
        }

        if (actualSchema.AdditionalPropertiesSchema != null)
        {
            ProcessSchema(actualSchema.AdditionalPropertiesSchema, applyIntegerType);
        }

        foreach (var subSchema in actualSchema.AllOf)
        {
            ProcessSchema(subSchema, applyIntegerType);
        }

        foreach (var subSchema in actualSchema.OneOf)
        {
            ProcessSchema(subSchema, applyIntegerType);
        }

        foreach (var subSchema in actualSchema.AnyOf)
        {
            ProcessSchema(subSchema, applyIntegerType);
        }
    }

    private void FixSchemaTypeFromFormat(JsonSchema schema)
    {
        // If type is not set but format indicates a numeric type (int32, int64, float, double), set the type based on format
        if ((schema.Type == JsonObjectType.None || schema.Type == JsonObjectType.Null) &&
            !string.IsNullOrEmpty(schema.Format))
        {
            if (schema.Format == "int32" || schema.Format == "int64")
            {
                schema.Type = JsonObjectType.Integer;
            }
            else if (schema.Format == "float" || schema.Format == "double")
            {
                schema.Type = JsonObjectType.Number;
            }
        }
    }

    private static readonly PropertyInfo[] SourceProperties = typeof(CodeGeneratorSettings).GetProperties();
    private static readonly CodeGeneratorSettings DefaultCodeGeneratorSettings = new();
    private static readonly Dictionary<string, PropertyInfo?> DestinationPropertyCache = new();

    private static void MapCSharpGeneratorSettings(
        CodeGeneratorSettings? source,
        CSharpGeneratorSettings destination)
    {
        if (source is null)
        {
            return;
        }

        var destinationType = destination.GetType();
        foreach (var property in SourceProperties)
        {
            var value = property.GetValue(source);
            if (value == null)
            {
                continue;
            }

            if (value.Equals(property.GetValue(DefaultCodeGeneratorSettings)))
            {
                continue;
            }

            if (!DestinationPropertyCache.TryGetValue(property.Name, out var settingsProperty))
            {
                settingsProperty = destinationType.GetProperty(property.Name);
                DestinationPropertyCache[property.Name] = settingsProperty;
            }

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
