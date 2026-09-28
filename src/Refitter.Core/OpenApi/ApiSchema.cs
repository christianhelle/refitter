#nullable enable

using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// A JSON schema in an OpenAPI document (a component schema, property, parameter, item, ...).
/// </summary>
/// <remarks>
/// The members and their semantics (reference resolution, inheritance, nullability) follow the schema model
/// the generated code has always been based on, so that contracts are named and shaped the same way.
/// See THIRD-PARTY-NOTICES.md.
/// </remarks>
internal class ApiSchema
{
    private static readonly Regex TypeNameTitleRegex = new("^[a-zA-Z0-9_]*$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private ApiSchema? reference;
    private ApiSchema? item;
    private ApiSchema? additionalItemsSchema;
    private bool allowAdditionalItems = true;
    private ApiSchema? additionalPropertiesSchema;
    private bool allowAdditionalProperties;
    private ApiSchema? not;
    private ApiSchema? dictionaryKey;

    public ApiSchema()
        : this(ApiSchemaType.JsonSchema)
    {
    }

    /// <summary>Creates a schema read from (or created for) a document of the given type.</summary>
    /// <param name="schemaType">Swagger 2.0 schemas do not allow additional properties unless they say so.</param>
    public ApiSchema(ApiSchemaType schemaType)
    {
        allowAdditionalProperties = schemaType != ApiSchemaType.Swagger2;
        Properties = new ApiSchemaPropertyDictionary(this);
        PatternProperties = new ApiSchemaPropertyDictionary(this, setNames: true);
        Definitions = new ApiSchemaDictionary(this);
        AllOf = new ApiSchemaList(this);
        AnyOf = new ApiSchemaList(this);
        OneOf = new ApiSchemaList(this);
        Items = new ApiSchemaList(this);
    }

    public string? SchemaVersion { get; set; }

    public string? Id { get; set; }

    public string? Title { get; set; }

    public virtual string? Description { get; set; }

    public string? Format { get; set; }

    public object? Default { get; set; }

    public decimal? MultipleOf { get; set; }

    public decimal? Maximum { get; set; }

    public decimal? ExclusiveMaximum { get; set; }

    public bool IsExclusiveMaximum { get; set; }

    public decimal? Minimum { get; set; }

    public decimal? ExclusiveMinimum { get; set; }

    public bool IsExclusiveMinimum { get; set; }

    public int? MaxLength { get; set; }

    public int? MinLength { get; set; }

    public string? Pattern { get; set; }

    public int MaxItems { get; set; }

    public int MinItems { get; set; }

    public bool UniqueItems { get; set; }

    public int MaxProperties { get; set; }

    public int MinProperties { get; set; }

    public bool IsDeprecated { get; set; }

    public string? DeprecatedMessage { get; set; }

    public bool IsAbstract { get; set; }

    public bool? IsNullableRaw { get; set; }

    public object? Example { get; set; }

    public bool IsFlagEnumerable { get; set; }

    public List<object?> Enumeration { get; set; } = new();

    public List<string> EnumerationNames { get; set; } = new();

    public List<string?> EnumerationDescriptions { get; set; } = new();

    public List<string> RequiredProperties { get; set; } = new();

    public ApiSchemaPropertyDictionary Properties { get; }

    public ApiSchemaPropertyDictionary PatternProperties { get; }

    public ApiSchemaDictionary Definitions { get; }

    public ApiSchemaList AllOf { get; }

    public ApiSchemaList AnyOf { get; }

    public ApiSchemaList OneOf { get; }

    public ApiSchemaList Items { get; }

    public ApiDiscriminator? DiscriminatorObject { get; set; }

    public Dictionary<string, object?>? ExtensionData { get; set; }

    /// <summary>The unresolved <c>$ref</c> of this schema.</summary>
    public string? ReferencePath { get; set; }

    /// <summary>The path or URL of the document this schema was loaded from, for external documents.</summary>
    public string? DocumentPath { get; set; }

    public virtual object? Parent { get; set; }

    public ApiSchema? ParentSchema => Parent as ApiSchema;

    public ApiObjectTypes Type { get; set; }

    public ApiSchema? Reference
    {
        get => reference;
        set
        {
            if (reference != value)
            {
                reference = value;
                ReferencePath = null;
            }

            if (value != null)
            {
                Type = ApiObjectTypes.None;
            }
        }
    }

    public ApiSchema? Item
    {
        get => item;
        set
        {
            if (item != value)
            {
                item = value;
                if (item != null)
                {
                    item.Parent = this;
                    Items.Clear();
                }
            }
        }
    }

    public ApiSchema? Not
    {
        get => not;
        set
        {
            not = value;
            if (not != null)
                not.Parent = this;
        }
    }

    public ApiSchema? DictionaryKey
    {
        get => dictionaryKey;
        set
        {
            dictionaryKey = value;
            if (dictionaryKey != null)
                dictionaryKey.Parent = this;
        }
    }

    public bool AllowAdditionalItems
    {
        get => allowAdditionalItems;
        set
        {
            if (allowAdditionalItems != value)
            {
                allowAdditionalItems = value;
                if (!allowAdditionalItems)
                    AdditionalItemsSchema = null;
            }
        }
    }

    public ApiSchema? AdditionalItemsSchema
    {
        get => additionalItemsSchema;
        set
        {
            if (additionalItemsSchema != value)
            {
                additionalItemsSchema = value;
                if (additionalItemsSchema != null)
                    AllowAdditionalItems = true;
            }
        }
    }

    public bool AllowAdditionalProperties
    {
        get => allowAdditionalProperties;
        set
        {
            if (allowAdditionalProperties != value)
            {
                allowAdditionalProperties = value;
                if (!allowAdditionalProperties)
                    AdditionalPropertiesSchema = null;
            }
        }
    }

    public ApiSchema? AdditionalPropertiesSchema
    {
        get => additionalPropertiesSchema;
        set
        {
            if (additionalPropertiesSchema != value)
            {
                additionalPropertiesSchema = value;
                if (additionalPropertiesSchema != null)
                    AllowAdditionalProperties = true;
            }
        }
    }

    public string? Discriminator
    {
        get => DiscriminatorObject?.PropertyName;
        set => DiscriminatorObject = !string.IsNullOrEmpty(value)
            ? new ApiDiscriminator { PropertyName = value }
            : null;
    }

    public bool IsEnumeration => Enumeration.Count > 0;

    public bool IsBinary => Type.IsFile() || (Type.IsString() && Format == "binary");

    public bool HasTypeNameTitle => !string.IsNullOrEmpty(Title) && TypeNameTitleRegex.IsMatch(Title);

    public bool IsObject => Type.IsObject();

    public bool IsArray => Type.IsArray() && Items.Count == 0;

    public bool IsTuple => Type.IsArray() && Items.Count > 0;

    public bool IsDictionary =>
        Type.IsObject() &&
        !HasActualProperties &&
        (AdditionalPropertiesSchema != null || PatternProperties.Count > 0);

    public bool IsAnyType =>
        (Type.IsObject() || Type == ApiObjectTypes.None) &&
        Reference == null &&
        AllOf.Count == 0 &&
        AnyOf.Count == 0 &&
        OneOf.Count == 0 &&
        !HasActualProperties &&
        PatternProperties.Count == 0 &&
        AdditionalPropertiesSchema == null &&
        !MultipleOf.HasValue &&
        !IsEnumeration;

    public bool HasReference =>
        Reference != null || HasAllOfSchemaReference || HasOneOfSchemaReference || HasAnyOfSchemaReference;

    public bool HasAllOfSchemaReference =>
        Type == ApiObjectTypes.None &&
        AnyOf.Count == 0 &&
        OneOf.Count == 0 &&
        Properties.Count == 0 &&
        PatternProperties.Count == 0 &&
        AdditionalPropertiesSchema == null &&
        !MultipleOf.HasValue &&
        !IsEnumeration &&
        AllOf.Count == 1 &&
        AllOf.Any(s => s.HasReference);

    public bool HasOneOfSchemaReference =>
        Type == ApiObjectTypes.None &&
        AnyOf.Count == 0 &&
        AllOf.Count == 0 &&
        Properties.Count == 0 &&
        PatternProperties.Count == 0 &&
        AdditionalPropertiesSchema == null &&
        !MultipleOf.HasValue &&
        !IsEnumeration &&
        OneOf.Count == 1 &&
        OneOf.Any(s => s.HasReference);

    public bool HasAnyOfSchemaReference =>
        Type == ApiObjectTypes.None &&
        AllOf.Count == 0 &&
        OneOf.Count == 0 &&
        Properties.Count == 0 &&
        PatternProperties.Count == 0 &&
        AdditionalPropertiesSchema == null &&
        !MultipleOf.HasValue &&
        !IsEnumeration &&
        AnyOf.Count == 1 &&
        AnyOf.Any(s => s.HasReference);

    /// <summary>The schema itself, or the schema it (transitively) references.</summary>
    public virtual ApiSchema ActualSchema => GetActualSchema(new List<ApiSchema>());

    /// <summary>The schema that describes the type of this schema (e.g. the shared schema of a property).</summary>
    public virtual ApiSchema ActualTypeSchema
    {
        get
        {
            var schema = Reference ?? this;
            if (schema.AllOf.Count > 1 && schema.AllOf.Count(s => !s.HasReference && !s.IsDictionary) == 1)
            {
                return schema.AllOf.First(s => !s.HasReference && !s.IsDictionary).ActualSchema;
            }

            return schema.OneOf.FirstOrDefault(o => !o.IsNullable(ApiSchemaType.JsonSchema))?.ActualSchema ?? ActualSchema;
        }
    }

    /// <summary>The most probable base schema in allOf.</summary>
    public ApiSchema? InheritedSchema
    {
        get
        {
            if (AllOf.Count == 0 || HasReference)
                return null;

            if (AllOf.Count == 1)
                return AllOf[0].ActualSchema;

            var withReference = AllOf.FirstOrDefault(s => s.HasReference);
            if (withReference != null)
                return withReference.ActualSchema;

            var withObjectType = AllOf.FirstOrDefault(s => s.Type.IsObject());
            if (withObjectType != null)
                return withObjectType.ActualSchema;

            return AllOf[0].ActualSchema;
        }
    }

    public ApiSchema? InheritedTypeSchema =>
        InheritedSchema == null && (ActualTypeSchema.IsDictionary || ActualTypeSchema.IsArray || ActualTypeSchema.IsTuple)
            ? ActualTypeSchema
            : InheritedSchema;

    public IReadOnlyCollection<ApiSchema> AllInheritedSchemas
    {
        get
        {
            var inheritedSchema = InheritedSchema;
            if (inheritedSchema == null)
                return Array.Empty<ApiSchema>();

            var result = new List<ApiSchema> { inheritedSchema };
            result.AddRange(inheritedSchema.AllInheritedSchemas);
            return result;
        }
    }

    public ApiDiscriminator? ActualDiscriminatorObject => DiscriminatorObject ?? ActualTypeSchema.DiscriminatorObject;

    public string? ActualDiscriminator => ActualTypeSchema.Discriminator;

    public ApiDiscriminator? ResponsibleDiscriminatorObject =>
        ActualDiscriminatorObject ?? InheritedSchema?.ActualSchema.ResponsibleDiscriminatorObject;

    public bool HasActualProperties
    {
        get
        {
            if (Properties.Count > 0)
                return true;

            return AllOf
                .Select(schema => schema.ActualSchema)
                .Any(schema => schema != InheritedSchema && schema.HasActualProperties);
        }
    }

    /// <summary>The direct properties and the properties of the allOf schemas that are not the base schema.</summary>
    public IReadOnlyDictionary<string, ApiSchemaProperty> ActualProperties
    {
        get
        {
            if (AllOf.Count == 0)
                return Properties.ToDictionary(p => p.Key, p => p.Value);

            var inheritedSchema = InheritedSchema;
            var properties = Properties
                .Union(AllOf.Where(s => s.ActualSchema != inheritedSchema).SelectMany(s => s.ActualSchema.ActualProperties))
                .ToList();

            var duplicates = properties
                .GroupBy(p => p.Key)
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicates.Count > 0)
            {
                throw new InvalidOperationException(
                    "The properties " + string.Join(", ", duplicates.Select(g => "'" + g.Key + "'")) +
                    " are defined multiple times.");
            }

            return properties.ToDictionary(p => p.Key, p => p.Value);
        }
    }

    public virtual bool IsNullable(ApiSchemaType schemaType)
    {
        if (IsNullableRaw == true)
            return true;

        if (IsEnumeration && Enumeration.Contains(null))
            return true;

        if (Type.IsNull())
            return true;

        if ((Type == ApiObjectTypes.None || Type.IsNull()) && OneOf.Any(schema => schema.IsNullable(schemaType)))
            return true;

        var actualSchema = ActualSchema;
        if (actualSchema != this && actualSchema.IsNullable(schemaType))
            return true;

        var actualTypeSchema = ActualTypeSchema;
        if (actualTypeSchema != this && actualTypeSchema.IsNullable(schemaType))
            return true;

        if (ExtensionData != null &&
            ExtensionData.TryGetValue("nullable", out var value) &&
            bool.TryParse(value?.ToString(), out var result))
        {
            return result;
        }

        return false;
    }

    /// <summary>Whether the given schema is a (transitive) base schema of this one.</summary>
    public bool Inherits(ApiSchema schema)
    {
        schema = schema.ActualSchema;
        var inheritedSchema = InheritedSchema;
        return inheritedSchema?.ActualSchema == schema || (inheritedSchema?.Inherits(schema) ?? false);
    }

    public bool InheritsSchema(ApiSchema? parentSchema) =>
        parentSchema != null &&
        ActualSchema.AllInheritedSchemas
            .Concat([this])
            .Any(s => s.ActualSchema == parentSchema.ActualSchema);

    private ApiSchema GetActualSchema(List<ApiSchema> checkedSchemas)
    {
        if (checkedSchemas.Contains(this))
            throw new InvalidOperationException("Cyclic references detected.");

        if (Reference == null && ReferencePath != null)
            throw new InvalidOperationException("The schema reference path '" + ReferencePath + "' has not been resolved.");

        if (!HasReference)
            return this;

        checkedSchemas.Add(this);
        if (HasAllOfSchemaReference)
            return AllOf[0].GetActualSchema(checkedSchemas);

        if (HasOneOfSchemaReference)
            return OneOf[0].GetActualSchema(checkedSchemas);

        if (HasAnyOfSchemaReference)
            return AnyOf[0].GetActualSchema(checkedSchemas);

        return Reference?.GetActualSchema(checkedSchemas) ?? this;
    }
}

/// <summary>A property of an object schema.</summary>
internal sealed class ApiSchemaProperty : ApiSchema
{
    private object? parent;

    public ApiSchemaProperty()
    {
    }

    public ApiSchemaProperty(ApiSchemaType schemaType)
        : base(schemaType)
    {
    }

    public string Name { get; internal set; } = string.Empty;

    public override object? Parent
    {
        get => parent;
        set
        {
            var isFirstParent = parent == null;
            parent = value;
            if (isFirstParent && InitialIsRequired)
                IsRequired = InitialIsRequired;
        }
    }

    public bool IsRequired
    {
        get => ParentSchema!.RequiredProperties.Contains(Name);
        set
        {
            if (ParentSchema == null)
            {
                InitialIsRequired = value;
            }
            else if (value)
            {
                if (!ParentSchema.RequiredProperties.Contains(Name))
                    ParentSchema.RequiredProperties.Add(Name);
            }
            else
            {
                ParentSchema.RequiredProperties.Remove(Name);
            }
        }
    }

    internal bool InitialIsRequired { get; set; }

    public bool IsReadOnly { get; set; }

    public bool IsWriteOnly { get; set; }

    public bool IsInheritanceDiscriminator => ParentSchema!.ActualDiscriminator == Name;

    public override bool IsNullable(ApiSchemaType schemaType)
    {
        if (schemaType == ApiSchemaType.Swagger2 && !IsRequired)
            return true;

        return base.IsNullable(schemaType);
    }
}

/// <summary>A discriminator of a polymorphic schema.</summary>
internal sealed class ApiDiscriminator
{
    public string? PropertyName { get; set; }

    /// <summary>The discriminator values and the schemas (references) they map to.</summary>
    public Dictionary<string, ApiSchema> Mapping { get; } = new();
}
