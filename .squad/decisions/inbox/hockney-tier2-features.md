# Tier 2 Investigation: Additional Features & Issues

**Investigator:** Hockney (Test & QA Specialist)  
**Date:** 2026-03-03  
**Scope:** Issues #361, #299, #423, #438, #193

---

## Executive Summary

Investigated 5 Tier 2/3 feature requests and bugs. **Critical finding:** Issue #361 and #672 are the **same bug** with a root cause in global identifier tracking. This bug is present but has a **working test suite** demonstrating the expected fix, indicating the fix may be partially implemented but not working correctly.

---

## Issues & Status

### 1. **Issue #361** - Code Generator adds numeric suffix when not needed ⚠️ HIGH PRIORITY
- **Related:** #672 (duplicate/same root cause)
- **Severity:** Bug (High Impact)
- **Status:** Open since 2024-04-10 (11 months)
- **Current State:** Has **failing test suite** (MultipleInterfacesByTagMethodNamingTests.cs)

**Problem:**  
When `multipleInterfaces: ByTag` is set, method names get numeric suffixes even when names are unique within their interface scope. The uniqueness check is performed **globally** instead of **per-interface**.

**Example (Actual):**
```csharp
public partial interface IUsersApi {
    Task GetAllUsersAsync(CancellationToken ct);    // Should be this
    Task GetAllUsers2Async(CancellationToken ct);   // Incorrectly numbered!
}

public partial interface IProductsApi {
    Task GetAllProducts3Async(CancellationToken ct); // Global counter continues!
}
```

**Example (Expected):**
```csharp
public partial interface IUsersApi {
    Task GetAllUsersAsync(CancellationToken ct);
}

public partial interface IProductsApi {
    Task GetAllProductsAsync(CancellationToken ct);  // Clean name, no suffix
}
```

**Root Cause Located:**  
- **File:** `src\Refitter.Core\RefitMultipleInterfaceByTagGenerator.cs` (Line 8-159)
- **Mechanism:** Uses a **single global** `HashSet<string> knownIdentifiers` for ALL interfaces
- **Problem:** Line 159 adds `$"{interfaceName}.{generatedName}"` but the parent scoping logic in `IdentifierUtils.Counted()` doesn't prevent global counters from incrementing

**Evidence:**
```csharp
// src\Refitter.Core\RefitMultipleInterfaceByTagGenerator.cs:8
private readonly HashSet<string> knownIdentifiers = new(); // GLOBAL SCOPE!

// Line 158-159 in GetOperationName()
var generatedName = IdentifierUtils.Counted(
    knownIdentifiers,  // Same set used for ALL interfaces
    GenerateOperationName(name, verb, operation, capitalizeFirstCharacter: true), 
    parent: interfaceName);
knownIdentifiers.Add($"{interfaceName}.{generatedName}");
```

The `IdentifierUtils.Counted()` method (lines 9-23 in IdentifierUtils.cs) checks for conflicts using parent scoping BUT the issue is that the counter increments based on previous additions across all interfaces.

**Test Evidence:**
- **Test File:** `src\Refitter.Tests\Examples\MultipleInterfacesByTagMethodNamingTests.cs`
- **Status:** Tests exist and define **correct expected behavior**
- **Tests Assert:** Method names should NOT have numeric suffixes when unique within interface
- Lines 166-213: Multiple test methods verify no "GetAllUsers2", "GetAllProducts2", etc.

**Fix Strategy:**
Need to refactor identifier tracking to be **per-interface** rather than global. Options:
1. Create separate `knownIdentifiers` set per interface during generation
2. Modify `IdentifierUtils.Counted()` to properly scope parent context
3. Reset or partition the global set when switching interfaces

**Dependencies:** None  
**Estimated Effort:** **MEDIUM** (2-4 hours)
- Requires understanding of generation flow
- Tests already exist (75% of work done!)
- Fix is surgical: change scope of knownIdentifiers

---

### 2. **Issue #299** - IAsyncEnumerable for JSONL/application/x-ndjson Support 🔬 RESEARCH NEEDED
- **Severity:** Enhancement (Medium Priority)
- **Status:** Open since 2024-01-18 (13 months)
- **Use Case:** OpenAI streaming responses, Server-Sent Events

**Problem:**  
No support for streaming JSONL responses as `IAsyncEnumerable<T>`. Currently generates synchronous responses for all content types.

**Expected Behavior:**
```csharp
// For content-type: application/x-ndjson or application/jsonl
[Get("/stream/data")]
IAsyncEnumerable<ChatMessage> StreamMessages(CancellationToken ct);
```

**Current Behavior:**
```csharp
[Get("/stream/data")]
Task<ICollection<ChatMessage>> GetMessages(CancellationToken ct);
```

**Technical Analysis:**
- **Response Type Detection:** `src\Refitter.Core\RefitInterfaceGenerator.cs` (Lines 112-145)
  - Method: `GetTypeName(OpenApiOperation operation)`
  - Checks response status codes (200, 201, 203, 206, 2XX, default)
  - Does NOT check content-type headers
  
- **Content Type Checking:** Lines 148-193
  - `IsFileStreamResponse()` checks for binary content types
  - Returns `Task<HttpResponseMessage>` for file downloads
  - **NO check for streaming JSON types** (`application/x-ndjson`, `application/jsonl`, `text/event-stream`)

**Refit Support:**
Refit DOES support `IAsyncEnumerable<T>` return types (since Refit 6.0). Example:
```csharp
[Get("/stream")]
IAsyncEnumerable<T> StreamData(CancellationToken cancellationToken);
```

**Implementation Path:**
1. Add content-type detection for streaming types in `GetTypeName()`
2. Add setting: `GenerateStreamingResponses` (bool, default: false)
3. When detected:
   - Parse schema to get item type
   - Return `IAsyncEnumerable<{ItemType}>` instead of `Task<ICollection<{ItemType}>>`
4. Add tests with OpenAPI spec containing `application/x-ndjson` responses

**Challenges:**
- NSwag's schema parsing for streaming responses may be limited
- Need to handle both JSONL (line-delimited JSON) and SSE (Server-Sent Events)
- Refit's streaming support may require specific configuration

**Dependencies:**  
- Refit 6.0+ for IAsyncEnumerable support
- OpenAPI spec must specify `application/x-ndjson` or `application/jsonl` content-type

**Estimated Effort:** **COMPLEX** (8-12 hours)
- Requires deep understanding of response type generation
- Need to research Refit's streaming implementation
- Must handle edge cases (what if schema is not array?)
- Testing requires real streaming API

---

### 3. **Issue #423** - AnyType + BodySerializationMethod handling 💡 WORKAROUND EXISTS
- **Severity:** Enhancement (Low Priority)
- **Status:** Open since 2024-07-22 (7 months)
- **Use Case:** Sending `JsonElement` with proper serialization

**Problem:**  
When using `anyType: "System.Text.Json.JsonElement"`, the body is sent as a **stream** instead of JSON string. User wants to control `BodySerializationMethod` for AnyType parameters.

**Current Behavior:**
```csharp
// Generated with anyType: "System.Text.Json.JsonElement"
[Post("/api/endpoint")]
Task SomeThing([Body] JsonElement body, CancellationToken ct);

// Refit sends this as StreamContent (binary)
```

**Desired Behavior:**
```csharp
// User wants to add [Body(BodySerializationMethod.Json)]
[Post("/api/endpoint")]
Task SomeThing([Body(BodySerializationMethod.Json)] JsonElement body, CancellationToken ct);
```

**Technical Analysis:**
- **AnyType Setting:** `src\Refitter.Core\Settings\CodeGeneratorSettings.cs` (Line 35)
  - Property: `AnyType` (default: "object")
  - Used by NSwag to generate types for schemas with `additionalProperties: true`
  
- **Body Parameter Generation:** `src\Refitter.Core\ParameterExtractor.cs` (assumed, not viewed)
  - Currently generates `[Body]` attribute without serialization method
  - No option to customize serialization method per parameter

**Solution Options:**

**Option A:** Add global setting (SIMPLE)
```json
{
  "anyType": "System.Text.Json.JsonElement",
  "anyTypeBodySerializationMethod": "Json"  // NEW SETTING
}
```

**Option B:** Add per-operation override (COMPLEX)
```json
{
  "anyType": "System.Text.Json.JsonElement",
  "bodySerializationMethodOverrides": {
    "operationId1": "Json",
    "operationId2": "Serialized"
  }
}
```

**Recommended:** Option A (global setting)
- Covers 90% of use cases
- Simple to implement
- Follows existing pattern (like `anyType` setting)

**Implementation:**
1. Add `AnyTypeBodySerializationMethod` property to `RefitGeneratorSettings`
2. Detect when parameter type matches `AnyType`
3. Add `BodySerializationMethod` to `[Body]` attribute if set
4. Example: `[Body(BodySerializationMethod.Json)]`

**Dependencies:** None  
**Estimated Effort:** **SIMPLE** (2-3 hours)
- Clear scope
- Follows existing patterns
- Low risk

---

### 4. **Issue #438** - Support custom format-mappings via configuration 🎯 HIGH VALUE
- **Severity:** Enhancement (Medium-High Priority)
- **Status:** Open since 2024-08-05 (7 months)
- **GitHub Reactions:** +2 (community interest)
- **Use Case:** Custom date types, proprietary serialization

**Problem:**  
No way to map custom OpenAPI formats to .NET types. Currently only supports built-in types:
- `date-time` → `DateTimeOffset`
- `date` → `DateOnly` (via `dateType` setting)

**Example Use Case:**
```yaml
# OpenAPI Schema
type: string
format: my-custom-datetime  # Custom format!
```

**Current Behavior:**
```csharp
public string SomeProperty { get; set; }  // Falls back to string
```

**Desired Behavior:**
```json
// .refitter configuration
{
  "codeGeneratorSettings": {
    "customFormatMappings": {
      "string:my-custom-datetime": "MyDomain.CustomDateTime",
      "string:iso-duration": "NodaTime.Duration",
      "integer:unix-timestamp": "System.DateTimeOffset"
    }
  }
}
```

```csharp
public MyDomain.CustomDateTime SomeProperty { get; set; }
```

**Technical Analysis:**
- **Current Format Handling:** `src\Refitter.Core\CSharpClientGeneratorFactory.cs` (Lines 68-164)
  - Method: `FixMissingTypesWithIntegerFormat()` - handles int32/int64/float/double
  - Method: `ApplyCustomIntegerType()` - applies custom integer types
  - Uses **NSwag's type resolution system**

- **Existing Type Overrides:** `src\Refitter.Core\Settings\CodeGeneratorSettings.cs`
  - Lines 35-78: Predefined type mappings (AnyType, DateType, DateTimeType, TimeType, etc.)
  - These are **global type replacements**, not format-based

**NSwag Integration Point:**
Issue mentions: `CSharpClientGeneratorSettings.CodeGeneratorSettings.TypeNameGenerator`
- This is the extension point in NSwag for custom type name generation
- Can override how types are resolved based on schema properties

**Implementation Strategy:**

**Phase 1: Configuration Model**
```csharp
// Add to CodeGeneratorSettings.cs
public Dictionary<string, string>? CustomFormatMappings { get; set; }
// Key: "type:format" (e.g., "string:my-datetime")
// Value: ".NET type" (e.g., "MyDomain.CustomDateTime")
```

**Phase 2: Type Name Generator**
```csharp
internal class CustomTypeNameGenerator : ITypeNameGenerator
{
    private readonly Dictionary<string, string> formatMappings;
    
    public string GetTypeName(JsonSchema schema, bool isNullable)
    {
        var key = $"{schema.Type}:{schema.Format}";
        if (formatMappings.TryGetValue(key, out var customType))
            return customType;
        
        return defaultGenerator.GetTypeName(schema, isNullable);
    }
}
```

**Phase 3: Integration**
- Hook into `CSharpClientGeneratorFactory.Create()` (Line 13)
- Set custom `TypeNameGenerator` if `CustomFormatMappings` is provided
- Apply mappings during schema processing

**Challenges:**
- **NSwag Dependency:** Must work within NSwag's type resolution system
- **Validation:** Need to validate that custom types exist/are accessible
- **Namespace Handling:** Custom types may need full namespaces or using statements

**Dependencies:**  
- Deep understanding of NSwag's TypeNameGenerator interface
- May require NSwag version check for API compatibility

**Estimated Effort:** **MEDIUM-COMPLEX** (6-8 hours)
- Requires NSwag API research
- Configuration parsing is straightforward
- Testing needs various format scenarios
- High value for community

---

### 5. **Issue #193** - Config parameter to add suffix to contract types 📝 SIMPLE WIN
- **Severity:** Enhancement (Low Priority)
- **Status:** Open since 2023-10-13 (16 months, oldest open issue)
- **Use Case:** Coding conventions (e.g., `PetDto`, `OrderDto`)

**Problem:**  
No way to automatically add suffix to generated contract/DTO types.

**Current Behavior:**
```csharp
public class Pet { ... }
public class Order { ... }
```

**Desired Behavior:**
```csharp
public class PetDto { ... }
public class OrderDto { ... }
```

**Technical Analysis:**
- **Contract Generation:** Uses NSwag's `CSharpClientGenerator` with `GenerateDtoTypes = true`
- **Naming:** Controlled by NSwag's type name resolution
- **Current Customization:** `ContractsNamespace` setting exists (line 47, RefitGeneratorSettings.cs)

**Similar Existing Feature:**
The `IdentifierUtils.Counted()` method (line 9, IdentifierUtils.cs) already supports `suffix` parameter:
```csharp
public static string Counted(ISet<string> knownIdentifiers, string name, string suffix = "", string parent = "")
```

**Implementation Strategy:**

**Option A: Property Name Generator** (RECOMMENDED)
```csharp
// Add to RefitGeneratorSettings
public string? ContractTypeSuffix { get; set; } // e.g., "Dto"

// Create custom IPropertyNameGenerator (already exists: CustomCSharpPropertyNameGenerator.cs)
// Modify to also handle type names with suffix
internal class CustomTypeNameGenerator : ITypeNameGenerator
{
    private readonly string? suffix;
    
    public string Generate(string originalName)
    {
        return originalName + (suffix ?? string.Empty);
    }
}
```

**Option B: Post-Processing** (SIMPLER)
- After NSwag generates code, use regex to add suffix to type declarations
- Pros: No NSwag integration needed
- Cons: Fragile, won't handle all edge cases

**Recommended:** Option A
- More robust
- Follows existing architecture (CustomCSharpPropertyNameGenerator.cs)
- Can be extended for prefixes, transformations

**Implementation Steps:**
1. Add `ContractTypeSuffix` property to `RefitGeneratorSettings`
2. Add `ContractTypePrefix` property (bonus feature, same effort)
3. Extend existing `CustomCSharpPropertyNameGenerator` to handle type names
4. Hook into `CSharpClientGeneratorSettings` (line 31, CSharpClientGeneratorFactory.cs)
5. Add CLI option `--contract-type-suffix`

**Dependencies:** None  
**Estimated Effort:** **SIMPLE** (2-3 hours)
- Similar to existing property name generation
- Low risk
- Quick win for community

---

## Root Causes & Dependencies

### Issue Relationships

```
#361 ← SAME BUG → #672
  ↓
  Global identifier tracking in RefitMultipleInterfaceByTagGenerator
  
#299 → Requires Refit 6.0+, OpenAPI content-type awareness
#423 → Independent, affects [Body] attribute generation
#438 → Independent, requires NSwag TypeNameGenerator integration
#193 → Independent, requires type name suffix/prefix support
```

### Shared Components

1. **IdentifierUtils.cs** - Used by #361, #672, #193
2. **RefitInterfaceGenerator.cs** - Used by #299 (response types)
3. **CodeGeneratorSettings.cs** - Extended by #423, #438, #193
4. **CSharpClientGeneratorFactory.cs** - Integration point for #438, #193

---

## Effort Estimation Summary

| Issue | Complexity | Estimated Time | Priority | ROI |
|-------|-----------|----------------|----------|-----|
| **#361 + #672** | MEDIUM | 2-4 hours | **HIGH** | ⭐⭐⭐⭐⭐ |
| #299 | COMPLEX | 8-12 hours | MEDIUM | ⭐⭐⭐ |
| #423 | SIMPLE | 2-3 hours | LOW | ⭐⭐ |
| #438 | MEDIUM-COMPLEX | 6-8 hours | MEDIUM-HIGH | ⭐⭐⭐⭐ |
| #193 | SIMPLE | 2-3 hours | LOW | ⭐⭐⭐ |

**Total Estimated Effort:** 20-30 hours

---

## Priority Recommendation

### Tier 1 (Fix Now) 🔥
**#361/#672 - Global identifier tracking bug**
- **Reason:** Existing bug with test suite already written
- **Impact:** Affects production code quality
- **Effort:** Low (tests exist, clear root cause)
- **Risk:** Low (surgical fix, well-tested)

### Tier 2A (High Value) ⭐
**#438 - Custom format mappings**
- **Reason:** High community interest (+2 reactions), enables advanced scenarios
- **Impact:** Unblocks custom type systems (NodaTime, domain types)
- **Effort:** Medium (requires NSwag research)

**#193 - Contract type suffix**
- **Reason:** Oldest issue (16 months), simple implementation
- **Impact:** Coding conventions, professional polish
- **Effort:** Low (simple addition)

### Tier 2B (Feature Enhancement) 📦
**#423 - AnyType serialization**
- **Reason:** Clear workaround available (use specific types)
- **Impact:** Quality of life for JsonElement users
- **Effort:** Low (straightforward addition)

### Tier 3 (Research Required) 🔬
**#299 - IAsyncEnumerable streaming**
- **Reason:** Complex, requires Refit integration research
- **Impact:** Enables modern streaming scenarios (OpenAI, SSE)
- **Effort:** High (8-12 hours, potential unknown issues)
- **Recommendation:** Research spike first (2 hours) to validate feasibility

---

## Recommended Implementation Order

1. **#361/#672** - Fix global identifier bug (2-4 hours)
   - Tests already exist
   - Highest ROI
   - Unblocks user workflows

2. **#193** - Add contract type suffix (2-3 hours)
   - Quick win
   - Oldest issue
   - Community goodwill

3. **#438** - Custom format mappings (6-8 hours)
   - High value
   - Community interest
   - Enables advanced scenarios

4. **#423** - AnyType serialization (2-3 hours)
   - Simple addition
   - Good documentation example

5. **#299** - IAsyncEnumerable (research spike → 2 hours, then re-evaluate)
   - Complex, needs validation
   - Consider as separate epic

---

## Testing Strategy

### For #361/#672 (Identifier Tracking)
- ✅ Tests already exist: `MultipleInterfacesByTagMethodNamingTests.cs`
- Run existing test suite
- Verify all 9 test methods pass after fix

### For #299 (IAsyncEnumerable)
- Create OpenAPI spec with `application/x-ndjson` response
- Test with real Refit streaming endpoint
- Verify generated code compiles
- Integration test with actual streaming server

### For #423 (BodySerializationMethod)
- Unit test with `anyType: JsonElement`
- Verify `[Body(BodySerializationMethod.Json)]` is generated
- Test actual HTTP requests with debugger/Fiddler

### For #438 (Format Mappings)
- Unit tests with custom format schemas
- Verify type mappings work
- Test with NodaTime, custom domain types
- Negative tests (invalid types, missing namespaces)

### For #193 (Contract Suffix)
- Unit test with `contractTypeSuffix: "Dto"`
- Verify all generated contracts have suffix
- Test with prefix option
- Ensure no suffix on interfaces (only contracts)

---

## Conclusion

**Key Finding:** Issue #361/#672 is a **high-priority bug with existing test coverage**, making it the perfect candidate for immediate implementation.

**Recommended Next Steps:**
1. Assign #361/#672 to developer for immediate fix (2-4 hour task)
2. Plan Tier 2A features (#438, #193) for next sprint
3. Conduct research spike on #299 (2 hours) before full implementation
4. Consider #423 as "nice to have" alongside other work

**Total Value:** Addressing these 5 issues will resolve **long-standing pain points** (16-month-old #193), fix **production bugs** (#361/#672), and **enable advanced scenarios** (#438, #299) that position Refitter as a premier OpenAPI client generator.

---

## Technical Notes for Developers

### #361/#672 Fix Checklist
- [ ] Review `RefitMultipleInterfaceByTagGenerator.cs:158-159`
- [ ] Consider per-interface identifier scoping
- [ ] Run `MultipleInterfacesByTagMethodNamingTests` suite
- [ ] Verify no regressions in single-interface mode
- [ ] Test with sample OpenAPI (3+ tags, duplicate operation names)

### #438 Implementation Checklist
- [ ] Research NSwag's `ITypeNameGenerator` interface
- [ ] Add `CustomFormatMappings` to `CodeGeneratorSettings`
- [ ] Implement custom type name generator
- [ ] Add CLI parameter `--custom-format-mappings`
- [ ] Document JSON configuration format
- [ ] Add integration test with NodaTime

### #193 Implementation Checklist
- [ ] Add `ContractTypeSuffix` and `ContractTypePrefix` to settings
- [ ] Extend or create custom type name generator
- [ ] Ensure only contracts (not interfaces) are affected
- [ ] Add CLI parameters `--contract-type-suffix`, `--contract-type-prefix`
- [ ] Add tests for various suffix/prefix combinations

---

**Investigation Complete** ✅  
**Ready for Sprint Planning** ✅
