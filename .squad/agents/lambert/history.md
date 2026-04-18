# Lambert History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Reproduced on clean .NET 10 build. Output.cs written to project root instead of Generated folder. Settings honored, but file path logic broken. Non-default folders work. First build fails due to sync mismatch; second build succeeds. Specific to default single-file output path behavior.

### 2026-04-17: Release Compatibility Validation + Tie-Break Repro

**Task**: Validation audit for 1.7.3 → HEAD breaking changes, plus concrete reproduction of flagged issues.

**Type Change Validation: `GenerateAuthenticationHeader` (bool → enum)**

**Location**: `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`

**Change**:
- **1.7.3**: `public bool GenerateAuthenticationHeader { get; set; }`
- **HEAD**: `public AuthenticationHeaderStyle AuthenticationHeaderStyle { get; set; }`

**Concrete Test Results**:
- Created `test-deser/Program.cs` to test deserialization
- **Test:** `"generateAuthenticationHeader": true` → deserializes to `AuthenticationHeaderStyle.None` (wrong!)
- **Test:** `"generateAuthenticationHeader": false` → deserializes to `AuthenticationHeaderStyle.None` (wrong!)
- **Test:** `"authenticationHeaderStyle": "Method"` → deserializes to `AuthenticationHeaderStyle.Method` (correct)

**Root Cause Analysis**:
- Property name changed: `GenerateAuthenticationHeader` → `authenticationHeaderStyle`
- JSON serializer uses camelCase policy (`Serializer.cs:17`)
- Old JSON key `generateAuthenticationHeader` doesn't match new property name
- Unrecognized keys silently ignored; property gets default value (`None`)

**Generation Behavior**:
- CLI generation with old key succeeds **WITHOUT error or warning**
- BUT: Setting is silently ignored—no authentication headers generated
- Users get **wrong output without any indication** (extremely dangerous silent failure)

**Build Validation**:
- ✅ `dotnet build -c Release` succeeded
- ✅ Generated code from 1.7.3-compatible `.refitter` file
- ✅ No compilation errors

**Test Coverage Expansion** (230 new files):
- New test suites: `ContractTypeSuffixTests`, `GenerateJsonSerializerContextTests`, `PropertyNamingPolicyTests` (multiple variants), authentication header generation tests

**Obsolete Properties (Non-Breaking Deprecation)**:
- `DependencyInjectionSettings.UsePolly` → `TransientErrorHandler`
- `DependencyInjectionSettings.PollyMaxRetryCount` → `MaxRetryCount`
- Both marked `[Obsolete]` with `[ExcludeFromCodeCoverage]` — no breaking change, just warnings

**Tie-Break Conclusion**: **BREAKING CHANGE CONFIRMED**. Silent failure of old `generateAuthenticationHeader` key is worse than explicit error — users will ship broken code.

**Required Actions**:
1. Document as BREAKING CHANGE in CHANGELOG
2. Bump to major version (2.0.0)
3. Add migration guide with search/replace instructions
4. Update all example files in repo (test/petstore.refitter still uses old key)
5. Consider adding compatibility shim (custom JSON converter) to warn users

### 2026-04-18: v2.0 P1 Audit Verification

**Task**: Verify 11 P1 (High) issues from v2.0 audit against current codebase.

**Key File Paths**:
- `src/Refitter.Core/JsonSerializerContextGenerator.cs` — AOT context generation
- `src/Refitter.Core/ParameterExtractor.cs` — parameter name sanitization, security headers, dynamic querystrings
- `src/Refitter.Core/IdentifierUtils.cs` — identifier validation and sanitization utilities
- `src/Refitter.Core/StringCasingExtensions.cs` — casing helpers (CapitalizeFirstCharacter)
- `src/Refitter/GenerateCommand.cs` — CLI output path resolution
- `src/Refitter.MSBuild/RefitterGenerateTask.cs` — MSBuild task output prediction and file filtering
- `src/Refitter.SourceGenerator/Refitter.SourceGenerator.csproj` — NuGet dependency configuration
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs` — auto-enabling settings

**Bug Patterns Found**:

1. **Regex-Based Type Discovery** (Issue #1017):
   - Pattern: Using regex to re-parse emitted C# code instead of using NSwag's type symbols
   - Impact: Misses generics, namespaces, nested types, polymorphic types
   - Location: `JsonSerializerContextGenerator.cs:48-73`

2. **Incomplete Identifier Sanitization** (Issues #1018, #1019):
   - Pattern: Custom sanitization that doesn't use existing `IdentifierUtils.ToCompilableIdentifier`
   - Impact: Produces invalid C# identifiers (leading digits, reserved keywords)
   - Locations: `ParameterExtractor.cs:106,154-170,583-602`

3. **Self-Assignment Due to Capitalization No-Op** (Issue #1020):
   - Pattern: `CapitalizeFirstCharacter("_foo")` returns `"_foo"` unchanged; property name == variable name
   - Impact: Constructor self-assigns, property never set, query parameter silently dropped
   - Location: `ParameterExtractor.cs:433,443` + `StringCasingExtensions.cs:39-45`

4. **CLI Override Ignored** (Issue #1021):
   - Pattern: Settings file defaults applied unconditionally, CLI flags not checked
   - Impact: `-o` flag silently ignored when using settings file
   - Location: `GenerateCommand.cs:665-679,691-694`

5. **Null-Reference on Valid Input** (Issue #1027):
   - Pattern: No null check before accessing `response.Content.Keys`
   - Impact: NRE on 204 No Content or error responses (valid OpenAPI)
   - Location: `RefitInterfaceGenerator.cs:262`

6. **Substring Pattern Matching** (Issue #1023):
   - Pattern: `IndexOf(pattern) >= 0` for file filtering
   - Impact: Pattern "pet" matches "mypet.refitter" (over-inclusion)
   - Location: `RefitterGenerateTask.cs:318-319`

7. **Unconditional Setting Override** (Issue #1026):
   - Pattern: Force-enable setting when related setting is true, no tri-state to detect explicit user choice
   - Impact: Silent breaking API shape change
   - Location: `CSharpClientGeneratorFactory.cs:69-71`

**Findings Summary**:
- 10/11 issues VALID (exist in current code)
- 1/11 issue PARTIAL (#1024 — design decision, not bug, but needs documentation)
- 0/11 issues INVALID
- All critical correctness issues confirmed (will produce non-compiling C# or NRE)
- All silent behavior changes confirmed (no error, wrong output)
