# Raw Property Name Support — Test Design

## Question 1: C# Compilation & System.Text.Json Behavior

**Test:** Create class with raw snake_case property and `[JsonPropertyName]` attribute.

```csharp
[JsonPropertyName("payMethod_SumBank")]
public double? payMethod_SumBank { get; set; }
```

**Expected:** Compiles without warnings; JSON deserialization maps correctly.

**Result:** ✅ PASS — Tested on net10.0. Compilation succeeds. Serialization/deserialization verified with standalone console app.

---

## Question 2: Edge Cases & Safety

| Scenario | Valid C# ID? | Safe? | Mitigation |
|----------|------|------|-----------|
| Snake_case: `payMethod_SumBank` | ✅ Yes | ✅ Yes | None required |
| Kebab-case: `pay-method-sum` | ❌ No | ❌ No | Validation: reject or sanitize |
| Spaces: `pay method` | ❌ No | ❌ No | Validation: reject or sanitize |
| Reserved keyword: `class` | ✅ (with @) | ⚠️ Warn | Use `@class` prefix auto-insertion |
| Unicode: `café_name` | ✅ Yes | ✅ Yes | No action (UTF-16 native support) |

**Key:** Must validate that OpenAPI-provided property names map to valid C# identifiers, or sanitize them.

---

## Question 3: Recommended Test Suite

If feature is enabled, add to `src/Refitter.Tests/Scenarios/RawPropertyNameGeneratorTests.cs`:

### Test 1: Basic Generation
```csharp
[Test]
public async Task Can_Generate_Code_With_Raw_Property_Names()
{
    var spec = @"{ components: { schemas: { Item: { properties: {
        payMethod_SumBank: { type: number }
    }}}}}";
    var code = await GenerateCode(spec);
    code.Should().NotBeNullOrWhiteSpace();
}
```

### Test 2: Property Naming Verification
```csharp
[Test]
public async Task Generated_Code_Preserves_Raw_Property_Names()
{
    var code = await GenerateCode(spec);
    code.Should().Contain("public double? payMethod_SumBank");
    code.Should().NotContain("public double? PayMethodSumBank");
}
```

### Test 3: Compilation
```csharp
[Test]
public async Task Generated_Code_Compiles_With_Raw_Names()
{
    var code = await GenerateCode(spec);
    BuildHelper.BuildCSharp(code).Should().BeTrue();
}
```

### Test 4: Serialization Roundtrip
```csharp
[Test]
public async Task JsonSerializationRoundtrips_With_Raw_Property_Names()
{
    var code = await GenerateCode(spec);
    var json = @"{""payMethod_SumBank"": 123.45}";
    
    // Compile generated code
    var dll = CompileCode(code);
    
    // Deserialize JSON
    var obj = Deserialize(json, dll);
    
    // Serialize back
    var json2 = Serialize(obj);
    
    // Verify JSON field name preserved
    json2.Should().Contain("\"payMethod_SumBank\"");
}
```

### Test 5: Edge Cases
```csharp
[Test]
public async Task Invalid_Property_Names_With_Hyphens_Are_Rejected()
{
    var spec = @"{ components: { schemas: { Item: { properties: {
        'pay-method': { type: string }
    }}}}}";
    
    var action = async () => await GenerateCode(spec);
    await action.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*invalid C# identifier*");
}

[Test]
public async Task Reserved_Keywords_Are_Escaped()
{
    var spec = @"{ components: { schemas: { Item: { properties: {
        'class': { type: string },
        'return': { type: string }
    }}}}}";
    
    var code = await GenerateCode(spec);
    code.Should().Contain("public string @class");
    code.Should().Contain("public string @return");
}
```

---

## Current Configuration (Code State)

| Entity | Status | File |
|--------|--------|------|
| `PropertyNameGenerator` property exists | ✅ Yes | `src/Refitter.Core/Settings/CodeGeneratorSettings.cs:264` |
| Marked `[JsonIgnore]` | ✅ Yes | `src/Refitter.Core/Settings/CodeGeneratorSettings.cs:265` |
| Excluded from serialization tests | ✅ Yes | `src/Refitter.Tests/SerializerTests.cs:36-37, 56-58, 72-73` |
| Used in factory | ✅ Yes | `src/Refitter.Core/CSharpClientGeneratorFactory.cs:33` |
| CLI option exists | ❌ No | N/A |
| JSON schema includes it | ⚠️ Yes (misleading) | `docs/json-schema.json` |

---

## Validation Checklist (for Feature Implementation)

- [ ] CLI option `--property-name-generator` or similar added to Settings
- [ ] JSON schema updated to remove or clarify `propertyNameGenerator` field
- [ ] SerializerTests updated to handle PropertyNameGenerator (or documented why excluded)
- [ ] RawPropertyNameGeneratorTests.cs added with 5+ test cases above
- [ ] Edge-case validation: hyphens, spaces, reserved keywords
- [ ] Compilation verified with BuildHelper
- [ ] JSON serialization roundtrip tested
- [ ] README updated with feature description and limitations
- [ ] All 1400+ existing tests still pass

---

## Reusable Pattern — Refitter Setting Parity

When a new `.refitter` enum setting lands, cover it in three layers:

1. **Serializer binding:** add `SerializerTests` coverage for string enum serialization/deserialization.
2. **CLI mapping:** if the CLI mapping helper is private (for example `GenerateCommand.CreateRefitGeneratorSettings`), use reflection in tests to verify `Settings` → `RefitGeneratorSettings` mapping without changing production visibility.
3. **Source Generator parity:** add `AdditionalFiles\FeatureName.refitter` plus a focused resource spec, then assert generated members via reflection on compiled types instead of snapshotting entire files.

For generated property-name assertions, prefer checking the identifier itself (string containment or reflection) over over-specifying nullability, because NSwag may emit non-nullable value types where tests might otherwise assume `?`.

## Implemented Pattern — Issue #967

For user-facing property-name customization in Refitter:

1. Expose a **serializable enum** (`PropertyNamingPolicy`) on `RefitGeneratorSettings` for CLI + `.refitter` parity instead of trying to serialize `IPropertyNameGenerator`.
2. Resolve precedence in `CSharpClientGeneratorFactory` as:
   - `CodeGeneratorSettings.PropertyNameGenerator` (programmatic override),
   - otherwise `PropertyNamingPolicy`.
3. For preserve-original behavior, generate identifiers by:
   - returning already-valid identifiers unchanged,
   - escaping reserved keywords with `@`,
   - replacing invalid character runs with `_` and prefixing `_` when needed for compilable output,
   - de-duplicating sibling collisions with `IdentifierUtils.Counted`.
4. Remove any JSON schema entry that exposes a non-serializable interface-based setting (`propertyNameGenerator`) once the enum replacement exists.
