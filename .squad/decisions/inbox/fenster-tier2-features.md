# Tier 2 Investigation: Type & Serialization Features (#178, #175, #179)

## Issues Summary

- **#178**: StringEnumConverter with custom NamingPolicy — Property-level `[JsonConverter]` attributes prevent custom naming policies
- **#175**: OneOf base type not generated — Discriminator base class missing for `oneOf` schemas  
- **#179**: Add JsonSerializerContext generation — Enable STJ source generators for AOT/trimming

---

## Issue #178: StringEnumConverter with Custom NamingPolicy

### Problem
Currently, Refitter generates `[JsonConverter(typeof(JsonStringEnumConverter))]` attributes **on enum properties** (property-level), which prevents global `JsonSerializerOptions` from applying custom naming policies (e.g., snake_case). According to STJ documentation, property-level converters always override global settings.

### Current Behavior
```csharp
public class FooClass
{
    [JsonConverter(typeof(JsonStringEnumConverter))] // Property-level - BLOCKS custom policy
    public Foo EnumValue { get; set; }
}
```

### Desired Behavior  
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))] // Type-level - ALLOWS custom policy override
public enum Foo { FooOne = 0, FooTwo = 1 }

public class FooClass
{
    public Foo EnumValue { get; set; } // No attribute here
}
```

### Root Cause
**Location**: `src\Refitter.Core\RefitGenerator.cs` (lines 211-229)

The current workaround uses regex to **strip property-level JsonConverter attributes** when `InlineJsonConverters = false`:

```csharp
private string SanitizeGeneratedContracts(string contracts)
{
    if (settings.CodeGeneratorSettings is not { InlineJsonConverters: false })
        return contracts;
    
    const string pattern = @"^\s*\[(System\.Text\.Json\.Serialization\.)?JsonConverter\(typeof\((System\.Text\.Json\.Serialization\.)?JsonStringEnumConverter\)\)\]\s*$";
    // Regex removes property-level attributes
}
```

However, **NSwag generates the attributes in the first place** via `GenerateDtoTypes = true` in `CSharpClientGeneratorFactory.cs` (line 28). Refitter relies entirely on **NSwag** for DTO generation and has limited control over the output.

### Solution Options

**Option A: Partial Workaround (Current State — Partially Implemented)**
- ✅ **Already exists**: `--no-inline-json-converters` flag (CLI) / `InlineJsonConverters = false` (API)  
- ✅ Strips property-level `[JsonConverter]` attributes via regex post-processing
- ❌ Does **not** add type-level `[JsonConverter]` to enum declarations  
- ❌ User must configure `JsonSerializerOptions` with custom `JsonStringEnumConverter` manually

**Status**: Partially addresses #178 but not fully — enums won't serialize as strings unless global settings configured.

**Option B: Full Fix via Custom NSwag Templates (Requires Investigation)**
- Use `CustomTemplateDirectory` setting (already supported) to override NSwag Liquid templates
- Modify enum/property templates to:
  1. Add `[JsonConverter]` to **enum declarations** (type-level)
  2. Remove `[JsonConverter]` from **property declarations**
- **Complexity**: Medium — requires understanding NSwag template syntax and testing
- **Reference**: NSwag templates at https://github.com/RicoSuter/NSwag/wiki/Templates

**Option C: Manual NSwag Customization (Not Feasible)**
- Directly intercept NSwag code generation to rewrite attributes
- **Blocked**: Would require forking NSwag or extensive reflection hacks
- Author explicitly stated in issue comments: _"Very little I can do regarding changing how contracts/dto types are generated"_

### Recommended Approach

**Short-term**: Document existing workaround clearly  
- Update README to explain `--no-inline-json-converters` flag + required `JsonSerializerOptions` configuration
- Example code snippet showing how to configure custom naming policy

**Long-term (if user demand justifies effort)**: Implement Option B  
- Create sample custom NSwag templates for type-level enum converters
- Package as optional feature or separate documentation guide

### Estimated Effort
- **Short-term (documentation only)**: **Simple** — 30 minutes  
- **Long-term (custom templates)**: **Medium** — 4-6 hours (template development + testing + docs)

---

## Issue #175: OneOf Base Type Not Generated

### Problem
When OpenAPI spec uses `oneOf` for polymorphic types (e.g., FusionAuth's `IdentityProvider`), NSwag does **not generate a base class** for the discriminator. Only the derived types are created, breaking polymorphic deserialization.

### Example Spec Structure
```yaml
IdentityProvider:
  oneOf:
    - $ref: '#/components/schemas/GoogleIdentityProvider'
    - $ref: '#/components/schemas/FacebookIdentityProvider'
```

### Expected Output (Missing)
```csharp
public class IdentityProvider { } // BASE CLASS MISSING

public class GoogleIdentityProvider : IdentityProvider { }
public class FacebookIdentityProvider : IdentityProvider { }
```

### Root Cause
**Location**: `src\Refitter.Core\CSharpClientGeneratorFactory.cs` (lines 138-146)

The code processes `AllOf`, `OneOf`, and `AnyOf` schemas during type detection:
```csharp
var subSchemas = actualSchema.AllOf
    .Concat(actualSchema.OneOf)  // OneOf IS traversed
    .Concat(actualSchema.AnyOf)
    .ToArray();
```

However, **NSwag's internal logic** (`CSharpClientGenerator` from NSwag library) does **not generate a base class** for `oneOf` discriminators by default. Refitter piggybacks on NSwag's DTO generation (`GenerateDtoTypes = true`) and inherits this limitation.

### Author's Comment (from issue)
> _"I piggy back entirely on NSwag for parsing the OpenAPI spec and for generating contracts... there is very little I can do to change how these types are generated."_

### Solution Options

**Option A: NSwag Configuration (Investigate)**
- Check if NSwag has hidden settings for `oneOf` base class generation  
- Possible settings: `JsonPolymorphicSerializationStyle`, `InheritanceCode`, discriminator handling
- **Current State**: Refitter uses `JsonPolymorphicSerializationStyle.SystemTextJson` when `UsePolymorphicSerialization = true`
- **Action**: Test with FusionAuth OpenAPI spec to confirm if existing settings help

**Option B: Post-Process Generated Code**
- Analyze generated code for `oneOf` patterns (look for `[JsonDerivedType]` attributes)
- Inject synthetic base class definitions if missing
- **Complexity**: High — brittle, requires AST parsing (Roslyn) or regex hacks

**Option C: Custom NSwag Templates**
- Override Class.liquid template to detect `oneOf` schemas and generate base classes
- **Complexity**: Medium-High — requires deep NSwag template knowledge

**Option D: User Workaround (Current)**
- Generate partial classes manually to extend with base types
- Suggested in issue comments: _"The contract/dto types generated are partial classes. Would it help extending those?"_

### Recommended Approach

**Short-term**: Test Option A with FusionAuth spec  
1. Clone FusionAuth OpenAPI spec: `https://raw.githubusercontent.com/FusionAuth/fusionauth-openapi/main/openapi.yaml`
2. Generate with `UsePolymorphicSerialization = true` and various NSwag settings  
3. Inspect output for base class generation

**Long-term**: If Option A fails, explore Option C (custom templates) or accept as NSwag limitation

### Estimated Effort
- **Option A investigation**: **Medium** — 2-3 hours (test generation, analyze output)  
- **Option C implementation**: **Complex** — 8-12 hours (template overrides + testing)

---

## Issue #179: Add JsonSerializerContext Generation

### Problem
To enable **System.Text.Json source generators** for AOT/trimming scenarios, Refitter needs to generate a `JsonSerializerContext` class with `[JsonSerializable]` attributes for all DTO types. Source generators require explicit type registration.

### Desired Output
```csharp
[JsonSerializable(typeof(Pet))]
[JsonSerializable(typeof(PetStatus))]
[JsonSerializable(typeof(Order))]
// ... all DTOs
public partial class ApiJsonSerializerContext : JsonSerializerContext { }
```

### Reference
Microsoft Docs: https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/

### Root Cause
**Feature does not exist**. Currently, Refitter generates:
1. **Contracts** (DTOs) — via NSwag's `GenerateDtoTypes`
2. **Interfaces** (Refit APIs) — via `RefitInterfaceGenerator`

There is **no code to generate `JsonSerializerContext` classes**.

### Solution Approach

**Implementation Location**: `src\Refitter.Core\RefitGenerator.cs`

Add a new generator class: `JsonSerializerContextGenerator.cs`

**Algorithm**:
1. **Extract all DTO type names** from generated contracts:
   - Parse contracts string for `public class`, `public record`, `public enum` declarations  
   - Use regex: `public\s+(class|record|enum)\s+(\w+)`
   - Or use Roslyn SyntaxTree to extract type names robustly

2. **Generate context class**:
```csharp
public static class JsonSerializerContextGenerator
{
    public static string Generate(string contracts, RefitGeneratorSettings settings)
    {
        var typeNames = ExtractTypeNames(contracts);
        var sb = new StringBuilder();
        
        foreach (var typeName in typeNames)
        {
            sb.AppendLine($"[JsonSerializable(typeof({typeName}))]");
        }
        
        var contextName = $"{settings.Namespace}JsonSerializerContext";
        sb.AppendLine($"public partial class {contextName} : JsonSerializerContext {{ }}");
        return sb.ToString();
    }
    
    private static IEnumerable<string> ExtractTypeNames(string contracts)
    {
        // Option 1: Regex (fast but brittle)
        var regex = new Regex(@"public\s+(?:class|record|enum)\s+(\w+)");
        return regex.Matches(contracts)
            .Select(m => m.Groups[1].Value)
            .Distinct();
        
        // Option 2: Roslyn (robust but heavier)
        // Parse contracts as SyntaxTree, extract TypeDeclarationSyntax nodes
    }
}
```

3. **Integrate into generation workflow**:
   - Add setting: `GenerateJsonSerializerContext` (bool, default: false)  
   - Call generator in `RefitGenerator.Generate()` after contracts generation
   - Output as separate file: `{Namespace}JsonSerializerContext.cs`

4. **CLI Flag**:
   - Add `--generate-json-serializer-context` option to `src\Refitter\Settings.cs`
   - Map to `RefitGeneratorSettings.GenerateJsonSerializerContext`

### Code Locations

**Files to modify**:
1. `src\Refitter.Core\Settings\RefitGeneratorSettings.cs` — Add `GenerateJsonSerializerContext` property
2. `src\Refitter\Settings.cs` — Add CLI option `--generate-json-serializer-context`
3. `src\Refitter\GenerateCommand.cs` — Map CLI option to settings
4. `src\Refitter.Core\JsonSerializerContextGenerator.cs` — **New file** with generation logic  
5. `src\Refitter.Core\RefitGenerator.cs` — Call generator and output as separate file

**Test files to create**:
1. `src\Refitter.Tests\Examples\JsonSerializerContextTests.cs` — Unit tests following existing pattern

### Recommended Approach

**Phase 1: Core Implementation (CLI only)**  
- Implement with **regex-based type extraction** (simpler, covers 95% of cases)
- Generate single context class with all types
- Add CLI flag and basic tests
- **Effort**: **Medium** — 4-6 hours

**Phase 2: Enhancements (optional)**  
- Use Roslyn for robust type extraction (handles nested types, generics)
- Support generic `ICollection<T>`, `Dictionary<K,V>` registration
- Add option to customize context class name
- **Effort**: **Medium** — 3-4 hours

### Estimated Effort
- **Phase 1 (MVP)**: **Medium** — 4-6 hours  
- **Phase 2 (Polish)**: **Medium** — 3-4 hours  
- **Total**: 7-10 hours

---

## Summary Table

| Issue | Title | Root Cause | Fix Complexity | Estimated Effort |
|-------|-------|------------|----------------|------------------|
| #178 | StringEnumConverter NamingPolicy | Property-level converters block policy; NSwag limitation | Simple (docs) / Medium (templates) | 30 min / 4-6 hrs |
| #175 | OneOf base type missing | NSwag doesn't generate discriminator base class | Medium-Complex | 2-3 hrs (test) / 8-12 hrs (fix) |
| #179 | JsonSerializerContext generation | Feature doesn't exist | Medium | 4-6 hrs (MVP) / 7-10 hrs (full) |

---

## Recommendations

### Priority 1: Issue #179 (JsonSerializerContext)
- **Impact**: High — enables AOT/trimming scenarios (modern .NET feature)
- **Feasibility**: High — pure additive feature, no NSwag dependencies
- **Recommendation**: **Implement Phase 1 (CLI MVP)**

### Priority 2: Issue #178 (StringEnumConverter)
- **Impact**: Medium — affects APIs with non-default enum naming (common in REST APIs)
- **Feasibility**: Medium — requires custom templates or better docs
- **Recommendation**: **Document existing workaround**, evaluate custom templates if user demand persists

### Priority 3: Issue #175 (OneOf base type)
- **Impact**: Medium — affects polymorphic APIs (less common)  
- **Feasibility**: Low-Medium — heavily constrained by NSwag behavior
- **Recommendation**: **Investigate NSwag settings first**, escalate to NSwag maintainers if unsolvable

---

## Next Steps

1. **#179**: Create `JsonSerializerContextGenerator` class and add CLI flag
2. **#178**: Add documentation section to README with workaround example
3. **#175**: Test FusionAuth spec with current settings, document findings

---

**Investigated by**: Fenster  
**Date**: 2025-01-28  
**Status**: Investigation Complete
