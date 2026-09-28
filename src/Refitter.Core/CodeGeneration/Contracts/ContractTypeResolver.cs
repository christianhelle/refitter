namespace Refitter.Core;

/// <summary>
/// Resolves the C# type of a schema, and names and registers the schemas that need a generated type.
/// The registration order is the order in which the types are generated. See THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class ContractTypeResolver
{
    private readonly Dictionary<ApiSchema, string> generatedTypeNames = new();
    private readonly List<KeyValuePair<ApiSchema, string>> registrationOrder = new();
    private readonly HashSet<string> reservedTypeNames = new(StringComparer.Ordinal);

    public ContractTypeResolver(ContractGeneratorSettings settings, ApiSchema? exceptionSchema)
    {
        Settings = settings;
        ExceptionSchema = exceptionSchema;
    }

    public ContractGeneratorSettings Settings { get; }

    public ApiSchema? ExceptionSchema { get; }

    /// <summary>The registered schemas and their type names, in registration order.</summary>
    public IReadOnlyList<KeyValuePair<ApiSchema, string>> Types => registrationOrder;

    public bool IsRegistered(ApiSchema schema) => generatedTypeNames.ContainsKey(schema);

    public string? TryGetTypeName(ApiSchema schema) =>
        generatedTypeNames.TryGetValue(schema, out var typeName) ? typeName : null;

    public string Resolve(ApiSchema schema, bool isNullable, string? typeNameHint)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        if (Settings.GenerateOptionalPropertiesAsNullable && schema is ApiSchemaProperty { IsRequired: false })
            isNullable = true;

        schema = GetResolvableSchema(schema);
        if (schema == ExceptionSchema)
            return "System.Exception";

        var markAsNullableReference = Settings.GenerateNullableReferenceTypes && isNullable;

        if (schema.ActualTypeSchema.IsAnyType &&
            schema.ActualDiscriminator == null &&
            schema.InheritedSchema == null &&
            schema.AllOf.Count == 0 &&
            !generatedTypeNames.ContainsKey(schema) &&
            !schema.HasReference)
        {
            return markAsNullableReference ? Settings.AnyType + "?" : Settings.AnyType;
        }

        var type = schema.ActualTypeSchema.Type;
        if (type == ApiObjectTypes.None && schema.ActualTypeSchema.IsEnumeration)
        {
            type = schema.ActualTypeSchema.Enumeration.All(v => v is int)
                ? ApiObjectTypes.Integer
                : ApiObjectTypes.String;
        }

        if (type.IsNumber())
            return ResolveNumber(schema.ActualTypeSchema, isNullable);

        if (type.IsInteger() && !schema.ActualTypeSchema.IsEnumeration)
            return ResolveInteger(schema.ActualTypeSchema, isNullable);

        if (type.IsBoolean())
            return isNullable ? "bool?" : "bool";

        var nullableReferenceSuffix = markAsNullableReference ? "?" : string.Empty;
        if (schema.IsBinary)
            return "byte[]" + nullableReferenceSuffix;

        if (type.IsString() && !schema.ActualTypeSchema.IsEnumeration)
            return ResolveString(schema.ActualTypeSchema, isNullable);

        if (schema.Type.IsArray())
            return ResolveArrayOrTuple(schema) + nullableReferenceSuffix;

        if (schema.IsDictionary)
            return ResolveDictionary(schema) + nullableReferenceSuffix;

        if (schema.ActualTypeSchema.IsEnumeration)
            return GetOrGenerateTypeName(schema, typeNameHint) + (isNullable ? "?" : string.Empty);

        return GetOrGenerateTypeName(schema, typeNameHint) + nullableReferenceSuffix;
    }

    public string GetOrGenerateTypeName(ApiSchema schema, string? typeNameHint)
    {
        schema = RemoveNullability(schema).ActualSchema;
        RegisterSchemaDefinitions(schema.Definitions);

        if (!generatedTypeNames.TryGetValue(schema, out var typeName))
        {
            typeName = Settings.TypeNameGenerator.Generate(schema, typeNameHint, reservedTypeNames);
            generatedTypeNames[schema] = typeName;
            registrationOrder.Add(new KeyValuePair<ApiSchema, string>(schema, typeName));
            reservedTypeNames.Add(typeName);
        }

        return typeName;
    }

    public void RegisterSchemaDefinitions(IDictionary<string, ApiSchema>? definitions)
    {
        if (definitions == null)
            return;

        foreach (var definition in definitions.ToList())
        {
            var actualSchema = definition.Value.ActualSchema;
            if (IsDefinitionTypeSchema(actualSchema))
            {
                GetOrGenerateTypeName(actualSchema, definition.Key);
            }
        }
    }

    public static ApiSchema RemoveNullability(ApiSchema schema) =>
        schema.OneOf.FirstOrDefault(o => !o.IsNullable(ApiSchemaType.JsonSchema)) ?? schema;

    public ApiSchema GetResolvableSchema(ApiSchema schema)
    {
        schema = RemoveNullability(schema);
        return IsDefinitionTypeSchema(schema.ActualSchema) ? schema : schema.ActualSchema;
    }

    private bool IsDefinitionTypeSchema(ApiSchema schema)
    {
        if ((schema.IsDictionary && !Settings.InlineNamedDictionaries) ||
            (schema.IsArray && !Settings.InlineNamedArrays) ||
            (schema.IsTuple && !Settings.InlineNamedTuples))
        {
            return true;
        }

        if (schema.IsAnyType && Settings.InlineNamedAny)
            return false;

        if (!schema.IsTuple && !schema.IsDictionary && !schema.IsArray)
        {
            if (!schema.IsEnumeration && schema.Type != ApiObjectTypes.None)
                return schema.Type.IsObject();

            return true;
        }

        return false;
    }

    private string ResolveString(ApiSchema schema, bool isNullable)
    {
        var suffix = Settings.GenerateNullableReferenceTypes && isNullable ? "?" : string.Empty;
        switch (schema.Format)
        {
            case "date":
                return !isNullable || Settings.DateType?.ToLowerInvariant() == "string"
                    ? Settings.DateType + suffix
                    : Settings.DateType + "?";
            case "date-time":
                return !isNullable || Settings.DateTimeType?.ToLowerInvariant() == "string"
                    ? Settings.DateTimeType + suffix
                    : Settings.DateTimeType + "?";
            case "time":
                return !isNullable || Settings.TimeType?.ToLowerInvariant() == "string"
                    ? Settings.TimeType + suffix
                    : Settings.TimeType + "?";
            case "duration":
            case "time-span":
                return !isNullable || Settings.TimeSpanType?.ToLowerInvariant() == "string"
                    ? Settings.TimeSpanType + suffix
                    : Settings.TimeSpanType + "?";
            case "uri":
                return "System.Uri" + suffix;
            case "guid":
            case "uuid":
                return isNullable ? "System.Guid?" : "System.Guid";
            case "base64":
            case "byte":
                return "byte[]" + suffix;
            default:
                return "string" + suffix;
        }
    }

    private static string ResolveInteger(ApiSchema schema, bool isNullable)
    {
        switch (schema.Format)
        {
            case "byte":
                return isNullable ? "byte?" : "byte";
            case "int64":
            case "long":
                return isNullable ? "long?" : "long";
            case "uint64":
            case "ulong":
                return isNullable ? "ulong?" : "ulong";
        }

        if (string.IsNullOrEmpty(schema.Format) &&
            schema.Type == ApiObjectTypes.Integer &&
            (schema.Minimum < int.MinValue || schema.Minimum > int.MaxValue ||
             schema.Maximum < int.MinValue || schema.Maximum > int.MaxValue))
        {
            return isNullable ? "long?" : "long";
        }

        return isNullable ? "int?" : "int";
    }

    private string ResolveNumber(ApiSchema schema, bool isNullable)
    {
        var type = schema.Format switch
        {
            "decimal" => Settings.NumberDecimalType,
            "double" => Settings.NumberDoubleType,
            "float" => Settings.NumberFloatType,
            _ => Settings.NumberType,
        };

        if (string.IsNullOrWhiteSpace(type))
            type = "double";

        return isNullable ? type + "?" : type;
    }

    private string ResolveArrayOrTuple(ApiSchema schema)
    {
        if (schema.Item != null)
        {
            var typeNameHint = (schema as ApiSchemaProperty)?.Name;
            var itemType = Resolve(schema.Item, schema.Item.IsNullable(Settings.SchemaType), typeNameHint);
            return Settings.ArrayType + "<" + itemType + ">";
        }

        if (schema.Items.Count > 0)
        {
            var itemTypes = schema.Items.Select(i => Resolve(i, i.IsNullable(Settings.SchemaType), null)).ToArray();
            return "System.Tuple<" + string.Join(", ", itemTypes) + ">";
        }

        return Settings.ArrayType + "<object>";
    }

    private string ResolveDictionary(ApiSchema schema)
    {
        var valueType = ResolveDictionaryValueType(schema, "object");
        var keyType = ResolveDictionaryKeyType(schema, "string");
        return Settings.DictionaryType + "<" + keyType + ", " + valueType + ">";
    }

    private string ResolveDictionaryValueType(ApiSchema schema, string fallbackType)
    {
        if (schema.AdditionalPropertiesSchema != null)
        {
            return Resolve(
                schema.AdditionalPropertiesSchema,
                schema.AdditionalPropertiesSchema.ActualSchema.IsNullable(Settings.SchemaType),
                null);
        }

        if (!schema.AllowAdditionalProperties && schema.PatternProperties.Count > 0)
        {
            var types = new HashSet<string>(
                schema.PatternProperties.Select(p => Resolve(p.Value, p.Value.IsNullable(Settings.SchemaType), null)));
            if (types.Count == 1)
                return types.First();
        }

        return fallbackType;
    }

    private string ResolveDictionaryKeyType(ApiSchema schema, string fallbackType) =>
        schema.DictionaryKey != null
            ? Resolve(schema.DictionaryKey, schema.DictionaryKey.ActualSchema.IsNullable(Settings.SchemaType), null)
            : fallbackType;
}

/// <summary>
/// Names types like <see cref="SchemaTypeNameGenerator"/>, but keeps the exact names of the document's schemas
/// for the schemas themselves: a hint that only normalizes to another schema's exact name gets a numeric suffix.
/// </summary>
internal sealed class SafeContractTypeNameGenerator(HashSet<string> preferredExactTypeNameHints) : ISchemaTypeNameGenerator
{
    private const string AnonymousTypeName = "Anonymous";
    private readonly SchemaTypeNameGenerator inner = new();

    public string Generate(ApiSchema schema, string? typeNameHint, IEnumerable<string> reservedTypeNames)
    {
        var normalizedHint = IdentifierUtils.NormalizeSchemaTypeNameHint(typeNameHint)
                          ?? IdentifierUtils.NormalizeSchemaTypeNameHint(schema.Title)
                          ?? AnonymousTypeName;

        var typeNames = reservedTypeNames as ICollection<string> ?? reservedTypeNames.ToList();
        if (!string.IsNullOrEmpty(typeNameHint) &&
            !string.Equals(typeNameHint, normalizedHint, StringComparison.Ordinal) &&
            preferredExactTypeNameHints.Contains(normalizedHint))
        {
            var reservedHintSet = new HashSet<string>(typeNames.Concat(preferredExactTypeNameHints), StringComparer.Ordinal);
            normalizedHint = IdentifierUtils.Counted(reservedHintSet, normalizedHint);
        }

        var generatedTypeName = inner.Generate(schema, normalizedHint, typeNames);
        return string.IsNullOrWhiteSpace(generatedTypeName)
            ? inner.Generate(schema, AnonymousTypeName, typeNames)
            : generatedTypeName;
    }
}
