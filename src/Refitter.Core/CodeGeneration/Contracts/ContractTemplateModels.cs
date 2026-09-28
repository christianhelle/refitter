#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Refitter.Core;

// The template models expose the members the contract templates use, and compute them on demand in the same
// way (and order) the generated code has always been based on. See THIRD-PARTY-NOTICES.md.

/// <summary>The model of the class template.</summary>
[SuppressMessage(
    "Minor Code Smell",
    "S2325:Methods and properties that don't access instance data should be static",
    Justification = "The Liquid templates, including custom templates, can only read instance members")]
internal sealed class ClassTemplateModel
{
    private readonly ContractTypeResolver resolver;
    private readonly ApiSchema schema;
    private readonly ContractGeneratorSettings settings;
    private readonly ApiDocument rootObject;
    internal readonly List<PropertyModel> PropertiesList;
    private readonly List<PropertyModel> allProperties;

    public ClassTemplateModel(
        string typeName,
        ContractGeneratorSettings settings,
        ContractTypeResolver resolver,
        ApiSchema schema,
        ApiDocument rootObject)
    {
        this.resolver = resolver;
        this.schema = schema;
        this.settings = settings;
        this.rootObject = rootObject;
        ClassName = typeName;
        SchemaTitle = schema.Title;

        var actualProperties = schema.ActualProperties;
        PropertiesList = new List<PropertyModel>(actualProperties.Count);
        foreach (var property in actualProperties.Values)
        {
            if (!property.IsInheritanceDiscriminator)
            {
                PropertiesList.Add(new PropertyModel(this, property, resolver, settings));
            }
        }

        if (schema.InheritedSchema != null)
        {
            BaseClass = new ClassTemplateModel(BaseClassName!, settings, resolver, schema.InheritedSchema, rootObject);
            allProperties = new List<PropertyModel>(PropertiesList.Count + BaseClass.allProperties.Count);
            allProperties.AddRange(BaseClass.allProperties);
            allProperties.AddRange(PropertiesList);
        }
        else
        {
            allProperties = PropertiesList;
        }
    }

    public string ClassName { get; }

    public string? SchemaTitle { get; }

    public bool IsObject => schema.ActualTypeSchema.IsObject;

    public bool IsAbstract => schema.ActualTypeSchema.IsAbstract;

    public IDictionary<string, object?>? ExtensionData => schema.ExtensionData;

    public ICollection<DerivedClassModel> DerivedClasses =>
        DerivedSchemaFinder.Find(schema, rootObject)
            .Where(_ => schema.ActualSchema.ResponsibleDiscriminatorObject != null)
            .Select(p => new DerivedClassModel(p.Value, p.Key, schema.ActualSchema.ResponsibleDiscriminatorObject!, resolver))
            .ToList();

    public bool UseSystemTextJson => true;

    public bool UseSystemTextJsonPolymorphicSerialization =>
        settings.JsonPolymorphicSerializationStyle == ContractPolymorphicSerializationStyle.SystemTextJson;

    public string Namespace => settings.Namespace;

    public bool GenerateNullableReferenceTypes => settings.GenerateNullableReferenceTypes;

    public bool HasAdditionalPropertiesType =>
        HasAdditionalPropertiesTypeInBaseClass ||
        (!schema.IsDictionary &&
         !schema.ActualTypeSchema.IsDictionary &&
         !schema.IsArray &&
         !schema.ActualTypeSchema.IsArray &&
         (schema.ActualTypeSchema.AllowAdditionalProperties || schema.ActualTypeSchema.AdditionalPropertiesSchema != null));

    public bool HasAdditionalPropertiesTypeInBaseClass => BaseClass?.HasAdditionalPropertiesType ?? false;

    public bool GenerateAdditionalPropertiesProperty => HasAdditionalPropertiesType && !HasAdditionalPropertiesTypeInBaseClass;

    public string? AdditionalPropertiesType => HasAdditionalPropertiesType ? "object" : null;

    public IEnumerable<PropertyModel> Properties => PropertiesList;

    public IEnumerable<PropertyModel> AllProperties => allProperties;

    public bool HasDescription =>
        schema is not ApiSchemaProperty &&
        (!string.IsNullOrEmpty(schema.Description) || !string.IsNullOrEmpty(schema.ActualTypeSchema.Description));

    public string? Description =>
        string.IsNullOrEmpty(schema.Description) ? schema.ActualTypeSchema.Description : schema.Description;

    public bool RenderInpc => settings.ClassStyle == ContractClassStyle.Inpc;

    public bool RenderPrism => settings.ClassStyle == ContractClassStyle.Prism;

    public bool RenderRecord => settings.ClassStyle == ContractClassStyle.Record;

    public string ClassType => settings.GenerateNativeRecords ? "record" : "class";

    public bool GenerateNativeRecords => settings.GenerateNativeRecords;

    public bool GenerateJsonMethods => settings.GenerateJsonMethods;

    public bool HasDiscriminator => !string.IsNullOrEmpty(schema.ActualDiscriminator);

    public string? Discriminator => schema.ActualDiscriminator;

    public bool IsTuple => schema.ActualTypeSchema.IsTuple;

    public IEnumerable<string> TupleTypes =>
        schema.ActualTypeSchema.Items
            .Select(i => resolver.Resolve(i, i.IsNullable(settings.SchemaType), string.Empty));

    public bool HasInheritance => schema.InheritedTypeSchema != null;

    public bool SortConstructorParameters => settings.SortConstructorParameters;

    public string? BaseClassName =>
        HasInheritance
            ? resolver.Resolve(schema.InheritedTypeSchema!, isNullable: false, string.Empty)
                .Replace(settings.ArrayType + "<", settings.ArrayBaseType + "<")
                .Replace(settings.DictionaryType + "<", settings.DictionaryBaseType + "<")
            : null;

    public ClassTemplateModel? BaseClass { get; }

    public bool InheritsExceptionSchema =>
        resolver.ExceptionSchema != null && schema.InheritsSchema(resolver.ExceptionSchema);

    public bool UseDateFormatConverter => settings.DateType.StartsWith("System.DateTime", StringComparison.Ordinal);

    public string TypeAccessModifier => settings.TypeAccessModifier;

    public bool UseRequiredKeyword => settings.UseRequiredKeyword;

    public string WriteAccessor => settings.WriteAccessor;

    public string PropertySetterAccessModifier =>
        string.IsNullOrEmpty(settings.PropertySetterAccessModifier) ? string.Empty : settings.PropertySetterAccessModifier + " ";

    public string JsonSerializerParameterCode =>
        !string.IsNullOrEmpty(settings.JsonSerializerSettingsTransformationMethod)
            ? settings.JsonSerializerSettingsTransformationMethod + "(new System.Text.Json.JsonSerializerOptions())"
            : "new System.Text.Json.JsonSerializerOptions()";

    public string JsonConvertersArrayCode =>
        settings.JsonConverters is { Length: > 0 } converters
            ? "new System.Text.Json.Serialization.JsonConverter[] { " + string.Join(", ", converters.Select(c => "new " + c + "()")) + " }"
            : string.Empty;

    public bool IsDeprecated => schema.IsDeprecated || schema.AllOf.Any(s => s.IsDeprecated);

    public bool HasDeprecatedMessage => !string.IsNullOrEmpty(DeprecatedMessage);

    public string? DeprecatedMessage =>
        schema.DeprecatedMessage ?? schema.AllOf.Select(s => s.DeprecatedMessage).FirstOrDefault(m => !string.IsNullOrEmpty(m));

    public sealed class DerivedClassModel
    {
        internal DerivedClassModel(string? typeName, ApiSchema schema, ApiDiscriminator discriminator, ContractTypeResolver resolver)
        {
            var mapping = discriminator.Mapping.SingleOrDefault(m => m.Value.ActualTypeSchema == schema.ActualTypeSchema);
            ClassName = resolver.GetOrGenerateTypeName(schema, typeName);
            IsAbstract = schema.ActualTypeSchema.IsAbstract;
            if (mapping.Value != null)
                Discriminator = mapping.Key;
            else
                Discriminator = !string.IsNullOrEmpty(typeName) ? typeName! : ClassName;
        }

        public string Discriminator { get; }

        public string ClassName { get; }

        public bool IsAbstract { get; }
    }
}

/// <summary>The model of a property in the class template.</summary>
internal sealed class PropertyModel
{
    private static readonly HashSet<string?> RangeFormats = new() { "int32", "float", "double", "int64", "uint64", "decimal" };

    private readonly ClassTemplateModel classTemplateModel;
    private readonly ApiSchemaProperty property;
    private readonly ContractTypeResolver resolver;
    private readonly ContractGeneratorSettings settings;

    public PropertyModel(
        ClassTemplateModel classTemplateModel,
        ApiSchemaProperty property,
        ContractTypeResolver resolver,
        ContractGeneratorSettings settings)
    {
        this.classTemplateModel = classTemplateModel;
        this.property = property;
        this.resolver = resolver;
        this.settings = settings;
        PropertyName = settings.PropertyNameGenerator.Generate(property);
    }

    public string PropertyName { get; set; }

    public string Name => property.Name;

    public string Type => resolver.Resolve(property, property.IsNullable(settings.SchemaType), GetTypeNameHint());

    public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

    public string? DefaultValue =>
        settings.ValueGenerator.GetDefaultValue(
            property,
            property.IsNullable(settings.SchemaType),
            Type,
            property.Name,
            settings.GenerateDefaultValues,
            resolver);

    public bool IsNullable => (settings.GenerateOptionalPropertiesAsNullable && !property.IsRequired) || property.IsNullable(settings.SchemaType);

    public bool IsRequired => property.IsRequired;

    public bool IsStringEnumArray =>
        property.ActualTypeSchema.IsArray &&
        property.ActualTypeSchema.Item != null &&
        property.ActualTypeSchema.Item.ActualTypeSchema.IsEnumeration &&
        property.ActualTypeSchema.Item.ActualTypeSchema.Type.IsString();

    public IDictionary<string, object?>? ExtensionData => property.ExtensionData;

    public string? Format => property.ActualSchema.Format;

    public bool HasDescription => !string.IsNullOrEmpty(property.Description);

    public string? Description => property.Description;

    public string FieldName => settings.FieldNamePrefix + ConversionUtilities.ConvertToLowerCamelCase(PropertyName, firstCharacterMustBeAlpha: true);

    public bool AllowEmptyStrings =>
        property.ActualTypeSchema.Type.IsString() && (!property.MinLength.HasValue || property.MinLength == 0);

    public bool HasSetter =>
        property.IsNullable(settings.SchemaType) ||
        ((!property.ActualTypeSchema.IsArray || !settings.GenerateImmutableArrayProperties) &&
         (!property.ActualTypeSchema.IsDictionary || !settings.GenerateImmutableDictionaryProperties));

    public string JsonPropertyRequiredCode
    {
        get
        {
            if (settings.RequiredPropertiesMustBeDefined && property.IsRequired)
            {
                return !IsNullable ? "Newtonsoft.Json.Required.Always" : "Newtonsoft.Json.Required.AllowNull";
            }

            return !IsNullable
                ? "Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore"
                : "Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore";
        }
    }

    public bool RenderRequiredAttribute =>
        settings.GenerateDataAnnotations &&
        property.IsRequired &&
        !property.IsNullable(settings.SchemaType) &&
        (property.ActualTypeSchema.IsAnyType ||
         property.ActualTypeSchema.Type.IsObject() ||
         property.ActualTypeSchema.Type.IsString() ||
         property.ActualTypeSchema.Type.IsArray());

    public bool RenderRangeAttribute =>
        settings.GenerateDataAnnotations &&
        (property.ActualTypeSchema.Type.IsNumber() || property.ActualTypeSchema.Type.IsInteger()) &&
        (property.ActualSchema.Maximum.HasValue || property.ActualSchema.Minimum.HasValue);

    private bool IsDecimalRange => GetSchemaFormat(property.ActualSchema) == "decimal";

    public string? RangeType => IsDecimalRange ? "decimal" : null;

    public string RangeMinimumValue
    {
        get
        {
            var actualSchema = property.ActualSchema;
            var schemaFormat = GetSchemaFormat(actualSchema);
            var rangeFormat = GetRangeFormat(schemaFormat);
            var rangeType = GetRangeType(schemaFormat);
            var minimum = actualSchema.Minimum;
            if (minimum.HasValue && actualSchema.IsExclusiveMinimum)
            {
                if (schemaFormat is "int32" or "int64")
                    minimum += 1m;
                else if (actualSchema.MultipleOf.HasValue)
                    minimum += actualSchema.MultipleOf;
            }

            if (IsDecimalRange)
                return ContractValueGenerator.ConvertNumberToString(minimum ?? decimal.MinValue);

            return minimum.HasValue
                ? ContractValueGenerator.GetNumericValue(actualSchema.Type, EnsureBounds(schemaFormat, minimum.Value), rangeFormat)
                : rangeType + ".MinValue";
        }
    }

    public string RangeMaximumValue
    {
        get
        {
            var actualSchema = property.ActualSchema;
            var schemaFormat = GetSchemaFormat(actualSchema);
            var rangeFormat = GetRangeFormat(schemaFormat);
            var rangeType = GetRangeType(schemaFormat);
            var maximum = actualSchema.Maximum;
            if (maximum.HasValue && actualSchema.IsExclusiveMaximum)
            {
                if (schemaFormat is "int32" or "int64")
                    maximum -= 1m;
                else if (actualSchema.MultipleOf.HasValue)
                    maximum -= actualSchema.MultipleOf;
            }

            if (IsDecimalRange)
                return ContractValueGenerator.ConvertNumberToString(maximum ?? decimal.MaxValue);

            return maximum.HasValue
                ? ContractValueGenerator.GetNumericValue(actualSchema.Type, EnsureBounds(schemaFormat, maximum.Value), rangeFormat)
                : rangeType + ".MaxValue";
        }
    }

    public bool RenderStringLengthAttribute
    {
        get
        {
            if (!settings.GenerateDataAnnotations)
                return false;

            if (property.IsRequired && property.MinLength == 1 && !property.MaxLength.HasValue)
                return false;

            return property.ActualTypeSchema.Type.IsString() &&
                   (property.ActualSchema.MinLength.HasValue || property.ActualSchema.MaxLength.HasValue);
        }
    }

    public int StringLengthMinimumValue => property.ActualSchema.MinLength.GetValueOrDefault();

    public string StringLengthMaximumValue =>
        property.ActualSchema.MaxLength.HasValue
            ? property.ActualSchema.MaxLength.Value.ToString(CultureInfo.InvariantCulture)
            : "int.MaxValue";

    public bool RenderMinLengthAttribute =>
        settings.GenerateDataAnnotations && property.ActualTypeSchema.Type.IsArray() && property.ActualSchema.MinItems > 0;

    public int MinLengthAttribute => property.ActualSchema.MinItems;

    public bool RenderMaxLengthAttribute =>
        settings.GenerateDataAnnotations && property.ActualTypeSchema.Type.IsArray() && property.ActualSchema.MaxItems > 0;

    public int MaxLengthAttribute => property.ActualSchema.MaxItems;

    public bool RenderRegularExpressionAttribute =>
        settings.GenerateDataAnnotations &&
        property.ActualTypeSchema.Type.IsString() &&
        !string.IsNullOrEmpty(property.ActualSchema.Pattern);

    public string? RegularExpressionValue => property.ActualSchema.Pattern?.Replace("\"", "\"\"");

    public bool IsStringEnum => property.ActualTypeSchema.IsEnumeration && property.ActualTypeSchema.Type.IsString();

    public bool IsDate => property.ActualSchema.Format == "date";

    public bool IsDeprecated => property.IsDeprecated;

    public bool HasDeprecatedMessage => !string.IsNullOrEmpty(property.DeprecatedMessage);

    public string? DeprecatedMessage => property.DeprecatedMessage;

    private string GetTypeNameHint()
    {
        var propertyName = PropertyName;
        if (!property.IsEnumeration)
            return propertyName;

        var className = classTemplateModel.ClassName;
        if (className.Contains("Anonymous"))
            return propertyName;

        if (propertyName.StartsWith(className, StringComparison.OrdinalIgnoreCase))
            return propertyName;

        return className + ConversionUtilities.ConvertToUpperCamelCase(PropertyName, firstCharacterMustBeAlpha: false);
    }

    private string? GetSchemaFormat(ApiSchema schema)
    {
        if (Type is "long" or "long?")
            return "int64";

        if (schema.Format == null)
        {
            switch (schema.Type)
            {
                case ApiObjectType.Integer:
                    return "int32";
                case ApiObjectType.Number:
                    return "double";
            }
        }

        return schema.Format;
    }

    private static string GetRangeFormat(string? format) => RangeFormats.Contains(format) ? format! : "double";

    private static string GetRangeType(string? format) =>
        format switch
        {
            "int32" => "int",
            "float" => "float",
            "double" => "double",
            "int64" => "long",
            "uint64" => "ulong",
            "decimal" => "decimal",
            _ => "double",
        };

    private static decimal EnsureBounds(string? format, decimal value) =>
        format switch
        {
            "int32" => Clamp(value, int.MinValue, int.MaxValue),
            "int64" => Clamp(value, long.MinValue, long.MaxValue),
            "uint64" => Clamp(value, 0m, ulong.MaxValue),
            _ => value,
        };

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        Math.Max(min, Math.Min(max, value));
}

/// <summary>The model of the enum template.</summary>
[SuppressMessage(
    "Minor Code Smell",
    "S2325:Methods and properties that don't access instance data should be static",
    Justification = "The Liquid templates, including custom templates, can only read instance members")]
internal sealed class EnumTemplateModel(string typeName, ApiSchema schema, ContractGeneratorSettings settings)
{
    public string Name { get; } = typeName;

    public bool HasDescription => schema is not ApiSchemaProperty && !string.IsNullOrEmpty(schema.Description);

    public string? Description => schema.Description;

    public IDictionary<string, object?>? ExtensionData => schema.ExtensionData;

    public bool IsStringEnum => schema.Type != ApiObjectType.Integer;

    public string TypeAccessModifier => settings.TypeAccessModifier;

    public bool IsEnumAsBitFlags => settings.EnforceFlagEnums || schema.IsFlagEnumerable;

    public bool UseSystemTextJson => true;

    public decimal JsonLibraryVersion => settings.JsonLibraryVersion;

    public bool HasExtendedValueRange => schema.Format == "int64";

    public IEnumerable<EnumerationItemModel> Enums
    {
        get
        {
            var items = new List<EnumerationItemModel>();
            for (var i = 0; i < schema.Enumeration.Count; i++)
            {
                var value = schema.Enumeration[i];
                if (value == null)
                    continue;

                var description = schema.EnumerationDescriptions.Count > i ? schema.EnumerationDescriptions[i] : null;
                if (schema.Type.IsInteger())
                {
                    var name = schema.EnumerationNames.Count > i ? schema.EnumerationNames[i] : "_" + value;
                    if (schema.IsFlagEnumerable && TryGetInt64(value, out var valueInt64))
                    {
                        items.Add(new EnumerationItemModel(
                            settings.EnumNameGenerator.Generate(i, name, value, schema),
                            name,
                            value.ToString()!,
                            description,
                            valueInt64.ToString(CultureInfo.InvariantCulture),
                            valueInt64.ToString(CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        items.Add(new EnumerationItemModel(
                            settings.EnumNameGenerator.Generate(i, name, value, schema),
                            name,
                            value.ToString()!,
                            description,
                            value.ToString(),
                            (1 << i).ToString(CultureInfo.InvariantCulture)));
                    }
                }
                else
                {
                    var name = schema.EnumerationNames.Count > i ? schema.EnumerationNames[i] : value.ToString()!;
                    items.Add(new EnumerationItemModel(
                        settings.EnumNameGenerator.Generate(i, name, value, schema),
                        name,
                        value.ToString()!,
                        description,
                        i.ToString(CultureInfo.InvariantCulture),
                        (1 << i).ToString(CultureInfo.InvariantCulture)));
                }
            }

            return items;
        }
    }

    private static bool TryGetInt64(object value, out long valueInt64)
    {
        switch (value)
        {
            case byte b:
                valueInt64 = b;
                return true;
            case sbyte sb:
                valueInt64 = sb;
                return true;
            case short s:
                valueInt64 = s;
                return true;
            case ushort us:
                valueInt64 = us;
                return true;
            case int i:
                valueInt64 = i;
                return true;
            case uint ui:
                valueInt64 = ui;
                return true;
            case long l:
                valueInt64 = l;
                return true;
            case ulong ul:
                valueInt64 = (long)ul;
                return true;
            case float f when IsWholeNumber(f):
                valueInt64 = (long)f;
                return true;
            case double d when IsWholeNumber(d):
                valueInt64 = (long)d;
                return true;
            default:
                valueInt64 = 0;
                return false;
        }
    }

    // Whether Math.Floor(value) == value: the fractional part is exactly zero (no positive double is smaller
    // than double.Epsilon), and infinities count as whole numbers
    private static bool IsWholeNumber(double value) =>
        double.IsInfinity(value) || Math.Abs(value - Math.Floor(value)) < double.Epsilon;
}

/// <summary>An enum member in the enum template.</summary>
internal sealed class EnumerationItemModel(
    string name,
    string originalName,
    string value,
    string? description,
    string? internalValue,
    string? internalFlagValue)
{
    public string Name { get; } = name;

    public string OriginalName { get; } = originalName;

    public string Value { get; } = value;

    public string? Description { get; } = description;

    public string? InternalValue { get; } = internalValue;

    public string? InternalFlagValue { get; } = internalFlagValue;
}

/// <summary>The model of the JSON inheritance converter and attribute templates.</summary>
[SuppressMessage(
    "Minor Code Smell",
    "S2325:Methods and properties that don't access instance data should be static",
    Justification = "The Liquid templates, including custom templates, can only read instance members")]
internal sealed class JsonInheritanceConverterTemplateModel(ContractGeneratorSettings settings)
{
    public bool UseSystemTextJson => true;

    public bool UseSystemTextJsonPolymorphicSerialization =>
        settings.JsonPolymorphicSerializationStyle == ContractPolymorphicSerializationStyle.SystemTextJson;
}

/// <summary>The model of the date format converter template.</summary>
[SuppressMessage(
    "Minor Code Smell",
    "S2325:Methods and properties that don't access instance data should be static",
    Justification = "The Liquid templates, including custom templates, can only read instance members")]
internal sealed class DateFormatConverterTemplateModel(ContractGeneratorSettings settings)
{
    public bool GenerateDateFormatConverterClass => !settings.ExcludedTypeNames.Contains("DateFormatConverter");

    public bool UseSystemTextJson => true;

    public string DateType => settings.DateType;
}

/// <summary>The model of the file template that contains all contracts.</summary>
[SuppressMessage(
    "Minor Code Smell",
    "S2325:Methods and properties that don't access instance data should be static",
    Justification = "The Liquid templates, including custom templates, can only read instance members")]
internal sealed class ContractFileTemplateModel(
    string classes,
    ApiDocument document,
    ContractGeneratorSettings settings)
{
    public string Namespace => settings.Namespace ?? string.Empty;

    public string[] NamespaceUsages => Array.Empty<string>();

    public bool GenerateNullableReferenceTypes => settings.GenerateNullableReferenceTypes;

    public bool GenerateContracts => true;

    public bool GenerateImplementation => true;

    public bool GenerateClientClasses => false;

    public string Clients => string.Empty;

    public string Classes { get; } = classes;

    public bool RequiresJsonExceptionConverter => false;

    public string ExceptionModelClass => "Exception";

    public bool RequiresFileParameterType
    {
        get
        {
            if (settings.ExcludedTypeNames.Contains("FileParameter"))
                return false;

            var operations = document.GetOperations().ToList();
            return operations.Any(o => o.Operation.GetActualParameters().Any(p => p.ActualTypeSchema.IsBinary)) ||
                   operations.Any(o => HasBinaryContent(o.Operation.ActualRequestBody));
        }
    }

    private static bool HasBinaryContent(ApiRequestBody? requestBody) =>
        requestBody != null && requestBody.Content.Any(c => IsBinaryContent(c.Value));

    private static bool IsBinaryContent(ApiMediaType content)
    {
        var schema = content.Schema;
        if (schema == null)
            return false;

        return schema.IsBinary ||
               schema.ActualSchema.ActualProperties.Any(p =>
                   p.Value.IsBinary ||
                   (p.Value.Item != null && p.Value.Item.IsBinary) ||
                   p.Value.Items.Any(i => i.IsBinary));
    }

    public bool GenerateFileResponseClass =>
        !settings.ExcludedTypeNames.Contains("FileResponse") &&
        document.GetOperations().Any(o => o.Operation.HasActualResponse((_, response) => response.IsBinary(o.Operation)));

    public bool GenerateExceptionClasses => false;

    public bool WrapResponses => false;

    public bool GenerateResponseClasses => true;

    public IEnumerable<string> ResponseClassNames => Array.Empty<string>();

    public IEnumerable<string> ExceptionClassNames => Array.Empty<string>();
}
