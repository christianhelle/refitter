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
