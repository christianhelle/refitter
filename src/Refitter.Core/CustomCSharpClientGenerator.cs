using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Models;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class CustomCSharpClientGenerator(OpenApiDocument document, CSharpClientGeneratorSettings settings, bool usePolymorphicSerialization)
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
    : CSharpClientGenerator(document, settings)
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
{
    internal CSharpOperationModel CreateOperationModel(OpenApiOperation operation) =>
        CreateOperationModel(operation, Settings);

    /// <summary>
    /// override to generate DTO types with our custom CSharpGenerator
    /// This code should be removed when NSwag supports STJ polymorphic serialization
    /// </summary>
    protected override IEnumerable<CodeArtifact> GenerateDtoTypes()
    {
        var generator = new CCustomSharpGenerator(document, Settings.CSharpGeneratorSettings, (CSharpTypeResolver)Resolver, usePolymorphicSerialization);
        return generator.GenerateTypes();
    }

    private class CCustomSharpGenerator(OpenApiDocument document, CSharpGeneratorSettings settings, CSharpTypeResolver resolver, bool usePolymorphicSerialization)
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
        : CSharpGenerator(document, settings, resolver)
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
    {
        /// <summary>
        /// override to generate Class with our custom ClassTemplateModel
        /// code is taken from NJsonSchema.CodeGeneration.CSharp.CSharpGenerator.GenerateType
        /// </summary>
        protected override CodeArtifact GenerateType(JsonSchema schema, string typeNameHint)
        {
            // Check if we have a custom type override for this schema type and format
            var parentGenerator = Parent as CustomCSharpClientGenerator;
            if (parentGenerator != null && schema.Format != null && 
                parentGenerator.Settings is ExtendedCSharpClientGeneratorSettings extendedSettings && 
                extendedSettings.TypeOverrides.TryGetValue($"{schema.Type}:{schema.Format}", out var overriddenType))
            {
                // If we have an override, use it directly instead of generating a type name
                return GenerateClass(schema, overriddenType);
            }
            
            var typeName = resolver.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration)
            {
                return base.GenerateType(schema, typeName);
            }
            else
            {
                return GenerateClass(schema, typeName);
            }
        }

        /// <summary>
        /// override to generate JsonInheritanceAttribute, JsonInheritanceConverter with our custom template models
        /// code is taken from NJsonSchema.CodeGeneration.CSharp.CSharpGenerator.GenerateTypes
        /// </summary>
        public override IEnumerable<CodeArtifact> GenerateTypes()
        {
            var baseArtifacts = base.GenerateTypes();
            var artifacts = new List<CodeArtifact>();

            if (baseArtifacts.Any(r => r.Code.Contains("JsonInheritanceConverter")))
            {
                if (Settings.ExcludedTypeNames?.Contains("JsonInheritanceAttribute") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "JsonInheritanceAttribute", new CustomJsonInheritanceConverterTemplateModel(Settings, usePolymorphicSerialization));
                    artifacts.Add(new CodeArtifact("JsonInheritanceAttribute", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }

                if (Settings.ExcludedTypeNames?.Contains("JsonInheritanceConverter") != true)
                {
                    var template = Settings.TemplateFactory.CreateTemplate("CSharp", "JsonInheritanceConverter", new CustomJsonInheritanceConverterTemplateModel(Settings, usePolymorphicSerialization));
                    artifacts.Add(new CodeArtifact("JsonInheritanceConverter", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template));
                }
            }

            return baseArtifacts.Concat(artifacts);
        }

        /// <summary>
        /// Code is taken from NJsonSchema.CodeGeneration.CSharp.CSharpGenerator.GenerateClass
        /// to instantiate our custom ClassTemplateModel
        /// </summary>
        private CodeArtifact GenerateClass(JsonSchema schema, string typeName)
        {
            var model = new CustomClassTemplateModel(typeName, Settings, resolver, schema, RootObject, usePolymorphicSerialization);

            RenamePropertyWithSameNameAsClass(typeName, model.Properties);

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Class", model);
            return new CodeArtifact(typeName, model.BaseClassName, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Contract, template);
        }

        /// <summary>
        /// Code is taken from NJsonSchema.CodeGeneration.CSharp.CSharpGenerator.RenamePropertyWithSameNameAsClass
        /// </summary>
        private static void RenamePropertyWithSameNameAsClass(string typeName, IEnumerable<PropertyModel> properties)
        {
            var propertyModels = properties as PropertyModel[] ?? properties.ToArray();
            PropertyModel? propertyWithSameNameAsClass = null;
            foreach (var p in propertyModels)
            {
                if (p.PropertyName == typeName)
                {
                    propertyWithSameNameAsClass = p;
                    break;
                }
            }

            if (propertyWithSameNameAsClass != null)
            {
                var number = 1;
                var candidate = typeName + number;
                while (propertyModels.Any(p => p.PropertyName == candidate))
                {
                    number++;
                }

                propertyWithSameNameAsClass.PropertyName = propertyWithSameNameAsClass.PropertyName + number;
            }
        }

        /// <summary>
        /// finally, our custom ClassTemplateModel and CustomJsonInheritanceConverterTemplateModel
        /// to have access to UsePolymorphicSerialization
        /// This code should be removed when NSwag supports STJ polymorphic serialization
        /// </summary>
        private class CustomClassTemplateModel(string typeName, CSharpGeneratorSettings settings, CSharpTypeResolver resolver, JsonSchema schema, object rootObject, bool usePolymorphicSerialization)
            : ClassTemplateModel(typeName, settings, resolver, schema, rootObject)
        {
            public bool UsePolymorphicSerialization => usePolymorphicSerialization;
        }

        private class CustomJsonInheritanceConverterTemplateModel(CSharpGeneratorSettings settings, bool usePolymorphicSerialization)
            : JsonInheritanceConverterTemplateModel(settings)
        {
            public bool UsePolymorphicSerialization => usePolymorphicSerialization;
        }
    }
}

/// <summary>
/// Extended CSharpClientGeneratorSettings class that adds support for type overrides
/// </summary>
internal class ExtendedCSharpClientGeneratorSettings : CSharpClientGeneratorSettings
{
    /// <summary>
    /// Dictionary of type overrides in the format "string:my-date-time" -> "Domain.Specific.DataType"
    /// </summary>
    public Dictionary<string, string> TypeOverrides { get; set; } = new();
}
