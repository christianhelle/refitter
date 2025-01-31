using System.Text.Json.Serialization;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

/// <summary>
/// CSharp code generator settings
/// </summary>
public class CodeGeneratorSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether a required property must be defined in JSON
    /// (sets Required.Always when the property is required) (default: true).
    /// </summary>
    public bool RequiredPropertiesMustBeDefined { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generated data annotation attributes (default: true).
    /// </summary>
    public bool GenerateDataAnnotations { get; set; } = true;

    /// <summary>
    /// Gets or sets the any type (default: "object").
    /// </summary>
    public string AnyType { get; set; } = "object";

    /// <summary>
    /// Gets or sets the date .NET type (default: 'DateTimeOffset').
    /// </summary>
    public string DateType { get; set; } = "System.DateTimeOffset";

    /// <summary>
    /// Gets or sets the date time .NET type (default: 'DateTimeOffset').
    /// </summary>
    public string DateTimeType { get; set; } = "System.DateTimeOffset";

    /// <summary>
    /// Gets or sets the time .NET type (default: 'TimeSpan').
    /// </summary>
    public string TimeType { get; set; } = "System.TimeSpan";

    /// <summary>
    /// Gets or sets the time span .NET type (default: 'TimeSpan').
    /// </summary>
    public string TimeSpanType { get; set; } = "System.TimeSpan";

    /// <summary>
    /// Gets or sets the generic array .NET type (default: 'ICollection').
    /// </summary>
    public string ArrayType { get; set; } = "System.Collections.Generic.ICollection";

    /// <summary>
    /// Gets or sets the generic dictionary .NET type (default: 'IDictionary').
    /// </summary>
    public string DictionaryType { get; set; } = "System.Collections.Generic.IDictionary";

    /// <summary>
    /// Gets or sets the generic array .NET type which is used for ArrayType instances (default: 'Collection').
    /// </summary>
    public string ArrayInstanceType { get; set; } = "System.Collections.ObjectModel.Collection";

    /// <summary>
    /// Gets or sets the generic dictionary .NET type which is used for DictionaryType instances (default: 'Dictionary').
    /// </summary>
    public string DictionaryInstanceType { get; set; } = "System.Collections.Generic.Dictionary";

    /// <summary>
    /// Gets or sets the generic array .NET type which is used as base class (default: 'Collection').
    /// </summary>
    public string ArrayBaseType { get; set; } = "System.Collections.ObjectModel.Collection";

    /// <summary>
    /// Gets or sets the generic dictionary .NET type which is used as base class (default: 'Dictionary').
    /// </summary>
    public string DictionaryBaseType { get; set; } = "System.Collections.Generic.Dictionary";

    /// <summary>
    /// Gets the access modifier of property setters (default: '').
    /// </summary>
    public string PropertySetterAccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom Json.NET converters (class names) which are registered for serialization and deserialization.
    /// </summary>
    public string[]? JsonConverters { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove the setter for non-nullable array properties (default: false).
    /// </summary>
    public bool GenerateImmutableArrayProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove the setter for non-nullable dictionary properties (default: false).
    /// </summary>
    public bool GenerateImmutableDictionaryProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use preserve references handling (All) in the JSON serializer (default: false).
    /// </summary>
    public bool HandleReferences { get; set; }

    /// <summary>
    /// Gets or sets the name of a static method which is called to transform the JsonSerializerSettings (for Newtonsoft.Json) or the JsonSerializerOptions (for System.Text.Json) used in the generated ToJson()/FromJson() methods (default: null).
    /// </summary>
    public string? JsonSerializerSettingsTransformationMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to render ToJson() and FromJson() methods (default: false).
    /// </summary>
    public bool GenerateJsonMethods { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether enums should be always generated as bit flags (default: false).
    /// </summary>
    public bool EnforceFlagEnums { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced dictionaries should be inlined or generated as class with dictionary inheritance.
    /// </summary>
    public bool InlineNamedDictionaries { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced tuples should be inlined or generated as class with tuple inheritance.
    /// </summary>
    public bool InlineNamedTuples { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced arrays should be inlined or generated as class with array inheritance.
    /// </summary>
    public bool InlineNamedArrays { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether optional schema properties (not required) are generated as nullable properties (default: false).
    /// </summary>
    public bool GenerateOptionalPropertiesAsNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate Nullable Reference Type annotations (default: false).
    /// </summary>
    public bool GenerateNullableReferenceTypes { get; set; }

    /// <summary>
    /// Generate C# 9.0 record types instead of record-like classes.
    /// </summary>
    public bool GenerateNativeRecords { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate default values for properties (when JSON Schema default is set, default: true).
    /// </summary>
    public bool GenerateDefaultValues { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether named/referenced any schemas should be inlined or generated as class.
    /// </summary>
    public bool InlineNamedAny { get; set; }

    /// <summary>
    /// Gets or sets the excluded type names (must be defined in an import or other namespace).
    /// </summary>
    public string[] ExcludedTypeNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the date string format to use
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Gets or sets the date-time string format to use
    /// </summary>
    public string? DateTimeFormat { get; set; }

    /// <summary>
    /// Gets or sets a custom <see cref="IPropertyNameGenerator"/>.
    /// </summary>
    [JsonIgnore]
    public IPropertyNameGenerator? PropertyNameGenerator { get; set; }
}
