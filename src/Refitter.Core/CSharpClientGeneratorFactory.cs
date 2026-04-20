
using System.Reflection;
using System.Runtime.CompilerServices;
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
        if (!settings.GenerateDefaultAdditionalProperties && document.Components?.Schemas != null)
        {
            foreach (var kvp in document.Components.Schemas)
            {
                kvp.Value.ActualSchema.AllowAdditionalProperties = false;
            }
        }

        ConvertOneOfWithDiscriminatorToAllOf();
        FixMissingTypesWithIntegerFormat();
        ApplyCustomIntegerType();

        var csharpClientGeneratorSettings = new CSharpClientGeneratorSettings
        {
            GenerateClientClasses = false,
            GenerateDtoTypes = true,
            GenerateClientInterfaces = false,
            GenerateExceptionClasses = false,
            CodeGeneratorSettings = { PropertyNameGenerator = CreatePropertyNameGenerator() },
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

        // Auto-enable optional properties as nullable when nullable reference types enabled.
        // This ensures consistent nullability behavior: when NRT is enabled, optional properties
        // should be nullable to avoid CS8618 warnings for required members.
        // Note: This is a behavioral change from v1.x where GenerateOptionalPropertiesAsNullable
        // defaulted to false. If you need the old behavior, explicitly set
        // GenerateOptionalPropertiesAsNullable = false in your settings.
        if (generator.Settings.CSharpGeneratorSettings.GenerateNullableReferenceTypes)
        {
            generator.Settings.CSharpGeneratorSettings.GenerateOptionalPropertiesAsNullable = true;
        }

        return generator;
    }

    private IPropertyNameGenerator CreatePropertyNameGenerator()
    {
        if (settings.CodeGeneratorSettings?.PropertyNameGenerator is { } propertyNameGenerator)
        {
            return propertyNameGenerator;
        }

        return settings.PropertyNamingPolicy switch
        {
            PropertyNamingPolicy.PreserveOriginal => new PreserveOriginalPropertyNameGenerator(),
            _ => new CustomCSharpPropertyNameGenerator(),
        };
    }

    /// <summary>
    /// Converts schemas that use oneOf/anyOf with a discriminator to use the allOf inheritance
    /// pattern that NSwag's C# code generator understands. Without this transformation,
    /// NSwag generates undefined anonymous types (e.g., "IdentityProvider2") instead of
    /// proper base class references.
    /// </summary>
    private void ConvertOneOfWithDiscriminatorToAllOf()
    {
        // Null-safe check for Swagger 2.0 docs that use definitions instead (#1015)
        if (document.Components?.Schemas == null)
            return;

        foreach (var kvp in document.Components.Schemas)
        {
            var schema = kvp.Value?.ActualSchema;
            if (schema == null)
                continue;

            if (schema.DiscriminatorObject == null)
                continue;

            var unionSchemas = schema.OneOf.Concat(schema.AnyOf).ToArray();
            if (unionSchemas.Length == 0)
                continue;

            // Ensure the base schema is typed as an object
            if (schema.Type == JsonObjectType.None || schema.Type == JsonObjectType.Null)
                schema.Type = JsonObjectType.Object;

            // For each subtype, add allOf pointing to the base schema if not already present
            foreach (var subSchemaRef in unionSchemas)
            {
                var subSchema = subSchemaRef?.ActualSchema;
                if (subSchema == null)
                    continue;

                bool alreadyInherits = subSchema.AllOf.Any(
                    a => a.HasReference && a.ActualSchema == schema);
                if (!alreadyInherits)
                {
                    var reference = new JsonSchema { Reference = schema };
                    subSchema.AllOf.Add(reference);
                }
            }

            // Remove the oneOf/anyOf from the base schema now that subtypes use allOf
            schema.OneOf.Clear();
            schema.AnyOf.Clear();
        }
    }

    private void FixMissingTypesWithIntegerFormat() =>
        TraverseDocumentSchemas(FixSchemaTypeFromFormat);

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

    private void ApplyCustomIntegerType()
    {
        var customIntegerType = settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32;
        if (customIntegerType == IntegerType.Int32)
            return;

        TraverseDocumentSchemas(FixSchemaIntegerFormat);
    }

    private static void FixSchemaIntegerFormat(JsonSchema schema)
    {
        if (schema.Type == JsonObjectType.Integer &&
            string.IsNullOrEmpty(schema.Format))
        {
            schema.Format = "int64";
        }
    }

    private void TraverseDocumentSchemas(Action<JsonSchema> visitor)
    {
        var visited = new HashSet<JsonSchema>(JsonSchemaReferenceComparer.Instance);
        var schemasToProcess = new Stack<JsonSchema>();

        foreach (var schema in EnumerateDocumentSchemaRoots())
        {
            TryPush(schema, schemasToProcess);
        }

        while (schemasToProcess.Count > 0)
        {
            var actualSchema = schemasToProcess.Pop().ActualSchema;
            if (!visited.Add(actualSchema))
            {
                continue;
            }

            visitor(actualSchema);

            foreach (var childSchema in EnumerateTraversableSchemas(actualSchema))
            {
                TryPush(childSchema, schemasToProcess);
            }
        }
    }

    private IEnumerable<JsonSchema?> EnumerateDocumentSchemaRoots()
    {
        if (document.Components?.Schemas != null)
        {
            foreach (var schema in document.Components.Schemas.Values)
            {
                yield return schema;
            }
        }

        if (document.Paths == null)
        {
            yield break;
        }

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem == null)
            {
                continue;
            }

            foreach (var parameter in pathItem.Parameters)
            {
                yield return parameter;
            }

            foreach (var operation in pathItem.Values)
            {
                if (operation == null)
                {
                    continue;
                }

                foreach (var parameter in operation.ActualParameters)
                {
                    yield return parameter;
                }

                if (operation.RequestBody?.Content != null)
                {
                    foreach (var content in operation.RequestBody.Content.Values)
                    {
                        yield return content.Schema;
                    }
                }

                foreach (var response in operation.ActualResponses.Values)
                {
                    if (response.Headers != null)
                    {
                        foreach (var header in response.Headers.Values)
                        {
                            yield return header;
                        }
                    }

                    if (response.Content == null)
                    {
                        continue;
                    }

                    foreach (var content in response.Content.Values)
                    {
                        yield return content.Schema;
                    }
                }
            }
        }
    }

    private static IEnumerable<JsonSchema?> EnumerateTraversableSchemas(JsonSchema schema)
    {
        yield return schema.AdditionalItemsSchema;
        yield return schema.AdditionalPropertiesSchema;
        yield return schema.DictionaryKey;
        yield return schema.Item;

        if (schema.Items != null)
        {
            foreach (var item in schema.Items)
            {
                yield return item;
            }
        }

        yield return schema.Not;

        foreach (var property in schema.Properties.Values)
        {
            yield return property;
        }

        foreach (var subSchema in schema.AllOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schema.OneOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schema.AnyOf)
        {
            yield return subSchema;
        }

        foreach (var definition in schema.Definitions.Values)
        {
            yield return definition;
        }
    }

    private static void TryPush(JsonSchema? schema, Stack<JsonSchema> stack)
    {
        if (schema == null)
        {
            return;
        }

        stack.Push(schema);
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

    private sealed class JsonSchemaReferenceComparer : IEqualityComparer<JsonSchema>
    {
        public static JsonSchemaReferenceComparer Instance { get; } = new();

        public bool Equals(JsonSchema? x, JsonSchema? y) =>
            ReferenceEquals(x, y);

        public int GetHashCode(JsonSchema obj) =>
            RuntimeHelpers.GetHashCode(obj);
    }
}
