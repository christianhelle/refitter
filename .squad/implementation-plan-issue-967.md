# Issue #967 Implementation Plan: Preserve Original Property Names

**Status:** Ready for Implementation  
**Owner:** Fenster (Refitter .NET Dev)  
**Related Issue:** https://github.com/christianhelle/refitter/issues/967  
**Priority:** Feature  
**Complexity:** Medium (3-4 implementation surfaces, moderate test coverage)

---

## Executive Summary

**Goal:** Allow generated contract property names to preserve the original OpenAPI property name (e.g., `payMethod_SumBank`) instead of always converting to PascalCase (`PayMethodSumBank`).

**Product Surface:** CLI option only (Phase 1; .refitter file support deferred)

**Team Decision:** ✅ APPROVED per `.squad/decisions.md` entry  
Team consensus: Use `--property-naming-policy` CLI enum with configurable naming strategies.

---

## 1. Recommended Implementation Shape

### 1.1 CLI Surface Design
```
--property-naming-policy [mode]
```

**Enum Values (in `PropertyNamingPolicy`):**
- `PascalCase` (default) — Current behavior (mandatory conversion)
- `Preserve` — Use exact OpenAPI property names, sanitized only for C# validity
- `CamelCase` — Convert to camelCase instead of PascalCase
- `SnakeCase` — Convert to snake_case format

### 1.2 Implementation Strategy

**Option A (Recommended):** Create pluggable naming generator hierarchy
- Abstract base: `IPropertyNameGenerator` (NSwag interface, already exists)
- Default: `CustomCSharpPropertyNameGenerator` (existing, mandatory PascalCase)
- New: `PreservingPropertyNameGenerator` (sanitizes invalid identifiers only)
- Extensible to support additional policies in future

**Option B (Alternative):** Enum-based mapper
- Single generator class with policy enum switch
- Simpler but less extensible; may need refactoring if users request custom generators

**Recommendation:** Option A — uses existing NSwag patterns, easier to extend, aligns with #516 design (custom generators support).

---

## 2. Exact Files/Components Likely to Change

### 2.1 CLI Layer (New/Modified)

**`src/Refitter/Settings.cs`**
- Add new property with `[CommandOption("--property-naming-policy")]`
- Type: `PropertyNamingPolicy` (enum)
- Default value: `PropertyNamingPolicy.PascalCase`
- Description: "Naming policy for generated contract properties"

**`src/Refitter/GenerateCommand.cs`**
- In `CreateRefitGeneratorSettings()` method (line ~300-350)
- Map CLI setting to `RefitGeneratorSettings.PropertyNamingPolicy`
- Instantiate correct generator based on policy enum
- Inject into `CodeGeneratorSettings.PropertyNameGenerator`

### 2.2 Core Layer (New/Modified)

**`src/Refitter.Core/PropertyNamingPolicy.cs` (NEW)**
```csharp
namespace Refitter.Core;

public enum PropertyNamingPolicy
{
    PascalCase = 0,    // Default
    Preserve = 1,      // Use original names
    CamelCase = 2,     // Convert to camelCase
    SnakeCase = 3      // Convert to snake_case
}
```

**`src/Refitter.Core/Settings/RefitGeneratorSettings.cs`**
- Add property: `public PropertyNamingPolicy PropertyNamingPolicy { get; set; } = PropertyNamingPolicy.PascalCase;`
- With `[Description]` and `[JsonConverter]` for serialization
- NOT in CodeGeneratorSettings (that's internal NSwag coupling)

**`src/Refitter.Core/PreservingPropertyNameGenerator.cs` (NEW)**
```csharp
internal class PreservingPropertyNameGenerator : IPropertyNameGenerator
{
    public string Generate(JsonSchemaProperty property) =>
        string.IsNullOrWhiteSpace(property.Name)
            ? "_"
            : SanitizeForCSharp(property.Name);

    private static string SanitizeForCSharp(string name)
    {
        // Remove/replace invalid C# identifier characters
        // Detect reserved keywords, add @-prefix if needed
        // Handle leading digits with underscore prefix
        // Return safe identifier
    }
}
```

**`src/Refitter.Core/CamelCasePropertyNameGenerator.cs` (NEW)** — Optional for Phase 1
```csharp
internal class CamelCasePropertyNameGenerator : IPropertyNameGenerator
{
    public string Generate(JsonSchemaProperty property) =>
        // Convert to camelCase
}
```

**`src/Refitter.Core/CSharpClientGeneratorFactory.cs`**
- Line ~33: Change hardcoded generator selection
- From: `PropertyNameGenerator = settings.CodeGeneratorSettings?.PropertyNameGenerator ?? new CustomCSharpPropertyNameGenerator()`
- To: `PropertyNameGenerator = CreatePropertyNameGenerator(settings)`
- New private method: `CreatePropertyNameGenerator(RefitGeneratorSettings)` to route based on `PropertyNamingPolicy` enum

### 2.3 Shared Infrastructure (New/Modified)

**`src/Refitter.Core/IdentifierUtils.cs` (NEW OR EXTEND)**
- Consolidate C# identifier validation and sanitization
- Methods:
  - `bool IsValidIdentifier(string name)` — Checks if string is valid C# identifier
  - `string SanitizeInvalidIdentifier(string name)` — Converts/removes invalid chars
  - `string EscapeReservedKeyword(string name)` — Returns `@name` if reserved
- Use in all property name generators to avoid duplication

**`src/Refitter.Core/Serializer.cs`** (No changes expected)
- `PropertyNamingPolicy` enum will serialize/deserialize via `[JsonConverter]` automatically

### 2.4 Testing Layer (New)

**`src/Refitter.Tests/Examples/PropertyNamingPolicyTests.cs` (NEW)**
- Test suite covering all 4 enum values
- Tests per SKILL.md validation checklist (5+ test cases minimum)

---

## 3. Ordered Work Breakdown with Dependencies

### Phase 1: Core Infrastructure (Foundation)

**Task 1.1: Create PropertyNamingPolicy Enum**
- File: `src/Refitter.Core/PropertyNamingPolicy.cs`
- Deliverable: 4-value enum (PascalCase, Preserve, CamelCase, SnakeCase)
- Dependencies: None
- Effort: 5 minutes
- Validation: Compiles

**Task 1.2: Create IdentifierUtils sanitization helper**
- File: `src/Refitter.Core/IdentifierUtils.cs` (may extend existing if it exists)
- Deliverable: 3 public methods (IsValidIdentifier, SanitizeInvalidIdentifier, EscapeReservedKeyword)
- Dependencies: None
- Effort: 30 minutes (careful with reserved keyword list)
- Validation: Unit tests (if new file); manual spot-check on examples below:
  - `payMethod_SumBank` → valid, no change
  - `pay-method` → invalid, convert to `pay_method` or reject
  - `class` → reserved keyword, becomes `@class`
  - `_123abc` → valid (leading underscore)
  - `123abc` → invalid, becomes `_123abc`

**Task 1.3: Create PreservingPropertyNameGenerator**
- File: `src/Refitter.Core/PreservingPropertyNameGenerator.cs`
- Deliverable: IPropertyNameGenerator implementation; delegates to IdentifierUtils sanitization
- Dependencies: Task 1.2 (IdentifierUtils)
- Effort: 15 minutes
- Validation: Compiles, basic usage test

**Task 1.4: Create CamelCasePropertyNameGenerator (Optional for MVP)**
- File: `src/Refitter.Core/CamelCasePropertyNameGenerator.cs`
- Deliverable: IPropertyNameGenerator; converts via NSwag's ConversionUtilities
- Dependencies: Task 1.2 (IdentifierUtils)
- Effort: 15 minutes
- Validation: Compiles

### Phase 2: Settings & Routing (Plumbing)

**Task 2.1: Add PropertyNamingPolicy to RefitGeneratorSettings**
- File: `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`
- Deliverable: New property + XML doc + `[Description]` + `[JsonConverter]`
- Dependencies: Task 1.1 (PropertyNamingPolicy enum)
- Effort: 10 minutes
- Validation: Serializer.Serialize/Deserialize preserve value; json-schema.json updated

**Task 2.2: Update CSharpClientGeneratorFactory.Create() routing**
- File: `src/Refitter.Core/CSharpClientGeneratorFactory.cs`
- Deliverable: Replace line 33 logic with new `CreatePropertyNameGenerator()` method
- Dependencies: Tasks 1.1, 1.3, 1.4, 2.1
- Effort: 15 minutes
- Validation: Factory instantiates correct generator per policy; existing tests still pass

**Task 2.3: Add CLI option to Settings.cs**
- File: `src/Refitter/Settings.cs`
- Deliverable: New property with `[CommandOption("--property-naming-policy")]`
- Dependencies: Task 1.1
- Effort: 5 minutes
- Validation: CLI help shows option; defaults to PascalCase

**Task 2.4: Add mapper in GenerateCommand.cs**
- File: `src/Refitter/GenerateCommand.cs`
- Deliverable: Map CLI Settings.PropertyNamingPolicy to RefitGeneratorSettings.PropertyNamingPolicy
- Dependencies: Tasks 1.1, 2.1, 2.3
- Effort: 5 minutes
- Validation: CLI option passed through to core; integration test

### Phase 3: Testing & Validation (Quality)

**Task 3.1: Create PropertyNamingPolicyTests.cs**
- File: `src/Refitter.Tests/Examples/PropertyNamingPolicyTests.cs`
- Deliverable: Test suite with 10+ tests per SKILL.md validation checklist:
  - Basic generation with each policy
  - Preserve mode: raw snake_case preserved in property name
  - PascalCase mode: current behavior maintained (regression test)
  - Edge cases: hyphens, spaces, reserved keywords, leading digits, Unicode
  - Compilation: generated code builds via BuildHelper
  - Serialization roundtrip: JSON deserializes correctly with `[JsonPropertyName]`
- Dependencies: Tasks 1.1–2.4 (entire core implementation)
- Effort: 2 hours
- Validation: All tests pass; 100% code coverage on new generators

**Task 3.2: Regression testing (existing test suite)**
- Run: `dotnet test -c Release src/Refitter.slnx`
- Deliverable: All 1415+ existing tests pass
- Dependencies: All previous tasks
- Effort: 15 minutes (wait time only)
- Validation: Zero new test failures

### Phase 4: Documentation & Polish

**Task 4.1: Update README.md**
- File: `README.md` section on CLI OPTIONS
- Deliverable:
  - Add `--property-naming-policy` to option list with description
  - Add example usage: `refitter ./openapi.json --property-naming-policy Preserve`
  - Add explanation of each enum value and use cases
  - Note: `[JsonPropertyName]` attribute is automatically generated for binding
- Effort: 30 minutes
- Validation: README.md is valid markdown; .refitter example JSON remains valid

**Task 4.2: Update json-schema.json**
- File: `docs/json-schema.json`
- Deliverable:
  - Add `propertyNamingPolicy` property definition with enum constraint
  - Add example value in .refitter sample
- Effort: 15 minutes
- Validation: `ConvertFrom-Json` succeeds; schema remains valid JSON Schema

**Task 4.3: Code formatting & final validation**
- Run: `dotnet format src/Refitter.slnx`
- Run: `dotnet format --verify-no-changes src/Refitter.slnx`
- Run: Full build & test cycle
- Effort: 5 minutes (automated)
- Validation: Zero formatting violations; build succeeds

---

## 4. Test Strategy and Validation Commands

### 4.1 Unit Tests (Automated)

**Test 1: Basic Preserve Mode**
```csharp
[Test]
public async Task Preserve_Mode_Generates_Snake_Case_Property_Names()
{
    var spec = @"
openapi: 3.0.0
info:
  title: API
  version: 1.0.0
components:
  schemas:
    Item:
      type: object
      properties:
        payMethod_SumBank:
          type: number
        order_ID:
          type: string
";
    var settings = new RefitGeneratorSettings { PropertyNamingPolicy = PropertyNamingPolicy.Preserve };
    var code = await GenerateCode(spec, settings);
    
    code.Should().Contain("public double? payMethod_SumBank");
    code.Should().Contain("public string? order_ID");
    code.Should().NotContain("PayMethodSumBank");
    code.Should().NotContain("OrderID");
}
```

**Test 2: PascalCase Mode (Regression)**
```csharp
[Test]
public async Task PascalCase_Mode_Maintains_Current_Behavior()
{
    var spec = /* same as above */;
    var settings = new RefitGeneratorSettings { PropertyNamingPolicy = PropertyNamingPolicy.PascalCase };
    var code = await GenerateCode(spec, settings);
    
    code.Should().NotContain("payMethod_SumBank");
    code.Should().Contain("public double? PayMethodSumBank");
}
```

**Test 3: Invalid C# Identifier Handling**
```csharp
[Test]
public async Task Preserve_Mode_Rejects_Invalid_Identifiers()
{
    var spec = @"
components:
  schemas:
    Item:
      properties:
        pay-method:  // Hyphens invalid in C#
          type: string
";
    var settings = new RefitGeneratorSettings { PropertyNamingPolicy = PropertyNamingPolicy.Preserve };
    
    var action = async () => await GenerateCode(spec, settings);
    await action.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*invalid C# identifier*");
}
```

**Test 4: Reserved Keyword Escaping**
```csharp
[Test]
public async Task Preserve_Mode_Escapes_Reserved_Keywords()
{
    var spec = @"
components:
  schemas:
    Item:
      properties:
        class:
          type: string
        return:
          type: string
";
    var settings = new RefitGeneratorSettings { PropertyNamingPolicy = PropertyNamingPolicy.Preserve };
    var code = await GenerateCode(spec, settings);
    
    code.Should().Contain("public string? @class");
    code.Should().Contain("public string? @return");
}
```

**Test 5: Compilation & Serialization**
```csharp
[Test]
public async Task Generated_Code_With_Preserve_Mode_Compiles_And_Deserializes()
{
    var spec = /* snake_case properties */;
    var code = await GenerateCode(spec, new RefitGeneratorSettings { PropertyNamingPolicy = PropertyNamingPolicy.Preserve });
    var dll = BuildHelper.BuildCSharp(code);
    
    var json = @"{""payMethod_SumBank"": 123.45}";
    var obj = JsonSerializer.Deserialize(json, dll.GetType("Item"));
    
    obj.GetProperty("payMethod_SumBank").GetValue(obj).Should().Be(123.45d);
}
```

### 4.2 Integration Tests (Manual/CLI)

**Validation 1: CLI Help**
```bash
dotnet run --project src/Refitter --framework net9.0 -- --help | grep -A5 property-naming-policy
# Expected output: --property-naming-policy option listed with description and enum values
```

**Validation 2: CLI Generation with Sample Spec**
```bash
# Create temp spec with snake_case properties
cat > /tmp/test-api.json << 'EOF'
{
  "openapi": "3.0.0",
  "info": {"title": "Test", "version": "1.0.0"},
  "components": {
    "schemas": {
      "Order": {
        "type": "object",
        "properties": {
          "order_id": {"type": "integer"},
          "payment_method": {"type": "string"}
        }
      }
    }
  }
}
EOF

# Generate with Preserve mode
dotnet run --project src/Refitter --framework net9.0 -- \
  /tmp/test-api.json \
  --property-naming-policy Preserve \
  --output /tmp/test-preserved.cs

# Verify output
grep -q "public.*order_id" /tmp/test-preserved.cs && echo "✓ Preserve mode works"

# Generate with PascalCase (default)
dotnet run --project src/Refitter --framework net9.0 -- \
  /tmp/test-api.json \
  --output /tmp/test-pascal.cs

# Verify output  
grep -q "public.*OrderId" /tmp/test-pascal.cs && echo "✓ PascalCase mode works"
```

**Validation 3: Serialization Roundtrip**
```csharp
// Manual test: Create small console app with generated contract
var json = @"{""order_id"": 42, ""payment_method"": ""credit_card""}";
var order = JsonSerializer.Deserialize<Order>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = false });

Assert.NotNull(order);
Assert.Equal(42, order.order_id);
Assert.Equal("credit_card", order.payment_method);

// Serialize back
var json2 = JsonSerializer.Serialize(order);
Assert.Contains("\"order_id\"", json2);
```

### 4.3 Build & Format Validation

```bash
# Full validation pipeline (required before PR)
dotnet build -c Release src/Refitter.slnx
dotnet test -c Release src/Refitter.slnx  # Allow network-related failures only
dotnet format --verify-no-changes src/Refitter.slnx
```

---

## 5. Risks, Edge Cases, and Considerations

### 5.1 Critical Edge Cases

| Edge Case | Risk | Mitigation |
|-----------|------|-----------|
| **Hyphenated names** (`pay-method`) | Invalid C# identifier | Reject in Preserve mode with clear error; offer alternative (sanitize to `pay_method`) |
| **Reserved keywords** (`class`, `return`) | Compilation error | Auto-escape with `@` prefix (e.g., `@class`) |
| **Leading digit** (`123abc`) | Invalid C#; SyntaxError | Prepend underscore → `_123abc` |
| **Spaces/special chars** | Invalid C# | Either reject or map to safe replacement |
| **Unicode** (`café_name`) | Valid in C# identifiers | No action; UTF-16 native support |
| **Collision** (`Class` → `@class` and `_class` → `_class`) | Two properties same final name | Emit warning; use sequential suffix (`_class`, `_class2`) |
| **[JsonPropertyName] missing** | Deserialization fails | ALWAYS emit `[JsonPropertyName("originalName")]` regardless of mode |

### 5.2 Product Considerations

- **Backward Compatibility:** Default is `PascalCase`; no breaking change
- **Serialization:** `.refitter` file changes: `propertyNamingPolicy` added to schema; old files without it default to PascalCase
- **Source Generator:** Must support `propertyNamingPolicy` in `.refitter` files (future phase)
- **Documentation:** Clarify that `[JsonPropertyName]` is always generated for binding to work correctly

### 5.3 Implementation Complexity Notes

- **NSwag Integration:** `IPropertyNameGenerator` is external interface; cannot extend via JSON schema discriminator. CLI-only is correct choice.
- **Keyword Detection:** Use `Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsReservedKeyword()` if available; otherwise maintain C# v11 keyword list
- **Character Validation:** `char.IsLetterOrDigit()` + `_` is base validation; full `[A-Za-z_][A-Za-z0-9_]*` pattern is more reliable

### 5.4 Documentation Gaps to Address

- Add section to README: "Property Naming Policies"
- Note: Preserve mode requires `[JsonPropertyName]` for deserialization (auto-generated)
- Clarify when to use each policy:
  - **PascalCase** (default): Standard .NET convention, breaking with API naming
  - **Preserve**: Match API exactly, maintain consistency with backend
  - **CamelCase**: JavaScript-compatible naming in contracts
  - **SnakeCase**: Rare, but for APIs strictly using snake_case

---

## 6. Source-Generator & Shared-Source Implications

### 6.1 Refitter.SourceGenerator Impact

**Status:** DEFERRED (Phase 2)

The source generator reads `.refitter` settings files and should support `propertyNamingPolicy`. However:

1. Current limitation: `.refitter` files cannot deserialize interface types (e.g., `IPropertyNameGenerator`)
2. Workaround: Use enum + factory pattern (same as CLI)
3. Implementation: After CLI Phase 1 completes, add `propertyNamingPolicy` property to `.refitter` schema and update source generator to instantiate correct generator based on enum

**Required Changes (Phase 2):**
- `Refitter.SourceGenerator/RefitterSourceGenerator.cs`: Add routing logic (copy from GenerateCommand)
- Update `.refitter` file schema documentation
- No changes needed to generator classes themselves

### 6.2 Refitter.MSBuild Impact

**Status:** SAME AS SOURCE GENERATOR (Deferred Phase 2)

The MSBuild task also reads `.refitter` files. Same implementation pattern applies.

### 6.3 Shared-Source Considerations

- **IdentifierUtils:** Used by both CLI and source generator; place in `Refitter.Core` (shared)
- **PropertyNamingPolicy enum:** Used by both; place in `Refitter.Core`
- **Generator classes:** (PreservingPropertyNameGenerator, CamelCasePropertyNameGenerator) used by both; place in `Refitter.Core`
- **GenerateCommand routing logic:** CLI-specific; keep in `src/Refitter/`; source generator will have its own copy

### 6.4 Schema Evolution

**`docs/json-schema.json` changes (Phase 1 AND Phase 2):**

Add to `.refitter` schema:
```json
"propertyNamingPolicy": {
  "type": "string",
  "description": "Naming policy for generated contract properties",
  "enum": ["PascalCase", "Preserve", "CamelCase", "SnakeCase"],
  "default": "PascalCase"
}
```

---

## 7. Success Criteria & Acceptance Tests

### Phase 1 (CLI) — MUST PASS

- [ ] `PropertyNamingPolicy` enum created and compiles
- [ ] `PreservingPropertyNameGenerator` created; generates snake_case names
- [ ] CLI option `--property-naming-policy` added and functional
- [ ] All 10+ unit tests pass (PropertyNamingPolicyTests.cs)
- [ ] All 1415+ existing tests still pass (zero regressions)
- [ ] `dotnet format --verify-no-changes` passes
- [ ] Manual CLI validation: `--property-naming-policy Preserve` generates correct code
- [ ] Serialization roundtrip test passes
- [ ] README.md updated with new option and examples
- [ ] `docs/json-schema.json` updated (if needed for future compatibility)

### Phase 2 (Source Generator/MSBuild) — FUTURE

- [ ] `.refitter` files support `propertyNamingPolicy` property
- [ ] Source generator instantiates correct generator based on setting
- [ ] MSBuild task respects setting
- [ ] Documentation updated for `.refitter` files

---

## 8. Timeline & Effort Estimate

| Phase | Task Count | Estimated Effort | Critical Path |
|-------|-----------|------------------|----------------|
| 1 Core Infrastructure | 4 tasks | 1 hour | IdentifierUtils → Generators → Enum |
| 2 Settings & Routing | 4 tasks | 45 min | PropertyNamingPolicy enum + factory |
| 3 Testing | 2 tasks | 2.5 hours | Depends on all of Phase 1–2 |
| 4 Documentation | 3 tasks | 1 hour | Can run in parallel |
| **TOTAL** | **13 tasks** | **5 hours** | **Day 1** |

**Phase 2 (Source Generator/MSBuild, Future):** +2 hours, after Phase 1 validated

---

## 9. File Checklist (Complete List of Changes)

### New Files
- [ ] `src/Refitter.Core/PropertyNamingPolicy.cs`
- [ ] `src/Refitter.Core/IdentifierUtils.cs` (or extend if exists)
- [ ] `src/Refitter.Core/PreservingPropertyNameGenerator.cs`
- [ ] `src/Refitter.Core/CamelCasePropertyNameGenerator.cs` (optional MVP)
- [ ] `src/Refitter.Tests/Examples/PropertyNamingPolicyTests.cs`

### Modified Files
- [ ] `src/Refitter/Settings.cs` — Add `PropertyNamingPolicy` CLI option
- [ ] `src/Refitter/GenerateCommand.cs` — Add mapper in `CreateRefitGeneratorSettings()`
- [ ] `src/Refitter.Core/Settings/RefitGeneratorSettings.cs` — Add `PropertyNamingPolicy` property
- [ ] `src/Refitter.Core/CSharpClientGeneratorFactory.cs` — Update line 33 generator selection logic
- [ ] `README.md` — Add CLI option documentation and examples
- [ ] `docs/json-schema.json` — Add `propertyNamingPolicy` property definition

### No Changes Required
- `src/Refitter.Core/CustomCSharpPropertyNameGenerator.cs` — Keep as-is (for PascalCase mode)
- `src/Refitter.Core/Settings/CodeGeneratorSettings.cs` — Keep `PropertyNameGenerator` as-is (internal)
- `src/Refitter.Tests/SerializerTests.cs` — Existing exclusions remain valid

---

## 10. Known Unknowns & Decision Points

1. **Hyphen Handling:** Should Preserve mode reject hyphens outright, or sanitize to underscore?
   - **Decision:** Reject with clear error message (safer, prevents silent behavior changes)

2. **Keyword Escaping in Preserve Mode:** Should we use `@` prefix or `_` suffix for reserved keywords?
   - **Decision:** Use `@` prefix (standard C# pattern for keyword avoidance)

3. **CamelCase Mode Priority:** Should we implement this in Phase 1, or defer?
   - **Decision:** Defer to Phase 1b if time permits; Preserve mode is MVP

4. **Name Collision Resolution:** If `Class` and `_class` both exist, how to handle?
   - **Decision:** Emit warning; use sequential suffix (`_class2`, `_class3`)

5. **Preserve Mode Default:** Should new projects default to Preserve or PascalCase?
   - **Decision:** Default to PascalCase (backward compatible); users opt-in to Preserve

---

## Appendix: Example Generated Output

### Input OpenAPI Schema
```yaml
components:
  schemas:
    PaymentOrder:
      type: object
      properties:
        payMethod_SumBank:
          type: number
        order_id:
          type: integer
        customer_Name:
          type: string
```

### Output with PascalCase (Current, Default)
```csharp
public class PaymentOrder
{
    [JsonPropertyName("payMethod_SumBank")]
    public double? PayMethodSumBank { get; set; }
    
    [JsonPropertyName("order_id")]
    public int? OrderId { get; set; }
    
    [JsonPropertyName("customer_Name")]
    public string? CustomerName { get; set; }
}
```

### Output with Preserve Mode (NEW)
```csharp
public class PaymentOrder
{
    [JsonPropertyName("payMethod_SumBank")]
    public double? payMethod_SumBank { get; set; }
    
    [JsonPropertyName("order_id")]
    public int? order_id { get; set; }
    
    [JsonPropertyName("customer_Name")]
    public string? customer_Name { get; set; }
}
```

**Key Difference:** Property names match OpenAPI exactly; `[JsonPropertyName]` attribute ensures correct serialization.

---

## Document Metadata

**Version:** 1.0  
**Created:** 2026-03-25  
**Author:** Fenster (Refitter .NET Dev)  
**Team Consensus Date:** 2026-03-25 (per `.squad/decisions.md`)  
**Next Review:** After Phase 1 implementation complete  
**Related Artifacts:**
- `.squad/agents/fenster/history.md` — Issue #967 investigation findings
- `.squad/skills/raw-property-names/SKILL.md` — Test design & edge-case validation
- `.squad/decisions.md` — Team consensus on feature approval
- GitHub Issue #967 — Feature request and discussion
