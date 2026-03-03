# Tier 2 Investigation: Enum & .NET 8 Issues (#300, #310)

## Issues Summary
- **#300**: Hyphens in enum names not handled by JsonStringEnumConverter
- **#310**: Source generator fails in .NET 8 builds

---

## #300: Enum Hyphens with JsonStringEnumConverter

### Problem Statement
System.Text.Json's `JsonStringEnumConverter` cannot deserialize enum values with hyphens, underscores, or special characters. The generated code includes:
- `[JsonConverter(typeof(JsonStringEnumConverter))]` on enum properties
- `[System.Runtime.Serialization.EnumMember(Value = @"allegro-pl")]` on enum members

However, `JsonStringEnumConverter` ignores `EnumMember` attributes, causing JSON values like `"allegro-pl"` to fail deserialization.

### Root Cause Analysis

**Code Generation Flow:**
1. NSwag generates contracts with `JsonStringEnumConverter` attributes (via `CSharpClientGeneratorFactory.cs`)
2. Enum properties get `[JsonConverter(typeof(JsonStringEnumConverter))]` inline attributes
3. Enum members get `[EnumMember(Value = "...")]` for OpenAPI values
4. **Problem**: `JsonStringEnumConverter` doesn't respect `EnumMember` attributes

**Key Code Locations:**
- **Enum generation**: NSwag templates (external dependency)
- **JsonConverter injection**: `CSharpClientGeneratorFactory.cs:25-48` - Sets `JsonLibrary = CSharpJsonLibrary.SystemTextJson`
- **InlineJsonConverters setting**: `CodeGeneratorSettings.cs:269-275` - Controls whether enum properties get `[JsonConverter]` attributes
- **Attribute removal**: `RefitGenerator.cs:211-229` - `SanitizeGeneratedContracts()` method strips `JsonStringEnumConverter` when `InlineJsonConverters = false`

### Why This Happens
- NSwag generates both `JsonConverter` and `EnumMember` attributes
- STJ's `JsonStringEnumConverter` only looks at enum member names, not `EnumMember.Value`
- The converter expects C# identifier-safe names (no hyphens)
- Users need `JsonStringEnumMemberConverter` (3rd party) which respects `EnumMember` attributes

### Current Workaround
User's PowerShell post-processing script replaces `JsonStringEnumConverter` with `JsonStringEnumMemberConverter`.

### Potential Solutions

**Option 1: Add `inlineJsonConverters: false` support** (Already exists!)
- Setting `codeGeneratorSettings.inlineJsonConverters = false` removes ALL `[JsonConverter]` attributes
- Users can then configure converters globally in `RefitSettings`
- **Downside**: All-or-nothing approach

**Option 2: Custom NSwag liquid template** (Future enhancement)
- Use `codeGeneratorSettings.customTemplateDirectory` (already implemented in v1.0+)
- Override NSwag's enum template to use different converter
- **Complexity**: Requires understanding NSwag template system

**Option 3: Post-generation transformation** (Not implemented)
- Add setting like `enumConverterType: "JsonStringEnumMemberConverter"`
- Replace converter type in generated code
- **Complexity**: Regex replacement in `SanitizeGeneratedContracts()`

### Recommended Approach
**Document existing `inlineJsonConverters: false` workaround** - this already solves the issue!

Users should:
1. Set `codeGeneratorSettings.inlineJsonConverters = false` in `.refitter` file
2. Configure converters globally in Refit settings:
```csharp
var refitSettings = new RefitSettings {
    ContentSerializer = new SystemTextJsonContentSerializer(
        new JsonSerializerOptions {
            Converters = { 
                new JsonStringEnumMemberConverter() // 3rd party package
            }
        })
};
```

---

## #310: Source Generator Does Not Work in .NET 8

### Problem Statement
User reports source generator produces zero output in .NET 8 projects, but works in .NET 7. No generated files, no compilation errors, complete silence.

### Root Cause Analysis

**Source Generator Architecture:**
- **Project**: `Refitter.SourceGenerator.csproj`
- **Target Framework**: `netstandard2.0` (line 4)
- **Generator Type**: `IIncrementalGenerator` (modern API)
- **Package Output**: `analyzers/dotnet/cs` folder

**Key Code Locations:**
- **Generator entry**: `RefitterSourceGenerator.cs:12-26` - Implements `IIncrementalGenerator`
- **Framework target**: `Refitter.SourceGenerator.csproj:4` - Uses `netstandard2.0`
- **Incremental pipeline**: `RefitterSourceGenerator.cs:20-23` - Filters `.refitter` files

### Investigation Findings

**Framework Compatibility:**
- Source generator targets `netstandard2.0` ✅ (compatible with .NET 8)
- Uses `IIncrementalGenerator` ✅ (modern API, supported in .NET 8)
- No direct .NET 8 API incompatibilities in code
- Package structure correct: `analyzers/dotnet/cs` path ✅

**Possible Causes (from issue comments):**

1. **Build server caching issue** (mentioned by maintainer)
   - Solution: `dotnet build-server shutdown`
   - Common issue with source generators

2. **First-build dependency issue** (mentioned by maintainer)
   - Source generators create files that project depends on
   - First build fails, subsequent builds succeed
   - Visual Studio 2022 handles this better than CLI

3. **Missing diagnostics** (code observation)
   - Source generator emits `REFITTER001` info diagnostics
   - If diagnostics aren't showing, generator may not be running at all

4. **.NET 8 SDK changes**
   - Possible changes in analyzer/source generator loading
   - May require package/project rebuild

### Not a Code Issue
Based on investigation:
- No .NET 8-specific API usage that would fail
- No deprecated APIs
- Framework targeting is correct
- Generator implementation follows standard patterns

### Recommended Solution

**Test Case Strategy:**
1. Create minimal .NET 8 project with source generator
2. Add verbose MSBuild logging: `dotnet build -v diag`
3. Check if generator assembly loads
4. Verify `.refitter` file is detected as `AdditionalText`
5. Confirm diagnostics are emitted (REFITTER001)

**Documentation Needs:**
- Add troubleshooting guide for source generator
- Document `dotnet build-server shutdown` requirement
- Explain first-build dependency issue
- Show how to enable diagnostic logging

**Potential Fix (if confirmed as real issue):**
- Update NuGet package dependencies (NSwag, H.Generators.Extensions)
- Test with latest .NET 8 SDK
- Consider multi-targeting source generator project

---

## Test Case Strategy

### For #300 (Enum Hyphens)

**Test Case 1: Enum with hyphens + inlineJsonConverters: true**
```yaml
enum:
  - "foo-bar"
  - "baz-qux"
```
Expected: 
- Generated enum has `[EnumMember(Value = @"foo-bar")]`
- Property has `[JsonConverter(typeof(JsonStringEnumConverter))]`
- Build succeeds

**Test Case 2: Enum with hyphens + inlineJsonConverters: false**
```yaml
enum:
  - "foo-bar"
  - "baz-qux"
```
Expected:
- Generated enum has `[EnumMember(Value = @"foo-bar")]`
- Property does NOT have `[JsonConverter]` attribute
- Build succeeds

**Test Case 3: Real-world Allegro API scenario**
Use actual `swagger.yaml` from https://developer.allegro.pl/swagger.yaml
- Test `MarketplaceId` enum generation
- Verify `allegro-pl`, `allegro-cz` values

### For #310 (.NET 8 Compatibility)

**Test Case 1: Basic .NET 8 source generator**
- Create minimal .NET 8 project
- Add Refitter.SourceGenerator package
- Add simple `.refitter` file
- Build and verify code generation

**Test Case 2: .NET 8 incremental build**
- Clean build
- Second build (incremental)
- Verify generator still runs

**Test Case 3: Diagnostic output validation**
- Build with `-v diag`
- Confirm REFITTER001 diagnostics appear
- Verify `.refitter` file detected

**Test Case 4: Build server caching**
- Build with source generator
- Run `dotnet build-server shutdown`
- Build again
- Verify behavior

---

## Code Locations Summary

### #300 Related
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs:25-48` - JSON library configuration
- `src/Refitter.Core/Settings/CodeGeneratorSettings.cs:269-275` - InlineJsonConverters setting
- `src/Refitter.Core/RefitGenerator.cs:211-229` - SanitizeGeneratedContracts (strips JsonConverter)
- `src/Refitter.Tests/Examples/InlineJsonConvertersTests.cs` - Existing test for this feature

### #310 Related
- `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs:12-26` - Generator implementation
- `src/Refitter.SourceGenerator/Refitter.SourceGenerator.csproj:4` - Framework targeting
- `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs:62-71` - Diagnostic emission

---

## Estimated Effort

### #300: Enum Hyphens
**Complexity**: **Simple**
- Feature already exists (`inlineJsonConverters: false`)
- No code changes needed
- **Effort**: Documentation update only (30 minutes)
- Add test case with hyphenated enums (1 hour)
- Update README with workaround example (30 minutes)
- **Total**: 2 hours

### #310: .NET 8 Compatibility
**Complexity**: **Medium** (investigation/documentation)
- Likely environmental/caching issue, not code bug
- Requires validation across different environments
- **Effort**: 
  - Create reproduction test project (1 hour)
  - Test with different .NET 8 SDK versions (1 hour)
  - Document troubleshooting steps (1 hour)
  - Add regression test if issue confirmed (2 hours)
- **Total**: 5 hours

---

## Recommendations

### Immediate Actions
1. **#300**: Document `inlineJsonConverters: false` workaround in README and issue
2. **#310**: Request more details (SDK version, verbose build logs, diagnostics output)

### Test Implementation Priority
1. **High**: #300 test with hyphenated enums (validates existing feature works)
2. **Medium**: #310 .NET 8 regression test (validates no framework compatibility issue)

### Follow-up Investigations
1. Test NSwag custom template approach for #300
2. Monitor .NET 9 SDK changes for source generator compatibility
3. Consider adding enum converter type configuration option
