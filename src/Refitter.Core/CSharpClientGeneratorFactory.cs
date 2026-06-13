using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

internal class CSharpClientGeneratorFactory
{
    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly IReadOnlyList<IOpenApiDocumentMutator> mutators;

    public CSharpClientGeneratorFactory(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        IReadOnlyList<IOpenApiDocumentMutator>? mutators = null)
    {
        this.settings = settings;
        this.document = document;
        this.mutators = mutators ?? CreateDefaultMutators(settings);
    }

    private static IReadOnlyList<IOpenApiDocumentMutator> CreateDefaultMutators(
        RefitGeneratorSettings settings) =>
    [
        new DisableAdditionalPropertiesMutator(settings.GenerateDefaultAdditionalProperties),
        new OneOfDiscriminatorToAllOfMutator(),
        new FixMissingIntegerTypesMutator(),
        new CustomIntegerTypeMutator(
            settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32),
    ];

    public CustomCSharpClientGenerator Create()
    {
        foreach (var mutator in mutators)
            mutator.Mutate(document);

        var csharpClientGeneratorSettings = new CSharpClientGeneratorSettings
        {
            GenerateClientClasses = false,
            GenerateDtoTypes = true,
            GenerateClientInterfaces = false,
            GenerateExceptionClasses = false,
            CodeGeneratorSettings =
            {
                PropertyNameGenerator = CreatePropertyNameGenerator(),
                TypeNameGenerator = CreateTypeNameGenerator(),
            },
            CSharpGeneratorSettings =
            {
                Namespace = settings.ContractsNamespace ?? settings.Namespace,
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                JsonPolymorphicSerializationStyle =
                    settings.UsePolymorphicSerialization
                        ? CSharpJsonPolymorphicSerializationStyle.SystemTextJson
                        : CSharpJsonPolymorphicSerializationStyle.NJsonSchema,
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
            },
        };

        if (settings.ParameterNameGenerator != null)
        {
            csharpClientGeneratorSettings.ParameterNameGenerator = settings.ParameterNameGenerator;
        }

        csharpClientGeneratorSettings.CSharpGeneratorSettings.TemplateFactory
            = new CustomTemplateFactory(csharpClientGeneratorSettings.CSharpGeneratorSettings);

        var generator = new CustomCSharpClientGenerator(
            document,
            csharpClientGeneratorSettings);

        ApplyCodeGeneratorSettings(
            settings.CodeGeneratorSettings,
            generator.Settings.CSharpGeneratorSettings);

        var useNativeRecords = settings.ImmutableRecords ||
                               generator.Settings.CSharpGeneratorSettings.GenerateNativeRecords;
        generator.Settings.CSharpGeneratorSettings.GenerateNativeRecords = useNativeRecords;
        generator.Settings.CSharpGeneratorSettings.ClassStyle = useNativeRecords
            ? CSharpClassStyle.Record
            : CSharpClassStyle.Poco;

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

    private SafeSchemaTypeNameGenerator CreateTypeNameGenerator()
    {
        var preferredExactTypeNameHints = GetNamedSchemaHints()
            .Where(typeNameHint => string.Equals(
                IdentifierUtils.NormalizeSchemaTypeNameHint(typeNameHint),
                typeNameHint,
                StringComparison.Ordinal))
            .ToList();

        return new(new(preferredExactTypeNameHints, StringComparer.Ordinal));
    }

    private IEnumerable<string> GetNamedSchemaHints()
    {
        if (document.Components?.Schemas != null)
        {
            foreach (var schema in document.Components.Schemas.Keys)
            {
                yield return schema;
            }
        }

        if (document.Definitions == null)
        {
            yield break;
        }

        foreach (var definition in document.Definitions.Keys)
        {
            yield return definition;
        }
    }

    private static void ApplyCodeGeneratorSettings(
        CodeGeneratorSettings? source,
        CSharpGeneratorSettings destination)
    {
        if (source is null)
        {
            return;
        }

        destination.RequiredPropertiesMustBeDefined = source.RequiredPropertiesMustBeDefined;
        destination.GenerateDataAnnotations = source.GenerateDataAnnotations;
        destination.AnyType = source.AnyType;
        destination.DateType = source.DateType;
        destination.DateTimeType = source.DateTimeType;
        destination.TimeType = source.TimeType;
        destination.TimeSpanType = source.TimeSpanType;
        destination.ArrayType = source.ArrayType;
        destination.DictionaryType = source.DictionaryType;
        destination.ArrayInstanceType = source.ArrayInstanceType;
        destination.DictionaryInstanceType = source.DictionaryInstanceType;
        destination.ArrayBaseType = source.ArrayBaseType;
        destination.DictionaryBaseType = source.DictionaryBaseType;
        destination.PropertySetterAccessModifier = source.PropertySetterAccessModifier;
        destination.JsonConverters = source.JsonConverters;
        destination.GenerateImmutableArrayProperties = source.GenerateImmutableArrayProperties;
        destination.GenerateImmutableDictionaryProperties = source.GenerateImmutableDictionaryProperties;
        destination.HandleReferences = source.HandleReferences;
        destination.JsonSerializerSettingsTransformationMethod = source.JsonSerializerSettingsTransformationMethod;
        destination.GenerateJsonMethods = source.GenerateJsonMethods;
        destination.EnforceFlagEnums = source.EnforceFlagEnums;
        destination.InlineNamedDictionaries = source.InlineNamedDictionaries;
        destination.InlineNamedTuples = source.InlineNamedTuples;
        destination.InlineNamedArrays = source.InlineNamedArrays;
        destination.GenerateOptionalPropertiesAsNullable = source.GenerateOptionalPropertiesAsNullable;
        destination.GenerateNullableReferenceTypes = source.GenerateNullableReferenceTypes;
        destination.GenerateNativeRecords = source.GenerateNativeRecords;
        destination.GenerateDefaultValues = source.GenerateDefaultValues;
        destination.InlineNamedAny = source.InlineNamedAny;
        destination.ExcludedTypeNames = source.ExcludedTypeNames;
        destination.JsonLibraryVersion = source.JsonLibraryVersion;

        if (source.PropertyNameGenerator != null)
        {
            destination.PropertyNameGenerator = source.PropertyNameGenerator;
        }
    }
}
