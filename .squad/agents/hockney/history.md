# Hockney — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** C# / .NET, TUnit, FluentAssertions  
**Repo root:** C:/projects/christianhelle/refitter  
**Solution:** src/Refitter.slnx  

My domain: src/Refitter.Tests/, src/Refitter.SourceGenerator.Tests/.

Test pattern: class in Refitter.Tests.Examples, const OpenAPI spec string, async [Test] methods using GenerateCode(), assertions via FluentAssertions, BuildHelper.BuildCSharp(code).Should().BeTrue() to verify generated code compiles.

Test resources (OpenAPI specs): src/Refitter.Tests/Resources/V2/ and V3/. Use SwaggerPetstore.json for general testing.

Run tests: dotnet test -c Release src/Refitter.slnx (~5 min — NEVER cancel). Network test failures in sandboxed environments are acceptable.

**Framework Details:** TUnit 1.15.11 + FluentAssertions 8.8.0, targets net10.0. BuildHelper shells out to dotnet build for compilation verification. Test scale: 103 files, 58 in Examples/, ~609 [Test] methods.

**2025 Work:** 103-file test suite audit; identified 40+ well-covered features (collection formats, polymorphic serialization, DI, Apizr, multiple interfaces, filtering, schema trimming, inheritance). Implemented 31 new tests across 8 files to fill coverage gaps in JsonSerializerContextGenerator, SchemaCleaner discriminators, RefitGenerator multiple files, ParameterExtractor edge cases, CSharpClientGeneratorFactory integer format handling, OpenApiDocumentFactory multi-file merge strategy, and CustomCSharpTypeResolver numeric type mapping.

## Learnings

### 2026-03-26 — Issue #967 Recursive-schema regression planning

- Current #967 coverage is limited to acyclic behavior: `PropertyNamingPolicyTests.cs` covers PascalCase defaults plus `PreserveOriginal` identifier preservation/escaping/sanitization/build, while `GenerateCommandTests`, `SettingsTests`, and `SerializerTests` only cover binding/defaults/JSON for the enum.
- `ExcludedTypeNames` has only a Petstore smoke assertion in `CustomCSharpGeneratorSettingsTests.cs`; there is no test that combines `PreserveOriginal`, `ExcludedTypeNames`, and a recursive schema, and no test that exercises the settings-file path used by MSBuild.
- Source-generator parity is missing in the current tree even though `src\Refitter.SourceGenerator.Tests\Resources\PropertyNamingPolicy.json` is embedded; there is no matching `AdditionalFiles\PropertyNamingPolicy.refitter` or source-generator test class.
- `CSharpClientGeneratorFactory.ProcessSchemaForMissingTypes()` and `ProcessSchemaForIntegerType()` both recurse without a shared visited-set, so any self-reference, mutual reference, array-item cycle, additional-properties cycle, or discriminator-induced `allOf` back-edge can escape the current suite.
- Manual CLI repro with a tiny self-referential `.refitter` configuration using `propertyNamingPolicy: PreserveOriginal` and `excludedTypeNames` failed to complete and had to be stopped, which is consistent with the unbounded traversal path.

### 2026 Recent Work — Issue #944 Unicode XML Documentation

Added regression test coverage for non-ASCII XML documentation fix:
- Unit test: XmlDocumentationGeneratorTests.cs validates Unicode preservation and escape sequence decoding
- Integration test: GenerateStatusCodeCommentsTests.cs end-to-end spec→generation→compilation check
- Tests confirm fix works across full pipeline; all 1415 tests pass

### 2026 Investigation — Issue #967 Raw Property Name Support

**Findings:** Raw snake_case property names (e.g., `payMethod_SumBank`) are **valid C# and safe with System.Text.Json**. Compiled and tested standalone console app confirming deserialization/serialization work correctly.

**Current State:** Feature infrastructure exists (`CodeGeneratorSettings.PropertyNameGenerator` property, NJsonSchema integration in `CSharpClientGeneratorFactory`) but is NOT exposed to users:
1. No CLI option `--property-name-generator`
2. JSON schema still lists `propertyNameGenerator` despite `[JsonIgnore]` in code
3. SerializerTests explicitly exclude PropertyNameGenerator from round-trip validation

**Safety Assessment:**
- ✅ C# compilation: Valid identifiers, no syntax errors
- ✅ System.Text.Json: Correctly maps via `[JsonPropertyName]` attribute
- ⚠️ Edge cases: Hyphens/invalid chars would break (need validation), reserved keywords require `@` prefix
- ⚠️ Config consistency: PropertyNameGenerator field marked `[JsonIgnore]`, so custom generators won't serialize

**Test Design Recommendations (if feature enabled):**
- Can_Generate_Code_With_Raw_Property_Names()
- Generated_Code_Uses_Raw_Property_Names (assertion: no PascalCase)
- Can_Build_Generated_Code_With_Raw_Names

---

## Issue #967 — Test Coverage Implementation (2026-03-25)

**Status:** ✅ DELIVERED & APPROVED

Added comprehensive regression coverage for property naming feature:
- Core unit tests: PascalCase regression, PreserveOriginal identifiers, keyword escaping, invalid identifier sanitization, compilation checks
- CLI binding: `--property-naming-policy` argument parsing
- Serializer round-trip: Settings deserialization from JSON
- Settings defaults: `PropertyNamingPolicy.PascalCase` verified
- Source Generator end-to-end: `.refitter` file generation matches CLI output

**Test Coverage:** 1468/1468 tests pass (4 issue #967 test files, 4 issue #967 test classes, 30+ test cases)

**Collaborators:**
- Fenster built the implementation
- Keaton reviewed and approved for merge
- Serialization_Preserves_Mapping_With_Raw_Names (JSON roundtrip)
- Invalid_Property_Names_Are_Rejected (hyphens, spaces, etc.)

**Feasibility:** 8.5 hours to fully expose (CLI option, tests, documentation). Feature is technically proven safe; blockers are purely user-exposure and validation design decisions.

## 2026-03-25: Issue #967 Team Assessment

Team consensus reached on GitHub issue #967 (Preserve Original Property Names):
- ✅ APPROVED FOR IMPLEMENTATION
- Safety verdict confirmed: Raw property names compile safely with System.Text.Json
- Recommended test coverage: reserved keywords, invalid identifiers, build success, serialization round-trip
- Edge cases requiring mitigation: hyphens, leading digits, name collisions
- Sanitization strategy: preserve casing/underscores, validate C# identifier legality

Consolidated decision entry created in decisions.md. See orchestration logs for full team assessment.

### 2026-03-25 — Issue #967 Regression Coverage Executed

Implemented end-to-end regression coverage for the shipped `PropertyNamingPolicy` surface:
- `src\Refitter.Tests\Examples\PropertyNamingPolicyTests.cs` verifies default PascalCase output, `PreserveOriginal` raw valid identifiers, reserved keyword escaping, invalid identifier sanitization, and `BuildHelper.BuildCSharp()` compilation.
- `SerializerTests`, `SettingsTests`, and `GenerateCommandTests` now cover enum serialization plus CLI/settings binding for `PropertyNamingPolicy`.
- Source Generator parity is covered with `src\Refitter.SourceGenerator.Tests\AdditionalFiles\PropertyNamingPolicy.refitter` and reflection-based assertions over the generated `PaymentResponse` contract.

**Landed behavior worth remembering:** the preserving generator keeps valid identifiers as-is, prefixes reserved keywords with `@`, and minimally sanitizes invalid names by replacing invalid characters with `_` and prefixing invalid starts with `_`.

**Validation:** `dotnet build -c Release src\Refitter.slnx`, `dotnet test --solution src\Refitter.slnx -c Release --no-build`, and `dotnet format --verify-no-changes src\Refitter.slnx` all passed; full suite count reached 1468 tests.


## Issue #967 — Stack Overflow in Recursive Schema Traversal (2026-03-26)

**Team Execution:** Fenster (implementation) + Hockney (regression) + McManus (CI/harness) + Keaton (architecture/review)

### Fenster's Work
- Root cause: ProcessSchemaForMissingTypes() and ProcessSchemaForIntegerType() in CSharpClientGeneratorFactory.cs recursively traverse schemas without visited-set
- Solution: Replaced duplicated recursive preprocessing with one shared iterative visitor using Stack<JsonSchema> and HashSet<JsonSchema> cycle detection
- Key insight: Pre-existing bug predating PR #969; not caused by property-naming work
- Files: src\Refitter.Core\CSharpClientGeneratorFactory.cs

### Shared Knowledge
- Duplicated recursive traversal pattern was the root of both overflow paths
- Iterative approach with instance-based visited-set matches existing SchemaCleaner pattern in codebase
- netstandard2.0 compatible (no custom equality comparer needed)
- PreserveOriginal + recursive schemas now validated across CLI, MSBuild, and SourceGenerator paths

### 2026-03-26 — Issue #967 Real-World Repro Validation

**User scenario:** Naji Makhoul reported stack overflow with real-world OpenAPI spec (666KB) using `propertyNamingPolicy: PreserveOriginal` setting.

**Validation performed:**
1. ✅ **CLI generation:** Ran Refitter with user's `tmp/api.refitter` + `tmp/api.json` → 18 files generated successfully (53.3 KB, 1,426 lines) in 2.17s
2. ✅ **Stack overflow FIXED:** No crash, completed normally (previous versions would stack-overflow during schema preprocessing)
3. ✅ **Generated code structure:** Multi-file by tag (`multipleInterfaces: ByTag`), 17 interface files + 1 Contracts.cs
4. ✅ **ExcludedTypeNames respected:** All 22 excluded types correctly omitted from generation
5. ✅ **Generated code compiles:** Created test project with stub types for excluded types → clean build (0 errors, 2 warnings about STJ version)
6. ✅ **Full test suite:** All 1,473 tests passed (net8.0 + net10.0), SourceGenerator + CLI parity verified
7. ✅ **Settings file features:** Validated `includePathMatches` filtering, `ignoredOperationHeaders`, `trimUnusedSchema`, `additionalNamespaces`, `returnIApiResponse`

**Not directly exercised:**
- Source generator harness with exact `.refitter` file (would need full project setup with dependencies on excluded types)
- Runtime Refit behavior (generated interfaces are syntactically valid but not integration-tested against real API)

**Residual risk:**
- None identified. Stack overflow root cause eliminated via iterative visitor pattern with cycle detection. User's real spec validates the fix end-to-end.

**Files used:**
- `tmp/email.txt` (user's scenario description)
- `tmp/api.json` (666KB OpenAPI 3.0.4 spec)
- `tmp/api.refitter` (real-world settings: PreserveOriginal, ByTag, excludedTypeNames, path filtering)
- Generated: `tmp/SC_API/*.cs` (cleaned up post-validation)

**Verdict:** ✅ PRODUCTION-READY. Fix handles real-world recursive schemas with complex settings combinations. Ready for preview release 1.8.0-preview.101.

---

## 2026-03-26 Cross-Agent Updates

**From McManus (DevOps):**
- Validated MSBuild/build-surface integration with the same repro bundle
- CLI direct generation: 497 KB output (12,626 lines) in 1.63 seconds ✅
- PreserveOriginal feature test: 3.0 KB metadata in 1.02 seconds ✅
- MSBuild petstore integration: generated, compiled, executed successfully ✅
- Full test suite: 1,473/1,473 tests passed in 37.9 seconds ✅
- No design-time, build-time, or functional caveats identified
- 4 logical commits staged locally on stackoverflow-exception branch; product gate (build/test/format) passed

**Merged Decisions:**
- Orchestration logs written: `.squad/orchestration-log/2026-03-26T23-22-02Z-{hockney,mcmanus}.md`
- Session log written: `.squad/log/2026-03-26T23-22-02Z-tmp-validation.md`
- Decision inbox merged and deduplicated into `.squad/decisions.md`
- Real-world repro validation appended to decision #10 (Issue #967)

**Next Steps:**
- Include fix in preview release 1.8.0-preview.101
- Notify user Naji Makhoul via GitHub issue #967
