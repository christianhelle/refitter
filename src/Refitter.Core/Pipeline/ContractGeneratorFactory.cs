namespace Refitter.Core;

/// <summary>
/// Prepares a document (mutators) and creates the <see cref="ContractGenerator"/> for the given settings.
/// </summary>
internal sealed class ContractGeneratorFactory
{
    private readonly ICodeGenerationConfiguration codeGeneration;
    private readonly INamingConfiguration naming;
    private readonly ApiDocument document;
    private readonly IReadOnlyList<IDocumentMutator> mutators;

    public ContractGeneratorFactory(
        RefitGeneratorSettings settings,
        ApiDocument document,
        IReadOnlyList<IDocumentMutator>? mutators = null)
    {
        codeGeneration = settings;
        naming = settings;
        this.document = document;
        this.mutators = mutators ?? CreateDefaultMutators(settings);
    }

    private static IReadOnlyList<IDocumentMutator> CreateDefaultMutators(RefitGeneratorSettings settings) =>
    [
        new DisableAdditionalPropertiesMutator(settings.GenerateDefaultAdditionalProperties),
        new FlattenPrimitiveAllOfMutator(),
        new OneOfDiscriminatorToAllOfMutator(),
        new FixMissingIntegerTypesMutator(),
        new CustomIntegerTypeMutator(settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32),
    ];

    public ContractGenerator Create()
    {
        foreach (var mutator in mutators)
            mutator.Mutate(document);

        // The property name generator needs the contract type names, which the generator resolves
        ContractGenerator? generator = null;

        var useNativeRecords = codeGeneration.ImmutableRecords || codeGeneration.CodeGeneratorSettings?.GenerateNativeRecords is true;
        var settings = new ContractGeneratorSettings
        {
            Namespace = naming.ContractsNamespace ?? naming.Namespace,
            JsonPolymorphicSerializationStyle = codeGeneration.UsePolymorphicSerialization
                ? ContractPolymorphicSerializationStyle.SystemTextJson
                : ContractPolymorphicSerializationStyle.JsonInheritanceConverter,
            TypeAccessModifier = codeGeneration.TypeAccessibility.ToString().ToLowerInvariant(),
#pragma warning disable CS0618 // Honored until custom templates are removed
            TemplateDirectory = codeGeneration.CustomTemplateDirectory,
#pragma warning restore CS0618
            PropertyNameGenerator = CreatePropertyNameGenerator(schema => generator!.GetTypeName(schema)),
            TypeNameGenerator = CreateTypeNameGenerator(),
            EnumNameGenerator = new UniqueContractEnumNameGenerator(),
            ParameterNameGenerator = CreateParameterNameGenerator(),
        };

        // The document's schemas are registered when the generator is created, before the code generator settings
        // (e.g. InlineNamedAny) apply, as they always have been
        generator = new ContractGenerator(document, settings);

        ApplyCodeGeneratorSettings(codeGeneration.CodeGeneratorSettings, settings);
        settings.GenerateNativeRecords = useNativeRecords;
        settings.ClassStyle = useNativeRecords ? ContractClassStyle.Record : ContractClassStyle.Poco;
        return generator;
    }

    private IContractPropertyNameGenerator CreatePropertyNameGenerator(Func<ApiSchema, string?> getTypeName)
    {
        if (codeGeneration.CodeGeneratorSettings?.PropertyNameProvider is { } propertyNameProvider)
        {
            return new UniqueContractPropertyNameGenerator(
                new ProviderContractPropertyNameGenerator(propertyNameProvider),
                getTypeName);
        }

        IContractPropertyNameGenerator inner = naming.PropertyNamingPolicy switch
        {
            PropertyNamingPolicy.PreserveOriginal => new PreserveOriginalContractPropertyNameGenerator(),
            _ => new ContractPropertyNameGenerator(),
        };

        return new UniqueContractPropertyNameGenerator(inner, getTypeName);
    }

    private IOperationParameterNameGenerator CreateParameterNameGenerator() =>
        codeGeneration.ParameterNameProvider is { } parameterNameProvider
            ? new ProviderOperationParameterNameGenerator(parameterNameProvider)
            : new DefaultOperationParameterNameGenerator();

    private SafeContractTypeNameGenerator CreateTypeNameGenerator()
    {
        // Schemas are listed twice (components and definitions are the same schemas), like before
        var preferredExactTypeNameHints = document.Components.Schemas.Keys
            .Concat(document.Definitions.Keys)
            .Where(hint => string.Equals(IdentifierUtils.NormalizeSchemaTypeNameHint(hint), hint, StringComparison.Ordinal))
            .ToList();

        return new SafeContractTypeNameGenerator(new HashSet<string>(preferredExactTypeNameHints, StringComparer.Ordinal));
    }

    private static void ApplyCodeGeneratorSettings(CodeGeneratorSettings? source, ContractGeneratorSettings destination)
    {
        if (source is null)
            return;

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
    }
}
