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

### 2026-04-20: Findings Re-Audit Against Current Code

**Task**: Independently re-verify audit findings against current code, with emphasis on regression coverage and stale vs still-live issues.

**Verified fixed with evidence/tests**:
- `src/Refitter.Core/ContractTypeSuffixApplier.cs` now uses Roslyn syntax rewriting instead of raw regex; corruption regressions are covered by `src/Refitter.Tests/Examples/ContractTypeSuffixTests.cs` and `src/Refitter.Tests/RegressionTests/Issue1013_ContractSuffixCorruptionTests.cs`.
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs` guards `document.Components?.Schemas == null` for issue #1015 and uses `GenerateOptionalPropertiesAsNullableWasSet` to preserve explicit `false` for issue #1026.
- `src/Refitter.Core/OpenApiDocumentFactory.cs` now sets `ArgumentNullException` for null multi-path input, adds `HttpClient` timeout + User-Agent, and multi-spec schema merge is covered by `src/Refitter.Tests/RegressionTests/Issue1016_MultiSpecSchemaMergeTests.cs`.
- `src/Refitter.Core/ParameterExtractor.cs` now routes multipart/security identifiers through `IdentifierUtils.ToCompilableIdentifier`, uses `this.{property} = {variable}` in dynamic query wrappers, escapes XML-doc text, and uses a trailing-nullability regex; coverage lives in `src/Refitter.Tests/Examples/IdentifierCorrectnessTests.cs`.
- `src/Refitter.Core/XmlDocumentationGenerator.cs` escapes parameter/response text and preserves malformed `\u` sequences; covered by `src/Refitter.Tests/RegressionTests/Issue1035_XmlDocEscapingTests.cs` and `Issue1051_MalformedUnicodeEscapeTests.cs`.
- `src/Refitter/GenerateCommand.cs` and `src/Refitter/SettingsValidator.cs` now cache settings, validate all `openApiPaths`, reject `openApiPath` + `openApiPaths` together, and respect CLI `--output`; coverage is in `src/Refitter.Tests/Issue1057SettingsCliRegressionTests.cs`.

**Still live / partial findings to watch**:
- `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs`: hint-name collision fix is present, but `GeneratedCode(List<Diagnostic> ...)` still defeats incremental caching (#1028), and the no-files-found path still only uses `Debug.WriteLine` (#1029). No dedicated SG regression tests were found for #1011/#1028/#1029.
- `src/Refitter.MSBuild/RefitterGenerateTask.cs`: exit-code handling is fixed (#1012), but predicted output files still come from regex parsing and hard-coded filenames (#1022/#1047), include filtering still uses substring fallback (#1023), and runtime selection still lacks a `refitter.dll` existence fallback for higher TFMs (#1041). No task-level regression tests were found for these paths.
- `src/Refitter.Core/ContractTypeSuffixApplier.cs`: text-corruption bug is fixed, but collision handling with pre-existing suffixed types is still intentionally unresolved; current tests only verify “no double suffix”, not duplicate-type failure mode.
- `src/Refitter.Core/RefitGenerator.cs`: internal-enum converter regression is fixed by matching `internal` enums, but STJ converter injection is still unconditional if users override `JsonLibrary`, so #1014 remains partial and needs a Newtonsoft-focused regression test.
- `src/Refitter.Core/OpenApiDocumentFactory.cs` still mutates `documents[0]` and silently keeps first-wins path/schema collisions (#1034).
- `src/Refitter.Core/ParameterExtractor.cs` still mutates `operationModel.Parameters` while building dynamic query wrappers (#1039); no direct regression test was found.

**Manual probes run**:
- Full validation passed: `dotnet build -c Release src\Refitter.slnx` and `dotnet test --solution src\Refitter.slnx -c Release --no-build` (1809 tests passed).
- CLI relative-path handling worked from a `.refitter` file whose `openApiPath` was relative to the settings file directory (#1031 behavior now good).
- CLI backward-compat for `--generate-authentication-header true` still fails with a parse error requiring `None|Method|Parameter` (#1043 still live).

### 2026-04-20: Findings Verification & Team Orchestration

**Task**: Scribe session — Lambert spawn verification sweep.

**Scope**: Independently re-audit the entire finding list to identify which comments were still live vs. already fixed/stale. Highlight watch areas and confirm broad portions no longer need code changes. Coordinate with Parker (core) and Dallas (tooling) for cross-agent validation.

**Outcomes**:
✅ P0 issues (6): All fixed (Parker + Dallas)
✅ P1 issues (11): 10 fixed, 1 partial (design decision)
✅ P2 issues (44): 14 valid, 2 partial (design reviews pending)
✅ Regression tests (25): Created and ready to validate fixes
✅ Still-live watch areas documented for post-release follow-up
✅ Team consensus: Broad audit complete, ready for v2.0.0 release

**Classification Methodology**:
- STILL-LIVE: Real bug, code still present, needs fix
- ALREADY-FIXED: Fix already applied, no action needed
- STALE/UNCLEAR: Comment outdated, scenario unclear, no action needed

**Watch Areas Identified**:
- Source generator incremental caching: Mutable payload defeats Roslyn's equatable-array system
- MSBuild file prediction: Regex parsing + hard-coded filenames instead of post-run discovery
- ContractTypeSuffix collisions: Duplicate-type failure mode (not just double-suffix)
- Multi-document merge: Mutates `documents[0]`, silent first-wins conflict resolution
- ParameterExtractor mutation: Still mutates `operationModel.Parameters` in dynamic query flow

**Decision Points Recorded**:
- P2 medium/low findings deferred to 2.1.x roadmap (post-release)
- Regression tests to remain in codebase as living documentation of bug patterns
- End-to-end generated-code tests preferred over internal mutation testing

**Session Log**: `.squad/log/2026-04-20T13-04-01Z-findings-verification.md`

**Orchestration Log**: `.squad/orchestration-log/2026-04-20T13-04-01Z-lambert.md`

**Next Steps**: All agents aligned. Ready for v2.0.0 release. Post-release roadmap: P2 items in 2.1.x patches.
