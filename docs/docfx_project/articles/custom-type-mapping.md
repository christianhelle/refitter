# Custom Type Mapping

Refitter supports custom type mapping for OpenAPI schema formats through the `typeOverrides` setting in the `codeGeneratorSettings` section of your `.refitter` configuration file. This powerful feature allows you to map custom OpenAPI format strings to your own domain-specific .NET types.

## Overview

When working with OpenAPI specifications, you may encounter custom format strings that don't map to standard .NET types. For example, an API might use `format: "my-custom-date"` for a proprietary date representation, or `format: "uuid-base64"` for a custom identifier format. The custom type mapping feature lets you specify exactly which .NET type should be used for these custom formats.

## When to Use Custom Type Mapping

Custom type mapping is useful when:

- Your API uses domain-specific data types with custom format strings
- You have existing .NET types that represent API data structures
- You want to avoid manual editing of generated code
- You need to maintain consistency across multiple API clients
- Your OpenAPI specification uses vendor-specific format extensions

## Configuration

### Basic Syntax

Custom type mappings are defined in the `codeGeneratorSettings.typeOverrides` array within your `.refitter` file:

```json
{
  "openApiPath": "./openapi.json",
  "namespace": "MyApi.Client",
  "codeGeneratorSettings": {
    "typeOverrides": [
      {
        "formatPattern": "string:my-custom-date",
        "typeName": "MyDomain.CustomDateType"
      }
    ]
  }
}
```

### Format Pattern Syntax

The `formatPattern` follows the structure: `{type}:{format}`

- **type**: The OpenAPI schema type (e.g., `string`, `integer`, `number`)
- **format**: The custom format string from your OpenAPI specification

Examples:
- `"string:my-custom-date"` - matches a string with format "my-custom-date"
- `"string:uuid-base64"` - matches a string with format "uuid-base64"  
- `"string:my-custom-datetime"` - matches a string with format "my-custom-datetime"
- `"integer:timestamp-millis"` - matches an integer with format "timestamp-millis"

### Type Name

The `typeName` should be the fully qualified .NET type name, including the namespace:

```json
"typeName": "MyCompany.MyProject.Domain.CustomDate"
```

If your custom type is in the same namespace as the generated code, you can omit the namespace:

```json
"typeName": "CustomDate"
```

## Complete Example

Let's walk through a complete example of using custom type mapping.

### OpenAPI Specification

Suppose you have an OpenAPI specification that uses custom date formats:

```yaml
openapi: 3.0.0
info:
  title: Custom Date API
  version: 1.0.0
paths:
  /appointments:
    get:
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Appointment'
components:
  schemas:
    Appointment:
      type: object
      properties:
        id:
          type: string
          format: uuid
        scheduledDate:
          type: string
          format: custom-date
          description: Date in proprietary format YYYYMMDD
        createdAt:
          type: string
          format: custom-timestamp
          description: Timestamp in proprietary format
        notes:
          type: string
```

### Custom Type Definitions

First, define your custom types:

```csharp
namespace MyCompany.Types
{
    /// <summary>
    /// Represents a date in YYYYMMDD format
    /// </summary>
    public class CustomDate
    {
        private readonly int _value;

        public CustomDate(int yyyymmdd)
        {
            _value = yyyymmdd;
        }

        public int Year => _value / 10000;
        public int Month => (_value / 100) % 100;
        public int Day => _value % 100;

        public DateTime ToDateTime() => new DateTime(Year, Month, Day);

        public static implicit operator string(CustomDate date) 
            => date._value.ToString();

        public static implicit operator CustomDate(string value) 
            => new CustomDate(int.Parse(value));

        public override string ToString() => _value.ToString();
    }

    /// <summary>
    /// Represents a custom timestamp
    /// </summary>
    public class CustomTimestamp
    {
        private readonly long _value;

        public CustomTimestamp(long unixMillis)
        {
            _value = unixMillis;
        }

        public DateTime ToDateTime() 
            => DateTimeOffset.FromUnixTimeMilliseconds(_value).DateTime;

        public static implicit operator string(CustomTimestamp timestamp) 
            => timestamp._value.ToString();

        public static implicit operator CustomTimestamp(string value) 
            => new CustomTimestamp(long.Parse(value));

        public override string ToString() => _value.ToString();
    }
}
```

### .refitter Configuration

Configure Refitter to use your custom types:

```json
{
  "openApiPath": "./openapi.json",
  "namespace": "MyCompany.Api.Client",
  "generateContracts": true,
  "codeGeneratorSettings": {
    "typeOverrides": [
      {
        "formatPattern": "string:custom-date",
        "typeName": "MyCompany.Types.CustomDate"
      },
      {
        "formatPattern": "string:custom-timestamp",
        "typeName": "MyCompany.Types.CustomTimestamp"
      }
    ]
  },
  "additionalNamespaces": [
    "MyCompany.Types"
  ]
}
```

### Generated Code

With the custom type mapping configured, Refitter will generate code like this:

```csharp
using Refit;
using System.Threading.Tasks;
using MyCompany.Types;

namespace MyCompany.Api.Client
{
    public partial interface ICustomDateApi
    {
        [Get("/appointments")]
        Task<ICollection<Appointment>> GetAppointments();
    }

    public class Appointment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("scheduledDate")]
        public CustomDate ScheduledDate { get; set; }

        [JsonPropertyName("createdAt")]
        public CustomTimestamp CreatedAt { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }
}
```

## Multiple Mappings

You can define multiple type overrides in the same configuration:

```json
{
  "codeGeneratorSettings": {
    "typeOverrides": [
      {
        "formatPattern": "string:custom-date",
        "typeName": "MyCompany.Types.CustomDate"
      },
      {
        "formatPattern": "string:custom-timestamp",
        "typeName": "MyCompany.Types.CustomTimestamp"
      },
      {
        "formatPattern": "string:custom-id",
        "typeName": "MyCompany.Types.CustomId"
      },
      {
        "formatPattern": "integer:epoch-seconds",
        "typeName": "MyCompany.Types.EpochTime"
      },
      {
        "formatPattern": "string:base64-uuid",
        "typeName": "MyCompany.Types.Base64Uuid"
      }
    ]
  }
}
```

## Best Practices

### 1. Implement Type Converters

Ensure your custom types have appropriate conversion operators for seamless integration with JSON serialization:

```csharp
public static implicit operator string(CustomType value) => value.ToString();
public static implicit operator CustomType(string value) => Parse(value);
```

### 2. Use Fully Qualified Type Names

Always use fully qualified type names to avoid ambiguity:

```json
"typeName": "MyCompany.MyProject.Types.CustomDate"
```

### 3. Add Required Namespaces

Include custom type namespaces in the `additionalNamespaces` setting:

```json
{
  "additionalNamespaces": [
    "MyCompany.Types",
    "MyCompany.CustomTypes"
  ],
  "codeGeneratorSettings": {
    "typeOverrides": [...]
  }
}
```

### 4. Document Custom Formats

Document your custom formats in the OpenAPI specification using the `description` field:

```yaml
scheduledDate:
  type: string
  format: custom-date
  description: Date in YYYYMMDD format (e.g., 20231215)
```

### 5. Maintain Type Consistency

Ensure the same custom format is mapped to the same type across all your API clients for consistency.

## JSON Serialization

Your custom types must work with the JSON serializer used by Refit. Here are examples for both System.Text.Json and Newtonsoft.Json:

### System.Text.Json

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

public class CustomDateConverter : JsonConverter<CustomDate>
{
    public override CustomDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new CustomDate(int.Parse(reader.GetString()));
    }

    public override void Write(Utf8JsonWriter writer, CustomDate value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

// Apply to your type
[JsonConverter(typeof(CustomDateConverter))]
public class CustomDate
{
    // ... implementation
}
```

### Newtonsoft.Json

```csharp
using Newtonsoft.Json;

public class CustomDateConverter : JsonConverter<CustomDate>
{
    public override CustomDate ReadJson(JsonReader reader, Type objectType, CustomDate existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new CustomDate(int.Parse((string)reader.Value));
    }

    public override void WriteJson(JsonWriter writer, CustomDate value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}

// Apply to your type
[JsonConverter(typeof(CustomDateConverter))]
public class CustomDate
{
    // ... implementation
}
```

## Troubleshooting

### Custom Type Not Applied

**Problem**: The generated code still uses `string` instead of your custom type.

**Solutions**:
- Verify the `formatPattern` exactly matches the OpenAPI specification format
- Ensure the format pattern includes the type prefix (e.g., `string:custom-date`, not just `custom-date`)
- Check that the OpenAPI schema actually uses the custom format on the property

### Compilation Errors

**Problem**: Generated code doesn't compile due to missing types.

**Solutions**:
- Add the namespace containing your custom type to `additionalNamespaces`
- Verify the `typeName` is fully qualified
- Ensure your custom type is accessible from the generated code project

### Runtime Serialization Errors

**Problem**: JSON serialization/deserialization fails at runtime.

**Solutions**:
- Implement appropriate `JsonConverter` for your custom type
- Verify implicit conversion operators are defined
- Test serialization round-trip before using in production

## CLI Tool Usage

When using the CLI tool, you cannot specify custom type mappings directly via command-line arguments. You must use a `.refitter` settings file:

```bash
refitter ./openapi.json --settings-file ./openapi.refitter
```

Where `openapi.refitter` contains your type override configuration.

## Source Generator and MSBuild

Custom type mappings work seamlessly with both the Source Generator and MSBuild integration. Simply define the `typeOverrides` in your `.refitter` file, and the mappings will be applied during code generation.

## See Also

- [.refitter File Format](refitter-file-format.md)
- [Code Generator Settings](refitter-file-format.md#code-generator-settings)
- [Using the Generated Code](using-the-generated-code.md)
- [Examples](examples.md)
