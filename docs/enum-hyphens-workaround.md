# Handling Enum Names with Hyphens

## Problem

OpenAPI specifications sometimes contain enum values with hyphens (e.g., `"foo-bar"`, `"content-type"`, `"application/json"`). When these are converted to C# enum names, the hyphens are typically replaced with underscores or removed to create valid C# identifiers.

However, System.Text.Json's built-in `JsonStringEnumConverter` doesn't respect the `[EnumMember]` attribute that NSwag generates to map the original hyphenated string values back to the enum names. This causes serialization and deserialization issues where the JSON values don't match the expected format.

### Example Issue

Given an OpenAPI enum:
```json
{
  "type": "string",
  "enum": ["foo-bar", "baz-qux"]
}
```

Refitter generates:
```csharp
public enum MyEnum
{
    [EnumMember(Value = "foo-bar")]
    FooBar = 0,
    
    [EnumMember(Value = "baz-qux")]
    BazQux = 1
}
```

By default, Refitter also adds `[JsonConverter(typeof(JsonStringEnumConverter))]` to enum properties. Unfortunately, System.Text.Json's `JsonStringEnumConverter` **ignores** the `[EnumMember]` attribute, causing it to serialize as `"FooBar"` instead of `"foo-bar"`.

## Solution

Use the `--no-inline-json-converters` flag to suppress the inline `JsonStringEnumConverter` attributes, then apply a third-party converter that properly handles `[EnumMember]` attributes.

### Step-by-Step Guide

#### 1. Generate Code Without Inline Converters

Use the `--no-inline-json-converters` flag when running Refitter:

```bash
refitter openapi.json --output Generated.cs --no-inline-json-converters
```

Or in a `.refitter` settings file:

```json
{
  "openApiPath": "openapi.json",
  "namespace": "MyApi",
  "codeGeneratorSettings": {
    "inlineJsonConverters": false
  }
}
```

#### 2. Install a Compatible JsonConverter

Install one of these NuGet packages that properly handle `[EnumMember]` attributes:

**Option A: JsonStringEnumMemberConverter (Recommended)**
```bash
dotnet add package JsonStringEnumMemberConverter
```

**Option B: Macross.Json.Extensions**
```bash
dotnet add package Macross.Json.Extensions
```

#### 3. Configure the Converter

**Option A: Apply Globally via JsonSerializerOptions**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonStringEnumMemberConverter;  // or Macross.Json.Extensions

var options = new JsonSerializerOptions
{
    Converters = { new JsonStringEnumMemberConverter() }  // respects [EnumMember]
};

// Use with HttpClient or Refit
var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(options)
};
```

**Option B: Apply to Specific Enums (Partial Class)**

If you prefer targeted control, use partial classes to add the attribute:

```csharp
using System.Text.Json.Serialization;
using JsonStringEnumMemberConverter;

namespace MyApi
{
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public partial enum MyEnum { }
}
```

#### 4. Use with Refit

Configure your Refit client:

```csharp
var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(
        new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumMemberConverter() }
        })
};

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
var client = RestService.For<IMyApi>(httpClient, refitSettings);
```

## Example

### OpenAPI Specification
```json
{
  "components": {
    "schemas": {
      "ContentType": {
        "type": "string",
        "enum": ["application/json", "application/xml", "text/plain"]
      }
    }
  }
}
```

### Generated Code (with --no-inline-json-converters)
```csharp
public enum ContentType
{
    [EnumMember(Value = "application/json")]
    ApplicationJson = 0,

    [EnumMember(Value = "application/xml")]
    ApplicationXml = 1,

    [EnumMember(Value = "text/plain")]
    TextPlain = 2
}

public class MyRequest
{
    // No [JsonConverter] attribute here
    public ContentType ContentType { get; set; }
}
```

### Usage with JsonStringEnumMemberConverter
```csharp
using System.Text.Json;
using JsonStringEnumMemberConverter;

var options = new JsonSerializerOptions
{
    Converters = { new JsonStringEnumMemberConverter() }
};

var json = """{"contentType": "application/json"}""";
var request = JsonSerializer.Deserialize<MyRequest>(json, options);
// request.ContentType == ContentType.ApplicationJson ✓

var serialized = JsonSerializer.Serialize(request, options);
// serialized == """{"contentType":"application/json"}""" ✓
```

## Why This Works

1. **`--no-inline-json-converters`** prevents Refitter from adding `[JsonConverter(typeof(JsonStringEnumConverter))]` to each enum property
2. **NSwag still generates** `[EnumMember]` attributes on enum values to preserve original string mappings
3. **Third-party converters** like `JsonStringEnumMemberConverter` correctly read and honor `[EnumMember]` attributes
4. **Result**: Enums serialize/deserialize with their original hyphenated values

## Related Resources

- [JsonStringEnumMemberConverter on NuGet](https://www.nuget.org/packages/JsonStringEnumMemberConverter/)
- [Macross.Json.Extensions on NuGet](https://www.nuget.org/packages/Macross.Json.Extensions/)
- [System.Text.Json EnumMember Issue on GitHub](https://github.com/dotnet/runtime/issues/31081)
- [Refitter --no-inline-json-converters documentation](https://refitter.github.io)

## See Also

- [CLI Options Reference](../README.md#--no-inline-json-converters)
- [Settings File Reference](../README.md#settings-file)
