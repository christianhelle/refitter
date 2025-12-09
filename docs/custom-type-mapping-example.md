# Custom Type Mapping Example

This example demonstrates how to use custom type mappings to override the default type mappings for OpenAPI schemas with custom format specifiers.

## Use Case

When your API uses custom format specifiers (e.g., `format: my-date-time`) that should map to domain-specific types instead of the default .NET types, you can use the `customTypeMapping` setting in the `.refitter` file.

## Example OpenAPI Specification

```json
{
  "openapi": "3.0.1",
  "info": {
    "title": "Custom Types API",
    "version": "1.0.0"
  },
  "paths": {
    "/users": {
      "post": {
        "requestBody": {
          "content": {
            "application/json": {
              "schema": {
                "$ref": "#/components/schemas/User"
              }
            }
          }
        },
        "responses": {
          "200": {
            "description": "Success"
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "User": {
        "type": "object",
        "properties": {
          "id": {
            "type": "integer",
            "format": "custom-id"
          },
          "createdAt": {
            "type": "string",
            "format": "custom-datetime"
          },
          "name": {
            "type": "string"
          }
        }
      }
    }
  }
}
```

## .refitter Configuration

Create a `.refitter` file with custom type mappings:

```json
{
  "openApiPath": "openapi.json",
  "namespace": "MyApi",
  "codeGeneratorSettings": {
    "customTypeMapping": {
      "string:custom-datetime": "MyDomain.CustomDateTime",
      "integer:custom-id": "MyDomain.EntityId"
    }
  }
}
```

## Generated Code (Without Custom Mapping)

Without custom type mapping, Refitter would generate:

```csharp
public partial class User
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }
}
```

## Generated Code (With Custom Mapping)

With custom type mapping, Refitter generates:

```csharp
public partial class User
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public MyDomain.EntityId Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public MyDomain.CustomDateTime CreatedAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }
}
```

## Domain Types

You would define the custom types in your domain:

```csharp
namespace MyDomain
{
    public class CustomDateTime
    {
        public DateTime Value { get; set; }
        
        // Add custom serialization logic here
        public static implicit operator DateTime(CustomDateTime customDateTime) 
            => customDateTime.Value;
        
        public static implicit operator CustomDateTime(DateTime dateTime) 
            => new CustomDateTime { Value = dateTime };
    }

    public class EntityId
    {
        public int Value { get; set; }
        
        public static implicit operator int(EntityId entityId) 
            => entityId.Value;
        
        public static implicit operator EntityId(int value) 
            => new EntityId { Value = value };
    }
}
```

## Key Format

The key format for `customTypeMapping` is `"type:format"` where:

- **type**: The OpenAPI type (e.g., `string`, `integer`, `number`, `boolean`)
- **format**: The format specifier from the OpenAPI schema (e.g., `custom-datetime`, `my-format`)

The value is the fully qualified .NET type name (including namespace).

## Supported OpenAPI Types

- `string` - Maps to string by default
- `integer` - Maps to int by default (or long if format is `int64`)  
- `number` - Maps to double by default (or float if format is `float`)
- `boolean` - Maps to bool by default

## Benefits

1. **Type Safety**: Use domain-specific types instead of primitives
2. **Encapsulation**: Encapsulate custom serialization logic within domain types
3. **Consistency**: Ensure consistent handling of custom formats across your codebase
4. **Maintainability**: Centralize type mapping configuration in one place
