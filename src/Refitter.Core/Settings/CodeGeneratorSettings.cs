using System.ComponentModel;
using System.Text.Json.Serialization;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

/// <summary>
/// CSharp code generator settings
/// </summary>
[Description("CSharp code generator settings")]
public class CodeGeneratorSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether a required property must be defined in JSON
    /// (sets Required.Always when the property is required) (default: true).
    /// </summary>
    [Description(
        """
        Gets or sets a value indicating whether a required property must be defined in JSON
        (sets Required.Always when the property is required) (default: true).
        """
    )]
    public bool RequiredPropertiesMustBeDefined { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generated data annotation attributes (default: true).
    /// </summary>
    [Description("Gets or sets a value indicating whether to generated data annotation attributes (default: true).")]
    public bool GenerateDataAnnotations { get; set; } = true;

    /// <summary>
    /// Gets or sets the any type (default: "object").
    /// </summary>
    [Description("Gets or sets the any type (default: 'object').")]
    public string AnyType { get; set; } = "object";

    /// <summary>
    /// Gets or sets the date .NET type (default: 'DateTimeOffset').
    /// </summary>
    [Description("Gets or sets the date .NET type (default: 'DateTimeOffset').")]
    public string DateType { get; set; } = "System.DateTimeOffset";

    /// <summary>
    /// Gets or sets the date time .NET type (default: 'DateTimeOffset').
    /// </summary>
    [Description("Gets or sets the date time .NET type (default: 'DateTimeOffset').")]
    public string DateTimeType { get; set; } = "System.DateTimeOffset";

    /// <summary>
    /// Gets or sets the time .NET type (default: 'TimeSpan').
    /// </summary>
    [Description("Gets or sets the time .NET type (default: 'TimeSpan').")]
    public string TimeType { get; set; } = "System.TimeSpan";

    /// <summary>
    /// Gets or sets the time span .NET type (default: 'TimeSpan').
    /// </summary>
    [Description("Gets or sets the time span .NET type (default: 'TimeSpan').")]
    public string TimeSpanType { get; set; } = "System.TimeSpan";

    /// <summary>
    /// Gets or sets the .NET type for OpenAPI integers without a format specifier (default: Int32).
    /// </summary>
    [Description("Gets or sets the .NET type for OpenAPI integers without a format specifier (default: Int32).")]
    public IntegerType IntegerType { get; set; } = IntegerType.Int32;

    /// <summary>
    /// Gets or sets the generic array .NET type (default: 'ICollection').
    /// </summary>
    [Description("Gets or sets the generic array .NET type (default: 'ICollection').")]
    public string ArrayType { get; set; } = "System.Collections.Generic.ICollection";

    /// <summary>
    /// Gets or sets the generic dictionary .NET type (default: 'IDictionary').
    /// </summary>
    [Description("Gets or sets the generic dictionary .NET type (default: 'IDictionary').")]
    public string DictionaryType { get; set; } = "System.Collections.Generic.IDictionary";

    /// <summary>
    /// Gets or sets the generic array .NET type which is used for ArrayType instances (default: 'Collection').
    /// </summary>
    [Description(
        "Gets or sets the generic array .NET type which is used for ArrayType instances (default: 'Collection')."
    )]
    public string ArrayInstanceType { get; set; } = "System.Collections.ObjectModel.Collection";

    /// <summary>
    /// Gets or sets the generic dictionary .NET type which is used for DictionaryType instances (default: 'Dictionary').
    /// </summary>
    [Description(
        """
        Gets or sets the generic dictionary .NET type which is used for DictionaryType instances (default: 'Dictionary').
        """
    )]
    public string DictionaryInstanceType { get; set; } = "System.Collections.Generic.Dictionary";

    /// <summary>
    /// Gets or sets the generic array .NET type which is used as base class (default: 'Collection').
    /// </summary>
    [Description("Gets or sets the generic array .NET type which is used as base class (default: 'Collection').")]
    public string ArrayBaseType { get; set; } = "System.Collections.ObjectModel.Collection";

    /// <summary>
    /// Gets or sets the generic dictionary .NET type which is used as base class (default: 'Dictionary').
    /// </summary>
    [Description("Gets or sets the generic dictionary .NET type which is used as base class (default: 'Dictionary').")]
    public string DictionaryBaseType { get; set; } = "System.Collections.Generic.Dictionary";

    /// <summary>
    /// Gets the access modifier of property setters (default: '').
    /// </summary>
    [Description("Gets the access modifier of property setters (default: '').")]
    public string PropertySetterAccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom Json.NET converters (class names) which are registered for serialization and deserialization.
    /// </summary>
    [Description(
        """
        Gets or sets the custom Json.NET converters (class names)
        which are registered for serialization and deserialization.
        """
    )]
    public string[]? JsonConverters { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove the setter for non-nullable array properties (default: false).
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether to remove the setter for non-nullable array properties (default: false)."
    )]
    public bool GenerateImmutableArrayProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove the setter for non-nullable dictionary properties (default: false).
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether to remove the setter for non-nullable dictionary properties (default: false)."
    )]
    public bool GenerateImmutableDictionaryProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use preserve references handling (All) in the JSON serializer (default: false).
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether to use preserve references handling (All) in the JSON serializer (default: false)."
    )]
    public bool HandleReferences { get; set; }

    /// <summary>
    /// Gets or sets the name of a static method which is called to transform the JsonSerializerSettings (for Newtonsoft.Json) or the JsonSerializerOptions (for System.Text.Json) used in the generated ToJson()/FromJson() methods (default: null).
    /// </summary>
    [Description(
        "Gets or sets the name of a static method which is called to transform the JsonSerializerSettings (for Newtonsoft.Json) or the JsonSerializerOptions (for System.Text.Json) used in the generated ToJson()/FromJson() methods (default: null)."
    )]
    public string? JsonSerializerSettingsTransformationMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to render ToJson() and FromJson() methods (default: false).
    /// </summary>
    [Description("Gets or sets a value indicating whether to render ToJson() and FromJson() methods (default: false).")]
    public bool GenerateJsonMethods { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether enums should be always generated as bit flags (default: false).
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether enums should be always generated as bit flags (default: false)."
    )]
    public bool EnforceFlagEnums { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced dictionaries should be inlined or generated as class with dictionary inheritance.
    /// </summary>
    [Description(
        """
        Gets or sets a value indicating whether named/referenced dictionaries should be inlined or generated as class with dictionary inheritance.
        """
    )]
    public bool InlineNamedDictionaries { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced tuples should be inlined or generated as class with tuple inheritance.
    /// </summary>
    [Description(
        """
        Gets or sets a value indicating whether named/referenced tuples should be inlined or generated as class with tuple inheritance.
        """
    )]
    public bool InlineNamedTuples { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced arrays should be inlined or generated as class with array inheritance.
    /// </summary>
    [Description(
        """
        Gets or sets a value indicating whether named/referenced arrays should be inlined or generated as class with array inheritance.
        """
    )]
    public bool InlineNamedArrays { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether optional schema properties (not required) are generated as nullable properties (default: false).
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether optional schema properties (not required) are generated as nullable properties (default: false)."
    )]
    public bool GenerateOptionalPropertiesAsNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate Nullable Reference Type annotations (default: false).
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether to generate Nullable Reference Type annotations (default: false)."
    )]
    public bool GenerateNullableReferenceTypes { get; set; }

    /// <summary>
    /// Generate C# 9.0 record types instead of record-like classes.
    /// </summary>
    [Description("Generate C# 9.0 record types instead of record-like classes.")]
    public bool GenerateNativeRecords { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate default values for properties (when JSON Schema default is set, default: true).
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether to generate default values for properties (when JSON Schema default is set, default: true)."
    )]
    public bool GenerateDefaultValues { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced any schemas should be inlined or generated as class.
    /// </summary>
    [Description(
        """
        Gets or sets a value indicating whether named/referenced any schemas should be inlined or generated as class.
        """
    )]
    public bool InlineNamedAny { get; set; }

    /// <summary>
    /// Gets or sets the excluded type names (must be defined in an import or other namespace).
    /// </summary>
    [Description("Gets or sets the excluded type names (must be defined in an import or other namespace).")]
    public string[] ExcludedTypeNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the date string format to use
    /// </summary>
    [Description("Gets or sets the date string format to use")]
    public string? DateFormat { get; set; }

    /// <summary>
    /// Gets or sets the date-time string format to use
    /// </summary>
    [Description("Gets or sets the date-time string format to use")]
    public string? DateTimeFormat { get; set; }

    /// <summary>
    /// Gets or sets a custom <see cref="IPropertyNameGenerator"/>.
    /// </summary>
    [Description("Gets or sets a custom IPropertyNameGenerator.")]
    [JsonIgnore]
    public IPropertyNameGenerator? PropertyNameGenerator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to inline JsonConverter attributes for enum properties (default: true).
    /// When set to false, enum properties will not have [JsonConverter(typeof(JsonStringEnumConverter))] attributes.
    /// </summary>
    [Description(
        "Gets or sets a value indicating whether to inline JsonConverter attributes for enum properties (default: true). When set to false, enum properties will not have [JsonConverter(typeof(JsonStringEnumConverter))] attributes."
    )]
    public bool InlineJsonConverters { get; set; } = true;

    /// <summary>
    /// Gets or sets a directory path which contains liquid templates for NSwag. If null or empty, uses default
    /// templates.
    /// </summary>
    [Description("Custom directory with NSwag fluid templates for code generation. Default is null which uses the default NSwag templates. See https://github.com/RicoSuter/NSwag/wiki/Templates")]
    public string? CustomTemplateDirectory { get; set; }
}
