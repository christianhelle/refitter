# Tier 2 Investigation: Multipart/Form Data Issues (#231, #222)

## Issues Summary

### Issue #231: Client parameter type names wrong with multipart FromForm endpoints
- **URL**: https://github.com/christianhelle/refitter/issues/231
- **Reporter**: @safakkesikci
- **Problem**: When generating code from OpenAPI specs with `multipart/form-data`, the parameter types are being incorrectly named or generated
- **OpenAPI Spec**: Contains endpoint with mixed string and binary file properties in multipart form-data

### Issue #222: Multipart endpoint FromForm argument missing from signature
- **URL**: https://github.com/christianhelle/refitter/issues/222
- **Reporter**: @fearef
- **Problem**: Non-file form-data parameters (like `OrganizationalUnitIds: long[]`) are completely missing from generated method signatures in multipart/form-data endpoints
- **Expected**: Method should include `long[] organizationalUnitIds` parameter
- **Actual**: Only `StreamPart file` is generated; the array parameter is omitted

## Root Cause Analysis

### Issue #222 Root Cause (Missing Non-Binary Parameters)

**CRITICAL BUG IDENTIFIED**: Non-binary form-data parameters are being completely dropped from multipart/form-data endpoints.

**Code Location**: `src/Refitter.Core/ParameterExtractor.cs`, lines 71-75

```csharp
var formParameters = operationModel.Parameters
    .Where(p => p.Kind == OpenApiParameterKind.FormData && !p.IsBinaryBodyParameter)
    .Select(p =>
        $"{JoinAttributes(GetAliasAsAttribute(p))}{GetParameterType(p, settings)} {p.VariableName}")
    .ToList();
```

**Problem**: This code correctly attempts to extract non-binary form parameters. However, **NSwag is NOT populating `operationModel.Parameters` with form-data schema properties for OpenAPI 3.x multipart/form-data requests**.

In OpenAPI 3.x, multipart/form-data is defined in `requestBody.content["multipart/form-data"].schema.properties`, NOT as parameters. NSwag's `CSharpOperationModel` does not automatically expand these properties into the Parameters collection for multipart content.

**Evidence from Issue #222 spec**:
```json
"requestBody": {
    "content": {
        "multipart/form-data": {
            "schema": {
                "properties": {
                    "file": { "type": "string", "format": "binary" },
                    "OrganizationalUnitIds": { "type": "array", "items": { "type": "integer", "format": "int64" } }
                }
            }
        }
    }
}
```

NSwag only creates parameters for:
- Binary files → become parameters with `IsBinaryBodyParameter = true`
- Non-binary properties → **MISSING from operationModel.Parameters entirely**

### Issue #231 Root Cause (Wrong Type Names)

**RELATED BUG**: When NSwag DOES create parameters for multipart endpoints, the type names may be incorrect because:

1. **Type resolution through NSwag generator** (line 298-299 in ParameterExtractor.cs):
   ```csharp
   var type = WellKnownNamespaces.TrimImportedNamespaces(
       FindSupportedType(parameterModel.Type));
   ```
   
2. **Binary parameter handling** (lines 77-87): Binary parameters use `StreamPart` correctly, but the logic depends on NSwag's `IsBinaryBodyParameter` flag.

3. **The actual type names depend on how NSwag parsed the schema**, and there may be issues with:
   - Property name casing (PascalCase vs camelCase)
   - Type inference from format specifiers
   - Generated contract type names not matching expected names

## Code Locations

### Primary Problem Area
- **File**: `src/Refitter.Core/ParameterExtractor.cs`
- **Lines**: 71-75 (formParameters extraction)
- **Lines**: 77-87 (binaryBodyParameters extraction)
- **Problem**: Relies entirely on NSwag's `operationModel.Parameters` collection, which doesn't include non-binary multipart form properties for OpenAPI 3.x

### Missing Logic
- **File**: `src/Refitter.Core/ParameterExtractor.cs`
- **Need**: New logic to manually parse `operation.RequestBody.Content["multipart/form-data"].Schema.Properties` when detected
- **Should create**: Parameters for each property in the schema that isn't binary (`format != "binary"`)

### Related Code
- **File**: `src/Refitter.Core/RefitInterfaceGenerator.cs`
- **Lines**: 236-242 (GenerateForMultipartFormData method) - Only adds `[Multipart]` attribute, doesn't handle parameters
- **Lines**: 64-66 - Where ParameterExtractor is called

## Recommended Fix Approach

### For Issue #222 (Missing Parameters) - HIGH PRIORITY

1. **Detect multipart/form-data operations**:
   - Check if `operation.RequestBody?.Content.ContainsKey("multipart/form-data")` is true

2. **Manually extract schema properties**:
   - Access `operation.RequestBody.Content["multipart/form-data"].Schema`
   - Iterate through `schema.ActualSchema.Properties`
   - For each property:
     - If `format == "binary"` → Already handled as `StreamPart` by existing logic
     - If NOT binary → Create form parameter with proper type, name, and `[AliasAs]` attribute if needed

3. **Generate proper attributes**:
   - Non-binary form parameters should NOT have `[Body]` attribute
   - They should have `[AliasAs("PropertyName")]` if the C# variable name differs from JSON property name
   - Consider if they need explicit form-data attributes (Refit may handle this implicitly with `[Multipart]`)

4. **Implementation location**: 
   - Add new method `GetMultipartFormDataParameters()` in `ParameterExtractor.cs`
   - Call it when `operationModel.Consumes.Contains("multipart/form-data")` is detected
   - Merge results with existing `binaryBodyParameters`

### For Issue #231 (Wrong Type Names) - MEDIUM PRIORITY

1. **Verify NSwag schema parsing**:
   - Check if `CSharpClientGeneratorFactory.Create()` is properly configuring type name generation
   - Review `CustomCSharpPropertyNameGenerator` usage

2. **Type name consistency**:
   - Ensure generated contract types match parameter type references
   - May need to use `generator.GetTypeName()` for non-binary form parameters (similar to how body parameters work)

3. **Property name mapping**:
   - Ensure `GetAliasAsAttribute()` correctly maps between OpenAPI property names and C# variable names
   - Apply proper casing (camelCase for variables, PascalCase for properties in schema)

### Combined Fix Strategy

```csharp
// Pseudocode for the fix
if (operationModel.Consumes.Contains("multipart/form-data") && 
    operation.RequestBody?.Content.TryGetValue("multipart/form-data", out var multipartContent) == true)
{
    var schema = multipartContent.Schema?.ActualSchema;
    if (schema?.Properties != null)
    {
        foreach (var property in schema.Properties)
        {
            var isBinary = property.Value.Format == "binary";
            if (isBinary)
            {
                // Already handled by existing binaryBodyParameters logic
                continue;
            }
            
            // Generate type name using NSwag generator
            var typeName = generator.GetTypeName(property.Value, nullable: !property.Value.IsRequired, null);
            var variableName = ConvertToCamelCase(property.Key);
            var aliasAttribute = property.Key != variableName ? $"AliasAs(\"{property.Key}\")" : "";
            
            formParameters.Add($"{JoinAttributes(aliasAttribute)}{typeName} {variableName}");
        }
    }
}
```

## Test Case Strategy

### Test Case 1: Non-Binary Form Parameter Missing (Issue #222)
**OpenAPI Spec**:
```json
{
  "requestBody": {
    "content": {
      "multipart/form-data": {
        "schema": {
          "properties": {
            "file": { "type": "string", "format": "binary" },
            "organizationalUnitIds": { 
              "type": "array", 
              "items": { "type": "integer", "format": "int64" } 
            }
          }
        }
      }
    }
  }
}
```

**Expected Output**:
```csharp
[Multipart]
[Post("/upload")]
Task UploadAsync(
    [AliasAs("organizationalUnitIds")] long[] organizationalUnitIds,
    [AliasAs("file")] StreamPart file,
    CancellationToken cancellationToken = default);
```

### Test Case 2: String Form Parameter (Issue #231)
**OpenAPI Spec**: See `animal-231.json` - has `Name` string property with binary files

**Expected Output**:
```csharp
[Multipart]
[Post("/animals")]
Task PostAsync(
    [AliasAs("Name")] string name,
    [AliasAs("AnimalClassFile")] StreamPart animalClassFile,
    [AliasAs("AnimalCrowdFile")] StreamPart animalCrowdFile,
    CancellationToken cancellationToken = default);
```

### Test Case 3: Multiple Non-Binary Types
**OpenAPI Spec**:
```json
{
  "requestBody": {
    "content": {
      "multipart/form-data": {
        "schema": {
          "properties": {
            "file": { "type": "string", "format": "binary" },
            "title": { "type": "string" },
            "description": { "type": "string" },
            "tags": { "type": "array", "items": { "type": "string" } },
            "isPublic": { "type": "boolean" }
          }
        }
      }
    }
  }
}
```

**Expected Output**: All properties present in method signature with proper types.

### Validation Tests
1. **Existing Tests Should Pass**: `MultiPartFormDataTests.cs` and `MultiPartFormDataArrayTests.cs` must continue passing
2. **Build Verification**: Generated code must compile with C# compiler
3. **Runtime Test**: If possible, test actual HTTP request formation (though this may be outside Refitter's scope)

## Estimated Effort

**Complexity**: **Medium**

**Reasoning**:
- The root cause is clear: missing parameter extraction logic
- The fix location is well-defined: `ParameterExtractor.cs`
- Requires accessing OpenAPI operation's RequestBody schema directly (not just NSwag's parameter model)
- Need to properly map schema properties to Refit parameters
- Must preserve existing functionality for binary parameters
- Type name generation via `generator.GetTypeName()` adds some complexity
- Testing requires creating new test cases with multipart schemas

**Estimated Time**: 
- Implementation: 2-4 hours
- Testing and validation: 2-3 hours
- Total: 4-7 hours

**Dependencies**:
- Understanding of NSwag's schema model (`JsonSchema`, `OpenApiMediaType`)
- Knowledge of Refit's multipart parameter handling
- Access to `CustomCSharpClientGenerator` for type name resolution

## Related Issues

- Both issues stem from the same root cause: incomplete handling of OpenAPI 3.x `multipart/form-data` schemas
- Existing tests only cover cases where NSwag successfully populates parameters
- May affect other content types if they use schema-based property definitions

## Next Steps

1. Implement the fix in `ParameterExtractor.cs`
2. Add test cases covering both issues
3. Verify existing multipart tests still pass
4. Test with both issue #222 and #231 OpenAPI specs
5. Document the fix in CHANGELOG.md
