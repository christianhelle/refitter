namespace Refitter.Core;

internal enum ContractClassStyle
{
    Poco,
    Inpc,
    Prism,
    Record,
}

internal enum ContractPolymorphicSerializationStyle
{
    NJsonSchema,
    SystemTextJson,
}

/// <summary>
/// Settings for generating contract types (classes, records and enums) from schemas.
/// See THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class ContractGeneratorSettings
{
    public ContractGeneratorSettings()
    {
        ValueGenerator = new ContractValueGenerator(this);
    }

    public ApiSchemaType SchemaType { get; set; } = ApiSchemaType.Swagger2;

    public string Namespace { get; set; } = "MyNamespace";

    public bool RequiredPropertiesMustBeDefined { get; set; } = true;

    public bool GenerateDataAnnotations { get; set; } = true;

    public string AnyType { get; set; } = "object";

    public string DateType { get; set; } = "System.DateTimeOffset";

    public string DateTimeType { get; set; } = "System.DateTimeOffset";

    public string TimeType { get; set; } = "System.TimeSpan";

    public string TimeSpanType { get; set; } = "System.TimeSpan";

    public string NumberType { get; set; } = "double";

    public string NumberFloatType { get; set; } = "float";

    public string NumberDoubleType { get; set; } = "double";

    public string NumberDecimalType { get; set; } = "decimal";

    public string ArrayType { get; set; } = "System.Collections.Generic.ICollection";

    public string DictionaryType { get; set; } = "System.Collections.Generic.IDictionary";

    public string ArrayInstanceType { get; set; } = "System.Collections.ObjectModel.Collection";

    public string DictionaryInstanceType { get; set; } = "System.Collections.Generic.Dictionary";

    public string ArrayBaseType { get; set; } = "System.Collections.ObjectModel.Collection";

    public string DictionaryBaseType { get; set; } = "System.Collections.Generic.Dictionary";

    public ContractClassStyle ClassStyle { get; set; } = ContractClassStyle.Poco;

    public decimal JsonLibraryVersion { get; set; } = 8.0m;

    public ContractPolymorphicSerializationStyle JsonPolymorphicSerializationStyle { get; set; } =
        ContractPolymorphicSerializationStyle.NJsonSchema;

    public string TypeAccessModifier { get; set; } = "public";

    public string PropertySetterAccessModifier { get; set; } = string.Empty;

    public string[]? JsonConverters { get; set; }

    public bool GenerateImmutableArrayProperties { get; set; }

    public bool GenerateImmutableDictionaryProperties { get; set; }

    public bool HandleReferences { get; set; }

    public string? JsonSerializerSettingsTransformationMethod { get; set; }

    public bool GenerateJsonMethods { get; set; }

    public bool EnforceFlagEnums { get; set; }

    public bool UseRequiredKeyword { get; set; }

    public string WriteAccessor { get; set; } = "set";

    public bool InlineNamedDictionaries { get; set; }

    public bool InlineNamedTuples { get; set; } = true;

    public bool InlineNamedArrays { get; set; }

    public bool InlineNamedAny { get; set; }

    public bool GenerateOptionalPropertiesAsNullable { get; set; }

    public bool GenerateNullableReferenceTypes { get; set; }

    public bool GenerateNativeRecords { get; set; }

    public string FieldNamePrefix { get; set; } = "_";

    public bool SortConstructorParameters { get; set; } = true;

    public bool GenerateDefaultValues { get; set; } = true;

    public string[] ExcludedTypeNames { get; set; } = Array.Empty<string>();

    public string? TemplateDirectory { get; set; }

    public IContractPropertyNameGenerator PropertyNameGenerator { get; set; } = new ContractPropertyNameGenerator();

    public ISchemaTypeNameGenerator TypeNameGenerator { get; set; } = new SchemaTypeNameGenerator();

    public IContractEnumNameGenerator EnumNameGenerator { get; set; } = new DefaultContractEnumNameGenerator();

    public ContractValueGenerator ValueGenerator { get; }

    // Client generator settings that shape the types of operation parameters and responses
    public string ResponseArrayType { get; set; } = "System.Collections.Generic.ICollection";

    public string ResponseDictionaryType { get; set; } = "System.Collections.Generic.IDictionary";

    public string ParameterArrayType { get; set; } = "System.Collections.Generic.IEnumerable";

    public string ParameterDictionaryType { get; set; } = "System.Collections.Generic.IDictionary";

    public IOperationParameterNameGenerator ParameterNameGenerator { get; set; } = new DefaultOperationParameterNameGenerator();
}
