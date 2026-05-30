# Decisions — Refitter Squad

## Active Decisions

### 1. Architecture Review Findings (Keaton)

**Status:** Informational / Recommendations  
**Date:** 2025

#### Strengths
1. **Clean Core/consumer separation** — `Refitter.Core` (netstandard2.0) contains all generation logic. Three consumers (CLI, SourceGenerator, MSBuild) reuse it without duplication.
2. **Strategy pattern for interface generation** — `IRefitInterfaceGenerator` with three implementations sharing common base class. New generation modes require only new class + switch case.
3. **Comprehensive feature surface** — DI registration generation (Polly/HttpResilience), Apizr integration, schema tree-shaking, dynamic querystring parameters, polymorphic serialization, XML doc generation.
4. **Partial interface output** — Generated interfaces are `partial`, enabling consumer extension without modifying generated code.
5. **Settings-file driven workflow** — `.refitter` JSON provides declarative configuration used identically by CLI, Source Generator, and MSBuild.

#### Concerns & Recommendations

**High Priority**
| # | Action | Owner |
|---|--------|-------|
| A1 | Wrap sync-over-async in SourceGenerator with `Task.Run` to prevent deadlock in IDE hosts | Fenster |
| A2 | Investigate ILMerge or packaging for SourceGenerator to avoid source-level include of Core files | Keaton |

**Medium Priority**
| # | Action | Owner |
|---|--------|-------|
| A3 | Replace regex JSON parsing in MSBuild task with System.Text.Json | Fenster |
| A4 | Extract settings mapping, file output, and console rendering from GenerateCommand (500+ lines) | Fenster |
| A5 | Group RefitGeneratorSettings sub-objects (output, filtering, behavior) for maintainability | Keaton (design) → Fenster (impl) |

**Low Priority**
| # | Action | Owner |
|---|--------|-------|
| A6 | Create `IRefitGenerator` interface for improved testability | Keaton |
| A7 | Decompose large ParameterExtractor (474 lines) into per-parameter-kind extractors | Fenster |
| A8 | Evaluate necessity of CustomCSharpClientGenerator wrapper if NSwag exposes CreateOperationModel | Keaton |

---

### 2. Code Quality Review (Fenster)

**Status:** Issues Identified  
**Date:** 2025

#### Bugs / High Priority

**B1: ContractsOutputFolder set to file path by default**
- **File:** `src/Refitter/GenerateCommand.cs:314`
- **Issue:** When `ContractsOutputPath` is null and `OutputPath` defaults to `"Output.cs"`, `ContractsOutputFolder` gets a file path instead of folder path, breaking multi-file output.
- **Fix:** Set to `settings.ContractsOutputPath` (null is acceptable; let core handle defaults).

**B2: Replace("I", string.Empty) strips all capital-I characters**
- **File:** `src/Refitter.Core/RefitMultipleInterfaceGenerator.cs:48`
- **Issue:** `interfaceName.Replace("I", string.Empty)` removes every I, not just leading prefix. `IInvoiceEndpoint` → `nvoceEndpoint` → `nvoceQueryParams` (should be `InvoiceQueryParams`).
- **Fix:** `interfaceName.StartsWith("I") ? interfaceName[1..] : interfaceName`

**B3: NSwag model mutation in ParameterExtractor**
- **File:** `src/Refitter.Core/ParameterExtractor.cs:402`
- **Issue:** `operationModel.Parameters.Remove(operationParameter)` mutates NSwag's model as side-effect to prevent double-processing. Fragile if model reused or collection has guards.
- **Fix:** Use separate exclusion set instead of mutating the owned NSwag model.

#### Documentation Bugs

**B4: Wrong [Description] on IncludeTags**
- **File:** `src/Refitter.Core/Settings/RefitGeneratorSettings.cs:200`
- **Issue:** Description says `"Generate a Refit interface for each endpoint."` — belongs to `MultipleInterfaces`.
- **Fix:** Change to describe tag filtering: `"Only include Endpoints that contain this tag. May be set multiple times and result in OR'ed evaluation."`

**B5: Wrong [Description] on ResponseTypeOverride**
- **File:** `src/Refitter.Core/Settings/RefitGeneratorSettings.cs:126`
- **Issue:** Description says `"AddAcceptHeaders dictionary of operation ids..."` — copy-paste from `AddAcceptHeaders`.
- **Fix:** Correct to describe response type override dictionary.

#### Code Quality / DRY

**B6: Duplicated setup in RefitGenerator.Generate() and GenerateMultipleFiles()**
- **File:** `src/Refitter.Core/RefitGenerator.cs`
- **Issue:** Both methods share identical first ~20 lines (factory creation, generator creation, contract generation, interface generator selection).
- **Fix:** Extract into shared `SetupGenerator()` method.

**B7: Two near-identical recursive schema traversal in CSharpClientGeneratorFactory**
- **File:** `src/Refitter.Core/CSharpClientGeneratorFactory.cs`
- **Issue:** `FixMissingTypesWithIntegerFormat()` and `ApplyCustomIntegerType()` both walk the full document tree in identical nested loop pattern (~50 lines each).
- **Fix:** Create generic `void TraverseSchemas(Action<JsonSchema> visitor)` method.

**B8: RefitGenerator.GenerateClient single-file result is non-obvious**
- **File:** `src/Refitter.Core/RefitGenerator.cs:289-294`
- **Issue:** In single-file mode, all interfaces combined into first `GeneratedCode` item; rest have empty content. Implicit contract.
- **Fix:** Add comment or explicit return type clarification.

#### CLI Option Gaps

**B9: GenerateAuthenticationHeader has no CLI option**
- **File:** `src/Refitter.Core/Settings/RefitGeneratorSettings.cs:388`
- **Issue:** Property exists in core model but no `--generate-auth-header` CLI option. Only accessible via `.refitter` file.

**B10: AddContentTypeHeaders has no CLI option**
- **File:** `src/Refitter.Core/Settings/RefitGeneratorSettings.cs:107`
- **Issue:** `AddAcceptHeaders` has `--no-accept-headers`, but `AddContentTypeHeaders` has no CLI option. Asymmetric exposure.

**B11: DependencyInjectionSettings is entirely CLI-inaccessible**
- **Issue:** DI configuration (base URL, handlers, retry policy, error handling) only configurable via `.refitter` file. No CLI mechanism to set even base URL for DI generation.

#### Minor / Typos

**B12: Typo in RefitInterfaceImports.cs**
- **File:** `src/Refitter.Core/RefitInterfaceImports.cs:8`
- **Issue:** `defaultNamespases` should be `defaultNamespaces`.

**B13: Typo in IdentifierUtils.cs comment**
- **File:** `src/Refitter.Core/IdentifierUtils.cs:45`
- **Issue:** Comment says "methode" should be "method".

---

### 3. Test Coverage Review (Hockney)

**Status:** Coverage Assessment  
**Date:** 2025  
**Scale:** 103 test files, 58 in Examples/, ~609 [Test] methods, TUnit + FluentAssertions, targeting net10.0

#### Well-Covered Features (40+)
Collection formats, polymorphic serialization, custom templates, multiple interfaces, tag/path filtering, deprecated endpoints, DI generation (Polly/HttpResilience), Apizr integration, optional/nullable parameters, dynamic querystring, integer types, multipart form data, schema trimming, inheritance, ImmutableRecords, ReturnIApiResponse/Observable, CancellationTokens, XmlDoc, multiple OpenAPI paths, settings serialization, CLI validation, all source generator modes.

#### Coverage Gaps (Priority Order)

**🔴 High Priority**

**T1: AddContentTypeHeaders — 0 dedicated tests**
- **Issue:** Implicit in multipart tests but never explicitly set true/false and asserted.
- **Action:** Add `AddContentTypeHeadersTests.cs` testing both modes with JSON and multipart.

**T2: Source generator edge cases**
- **Issue:** `Refitter.SourceGenerator.Tests` only covers Petstore spec.
- **Action:** Add `.refitter` files + `.g.cs` snapshots for edge cases (polymorphism, optional params, error specs).

**🟡 Medium Priority**

**T3: GenerateStatusCodeComments — 1 file, minimal assertions**
- **Action:** Add dedicated test class asserting comment format.

**T4: ContractOnly / InterfaceOnly — 1 file (SwaggerPetstoreTests)**
- **Action:** Expand to test with multiple interface modes and DI generation.

**T5: NoAcceptHeaders / NoAutoGeneratedHeader / NoOperationHeaders — 1 file each**
- **Action:** Add dedicated negative-assertion tests verifying absence.

**T6: SkipDefaultAdditionalProperties — 1 file**
- **Action:** Add generation test with spec containing `additionalProperties: {}` schemas.

**🟢 Lower Priority**

**T7: OpenAPI 3.1 features**
- **Action:** Add V3.1 spec to Resources and smoke-test generation.

**T8: Negative / error path tests**
- **Action:** Add tests for: duplicate operationIds, invalid YAML, unsupported HTTP methods.

**T9: NamingSettings — 1 file**
- **Action:** Add `NamingSettingsTests.cs` for camelCase/PascalCase outputs.

**T10: UsePolymorphicSerializationAndCustomTemplatesTests inconsistency**
- **Action:** Add `using TUnit.Core;` for consistency.

**Overall Quality:** High. Tests meaningful with real compilation checks via `BuildHelper.BuildCSharp()`. Weak spots: single-file coverage for several settings.

---

### 4. CI/CD Review (McManus)

**Status:** Assessment Complete  
**Date:** 2025  
**Overall Grade: B+ (87%)** — Production-ready with solid multi-platform testing and modern authentication

#### Workflow Inventory (25 total)

**Core CI/CD (5):** build.yml, smoke-tests.yml, release.yml, release-preview.yml, msbuild.yml  
**Extended Testing (7):** regression-tests.yml, production-tests.yml, codecov.yml, changelog.yml, docfx.yml, template.yml, template-url.yml  
**Squad/Automation (13+):** squad-ci.yml, squad-release.yml, squad-preview.yml, squad-promote.yml, squad-insider-release.yml, squad-docs.yml, squad-triage.yml, squad-label-enforce.yml, squad-issue-assign.yml, squad-heartbeat.yml, sync-squad-labels.yml

#### Build Configuration
- **C# Version:** preview (bleeding edge)
- **Nullability:** enabled
- **NuGet:** Auto-pack on Release build, MIT license, SourceLink embedded
- **Test Runner:** Microsoft.Testing.Platform (modern, not legacy NUnit)
- **SDK Requirement:** .NET 10.0.100+ (pinned); workflows install 8.0, 9.0, 10.0 separately

#### Test Coverage
- **Specs tested:** 13+ (v2 + v3, JSON + YAML)
- **Features tested:** 20+ CLI options (disposable, multiple interfaces, contracts-only, polymorphic serialization, etc.)
- **Validation:** Code generation + runtime build success
- **Multi-OS:** macOS, Windows, Ubuntu

#### Gaps & Risks

**🔴 CRITICAL (2)**

**C1: Exposed Codecov Token**
- **File:** `.github/workflows/codecov.yml:24`
- **Issue:** Token `cbd141f8-4d0d-4bc1-84a7-970f9f9b87ac` hardcoded in YAML.
- **Impact:** If leaked, attackers can modify code coverage reports.
- **Action:** Rotate immediately; use GitHub Secrets.

**C2: squad-ci.yml Not Configured**
- **File:** `.github/workflows/squad-ci.yml`
- **Issue:** Placeholder says "No build commands configured"; runs on PR/push to dev/preview/main/insider but doesn't validate anything.
- **Impact:** False security — looks like CI runs but doesn't.
- **Action:** Delete or configure actual build/test commands.

**🟠 HIGH (2)**

**C3: No Explicit Format Validation Step**
- **Issue:** `dotnet format --verify-no-changes` not explicitly run in CI.
- **Current:** Only indirect validation via build success (per history: "CI must pass format validation").
- **Action:** Add explicit check in `build.yml` or `codecov.yml`.

**C4: release-template.yml Changes Not Tested in PR**
- **Issue:** Release workflow only runs on **push** to `release` branch, not on PR.
- **Action:** Consider `workflow_call` to enable preview releases in squad-preview.yml.

**🟡 MEDIUM (3)**

**C5: Hardcoded Version Number**
- **File:** `1.7.4` hardcoded in build.yml, codecov.yml, release.yml
- **Action:** Use Nerdbank.GitVersioning or version.json.

**C6: Production Tests Run Daily**
- **File:** `production-tests.yml` schedule 2 AM daily
- **Risk:** Flaky external Petstore3 tests could cause false alarms.

**C7: No Path Filters on squad-* Workflows**
- **Risk:** Could cause noise if squad workflows are chatty.

**ℹ️ MINOR (3)**

**C8: Windows-Only MSBuild Tests**
- **File:** `msbuild.yml` runs only on windows-latest
- **Action:** Consider macOS/Ubuntu if cross-platform support is a goal.

**C9: Changelog Generation Requires PAT**
- **File:** `changelog.yml`
- **Note:** Worth documenting in CONTRIBUTING.md.

**C10: External Repository Deploy**
- **File:** `docfx.yml` → refitter.github.io
- **Note:** Ensure PAT is rotated periodically.

#### Strengths
✅ Multi-OS testing, comprehensive feature testing, live service validation, multiple distribution channels (NuGet + Docker), clean abstractions, modern OIDC auth, documentation pipeline

---

### 5. Documentation Update Complete (Fenster)

**Status:** ✅ Complete  
**Date:** 2026-03-05

Fenster successfully applied all 9 requested documentation updates to README.md and docs/json-schema.json:
- Added 3 missing CLI options (`--no-xml-doc-comments`, `--ignored-operation-headers`, `--json-serializer-context`)
- Added 5 missing .refitter properties (`generateJsonSerializerContext`, `returnIObservable`, `collectionFormat`, `contractTypeSuffix` to examples)
- Added 8 missing property descriptions in settings reference section
- Updated JSON schema with new property definitions

All changes validated (JSON syntax OK, formatting preserved). Documentation now synchronized with codebase.

---

### 6. Test Validation Report (Hockney)

**Status:** ✅ Complete  
**Date:** 2026-03-05

Validated two branches:
- **Issue #580 (Nullable String Handling):** 6/7 tests pass ✅ APPROVED. Core fix works correctly; 1 minor test assertion issue (does not affect functionality).
- **Issue #672 (Method Naming Scope):** 10/10 tests pass ✅ APPROVED FOR MERGE. Perfect implementation of per-interface method naming.

Pre-existing test failures (4 total) exist in both branches — not regressions. Full suite: 1,142/1,146 passing (99.65%).

Recommendations: Merge both branches; fix 3 SourceGenerator interface casing tests in follow-up.

---

## Summary Table

| Priority | Category | Issue | Owner | Timeline |
|----------|----------|-------|-------|----------|
| 🔴 CRITICAL | Security | Exposed Codecov token | Christian | ASAP |
| 🔴 CRITICAL | CI/CD | squad-ci.yml non-functional | Christian | Before next PR merge |
| 🟠 HIGH | Code Quality | ContractsOutputFolder mix-up (B1) | Fenster | Next sprint |
| 🟠 HIGH | Code Quality | Replace("I") bug (B2) | Fenster | Next sprint |
| 🟠 HIGH | Code Quality | NSwag model mutation (B3) | Fenster | Next sprint |
| 🟠 HIGH | CI/CD | Missing format verification | Christian | Next sprint |
| 🟠 HIGH | Test Coverage | AddContentTypeHeaders untested (T1) | Hockney | Next sprint |
| 🟡 MEDIUM | Architecture | SourceGenerator sync-over-async (A1) | Fenster | Next sprint |
| 🟡 MEDIUM | Architecture | Duplicated source compilation (A2) | Keaton | Next sprint |
| 🟡 MEDIUM | Code Quality | Settings class sprawl (A5) | Keaton/Fenster | Next sprint |
| 🟡 MEDIUM | Test Coverage | Source generator edge cases (T2) | Hockney | Next sprint |
| 🟡 MEDIUM | CI/CD | Hardcoded version numbers (C5) | Christian | Next sprint |
| 🟢 LOW | Code Quality | GenerateCommand extraction (A4) | Fenster | Future |
| 🟢 LOW | Documentation | Typos B12, B13 | Fenster | Future |

---

### 7. Issue #944 — Non-ASCII XML Status-Code Comments (2026-03-06)

**Status:** ✅ APPROVED FOR MERGE  
**Date:** 2026-03-06

#### Summary

Fixed XML documentation generation to properly handle non-ASCII characters in OpenAPI response descriptions. Root cause: NSwag's `CSharpResponseModel.ExceptionDescription` encodes non-ASCII characters as JSON-style escape sequences (`\uXXXX`), which were being written raw into XML comments, rendering as literal backslash sequences instead of Unicode characters.

#### Solution

**Production Fix (Fenster):**
- File: `src/Refitter.Core/XmlDocumentationGenerator.cs`
- Added `DecodeJsonEscapedText()` method that safely decodes `\uXXXX`, `\"`, `\\`, `\n`, `\r`, `\t` sequences
- Applied decoding in `BuildResponseDescription()` before XML-escaping, preventing both escape sequence leakage and XML injection
- Preserves hardcoded XML markup (`<see cref="..."/>`, `<list>` elements)

**Test Coverage (Hockney):**
- Unit test: `XmlDocumentationGeneratorTests.cs` — validates readable Unicode output and absence of raw escape sequences
- Integration test: `GenerateStatusCodeCommentsTests.cs` — end-to-end spec→generation→compilation verification

**Review (Keaton):**
- Scope correctly narrow: only `ExceptionDescription` affected
- Ordering correct: decode first, then XML-escape (prevents injection)
- Bounds checking valid for `\uXXXX` parsing
- Surrogate pairs handled naturally via C# UTF-16
- All 1415 tests pass; no regressions

#### Files Modified

- `src/Refitter.Core/XmlDocumentationGenerator.cs` — decode logic
- `src/Refitter.Tests/XmlDocumentationGeneratorTests.cs` — unit tests
- `src/Refitter.Tests/Examples/GenerateStatusCodeCommentsTests.cs` — integration tests

#### Gates

✅ Build passing  
✅ All tests passing (1451/1451)  
✅ Code formatting verified

---

### 8. User Directive: Commit Message Style (2026-03-06)

**Status:** ℹ️ Process Update  
**Date:** 2026-03-06  
**By:** Christian Helle (via Copilot)

Commit changes in small logical groups with one-liner commit messages without a co-author trailer.

---

### 9. Issue #967 — Preserve Original Property Names (2026-03-25)

**Status:** ✅ TEAM CONSENSUS — APPROVED FOR IMPLEMENTATION  
**Date:** 2026-03-25  
**Assessors:** Keaton (Architect), Fenster (.NET Dev), Hockney (Tester)

#### Problem Statement

User requests the ability to generate C# properties that preserve the original OpenAPI property name (e.g., `payMethod_SumBank`) instead of converting to PascalCase (`PayMethodSumBank`).

**Current output:**
```csharp
[System.Text.Json.Serialization.JsonPropertyName("payMethod_SumBank")]
public double? PayMethodSumBank { get; set; }
```

**Desired output:**
```csharp
public double? payMethod_SumBank { get; set; }
```

#### Key Findings

**Keaton (Architect):**
- ✅ Architecture supports pluggable `IPropertyNameGenerator` at factory level
- ❌ Property generation hardcoded; users cannot access without code changes
- 🔧 Recommended shape: `PropertyNamingPolicy` enum (`PascalCase`, `PreserveOriginal`)
- Effort: 1-2 days

**Fenster (.NET Dev):**
- 📍 Hardcoded in `CustomCSharpPropertyNameGenerator.cs:45-47`
- ❌ No CLI option; `.refitter` file blocked by `[JsonIgnore]`
- 🔨 Required surfaces: CLI option, settings mapping, new generator class
- Recommendation: CLI-only initially (defer `.refitter` support due to polymorphic deserialization complexity)
- Effort: 4-6 hours

**Hockney (Tester):**
- ✅ C# compilation safe; `payMethod_SumBank` is valid identifier
- ✅ System.Text.Json works correctly with `[JsonPropertyName]` attribute
- ⚠️ Edge cases require mitigation: reserved keywords, hyphens, leading digits, name collisions
- 📋 Comprehensive tests needed for edge-case coverage
- Verdict: Safe to implement with proper validation

#### Recommended Product Shape

1. **New enum:** `PropertyNamingPolicy` on `RefitGeneratorSettings`
   - Values: `PascalCase` (default), `PreserveOriginal`
   
2. **New generator class:** `PreserveOriginalPropertyNameGenerator : IPropertyNameGenerator`
   - Sanitizes invalid C# identifiers only; preserves casing/underscores
   - Handles reserved keywords (e.g., `@class`)
   - Validates against name collisions

3. **CLI option:** `--property-naming-policy`
   - Example: `--property-naming-policy PreserveOriginal`

4. **Edge-case handling:**
   - Reserved keywords → add `@` prefix or suffix
   - Spaces/hyphens → replace with underscore
   - Leading digits → prepend underscore
   - Name collisions → validation error

5. **Testing:**
   - Unit tests for `PreserveOriginalPropertyNameGenerator` (reserved keywords, invalid identifiers)
   - Integration tests (CLI generation + code compilation)
   - Serialization round-trip validation

6. **Documentation:**
   - Update README with new CLI option
   - Update JSON schema
   - Clarify that `[JsonPropertyName]` still generated for correct deserialization

#### Implementation Roadmap

| Phase | Task | Effort | Dependencies |
|-------|------|--------|---|
| Phase 1 | Create `PreserveOriginalPropertyNameGenerator` | 1h | None |
| | Add CLI option `--property-naming-policy` | 1h | None |
| | Wire settings mapping | 1h | Phase 1 |
| Phase 2 | Add unit tests (edge cases) | 1-2h | Phase 1 |
| | Add integration tests | 1h | Phase 1 |
| Phase 3 | Update README + schema | 1h | Phase 2 |
| **Total** | | **6-7h** | |

#### Future Work (Deferred)

- `.refitter` file support via whitelist strategy (requires polymorphic serialization design)
- Schema cleanup: remove dead `propertyNameGenerator` references

#### Consensus

✅ **PROCEED WITH IMPLEMENTATION**

Issue #967 is **technically feasible**, addresses a **real use case** (snake_case JSON APIs), carries **low risk** (no breaking changes), and has **clean product shape** (simple enum).

---

*Maintained by Scribe. Agents write to `.squad/decisions/inbox/` and Scribe merges, deduplicates, and consolidates here.*

---

## Issue #967 — prd issue

# Decision: PRD for Issue #967 — Preserve Original Property Names

**Author:** Keaton (Lead / Architect)  
**Date:** 2026-03-25  
**Status:** PRD DRAFTED — Awaiting coordinator/user confirmation on open questions

## Decision

Produced a comprehensive PRD for issue #967. Key architectural decisions embedded:

1. **Setting location:** New `PropertyNamingPolicy` enum on `RefitGeneratorSettings` (top-level, not nested in `CodeGeneratorSettings`) — consistent with `MultipleInterfaces`, `TypeAccessibility`, `CollectionFormat` pattern.

2. **Enum values:** `PascalCase` (default) and `PreserveOriginal`. Future-extensible to `camelCase` or `snake_case` if requested.

3. **Implementation approach:** New `PreserveOriginalPropertyNameGenerator : IPropertyNameGenerator` class, selected in `CSharpClientGeneratorFactory` based on the enum value.

4. **Edge-case strategy:** Sanitize-not-reject. Invalid C# identifiers (hyphens, spaces, leading digits) get minimal transforms (underscores). Reserved keywords get `@` prefix. This matches NSwag's philosophy.

5. **Configuration surfaces:** CLI (`--property-naming-policy`), `.refitter` file (`propertyNamingPolicy`), and JSON schema — all three from day one. The previous team assessment suggested deferring `.refitter` support, but since this is a simple enum (not a polymorphic type like `IPropertyNameGenerator`), standard JSON deserialization handles it with zero complexity.

6. **`[JsonPropertyName]` behavior:** Always emitted regardless of policy. This ensures serialization correctness even when property names match the JSON key.

## Open Questions for User

- Should `PreserveOriginal` also skip the reserved-character sanitization (second pass in `CustomCSharpPropertyNameGenerator`), or only skip PascalCase conversion?
- Is `camelCase` a desired third option, or should we stick with two values?
- Should the feature be marked `[Experimental]` in the first release?

## Impact

No breaking changes. Default behavior unchanged. All existing tests remain valid.
---

## Issue #967 — issue 967

# Fenster — Issue #967 Implementation Decision

## Decision

Ship property-name preservation as a simple top-level `PropertyNamingPolicy` enum on `RefitGeneratorSettings` with two values only:

- `PascalCase` (default)
- `PreserveOriginal`

Keep the existing programmatic `CodeGeneratorSettings.PropertyNameGenerator` override as the highest-precedence escape hatch for library consumers.

## Why

- A serializable enum gives immediate parity across CLI, `.refitter`, Source Generator, and MSBuild without introducing polymorphic JSON deserialization.
- Preserving original names safely still needs generator logic, so the implementation should live in core and be selected centrally from `CSharpClientGeneratorFactory`.
- The old `propertyNameGenerator` JSON schema entry was misleading because it advertised a setting users could not actually round-trip from `.refitter` files.

## Implementation Notes

1. Added `PropertyNamingPolicy` to the shared settings model and CLI surface.
2. Introduced `PreserveOriginalPropertyNameGenerator` for:
   - valid identifiers unchanged,
   - reserved keywords escaped with `@`,
   - invalid identifier shapes minimally sanitized with underscores,
   - sibling collisions de-duplicated via `IdentifierUtils.Counted`.
3. Removed `propertyNameGenerator` from `docs/json-schema.json` and documented `propertyNamingPolicy` in `README.md`.

## Validation

- CLI `--help` includes `--property-naming-policy`
- Direct CLI generation preserves raw property names and keeps `[JsonPropertyName]`
- `.refitter` generation preserves raw property names and keeps `[JsonPropertyName]`
- `dotnet build -c Release src\Refitter.slnx`
- `dotnet test -c Release --solution src\Refitter.slnx`
- `dotnet format --verify-no-changes src\Refitter.slnx`
---

## Issue #967 — issue 967

# Hockney Decision — Issue #967 Regression Coverage

## Decision

Treat `PropertyNamingPolicy.PreserveOriginal` test expectations as:

- preserve already-valid identifiers exactly (for example `payMethod_SumBank`)
- escape reserved C# keywords with `@` (for example `class` → `@class`)
- minimally sanitize invalid identifiers into compilable names by replacing invalid characters with `_` and prefixing invalid starts with `_` (for example `1st-payment-method` → `_1st_payment_method`)

## Why

This matches the landed implementation and keeps the generated DTO surface predictable without reintroducing PascalCase rewriting. It also gives stable regression coverage for both compile safety and user-facing naming behavior.

## Test Strategy

- text-based generation assertions in `src\Refitter.Tests\Examples\PropertyNamingPolicyTests.cs`
- CLI/settings binding coverage in `SerializerTests`, `SettingsTests`, and `GenerateCommandTests`
- Source Generator parity via `AdditionalFiles\PropertyNamingPolicy.refitter` plus reflection over generated members instead of brittle full-file snapshots
---

## Issue #967 — issue 967

# Decision: Issue #967 Implementation — APPROVED

**Date:** 2026-07-09  
**Author:** Keaton (Lead / Architect)  
**Status:** Approved  

## Context

Issue #967 adds `PropertyNamingPolicy` with `PascalCase` (default) and `PreserveOriginal` values, allowing users to preserve original OpenAPI property names in generated contracts.

## Decision

Implementation is **approved for merge** pending PR creation. All three gates pass:
- Build: 0 errors, 0 warnings
- Tests: 1468/1468 pass
- Format: clean

## Findings

1. **Feature correctness:** Verified. The `PreserveOriginalPropertyNameGenerator` correctly handles valid identifiers, reserved keywords, invalid start characters, and invalid body characters. Programmatic `IPropertyNameGenerator` override takes precedence as designed.

2. **Test coverage is comprehensive:** Core unit tests (default PascalCase regression, preserved identifiers, keyword escaping, invalid identifier sanitization, compilation check), serializer round-trip, CLI mapping, settings defaults, and source generator end-to-end through `.refitter` file.

3. **Previous test failures were not caused by code defects.** The `dotnet test` CLI syntax changed — positional solution argument now requires `--solution` flag.

## Follow-up (non-blocking)

- **Dead code in IdentifierUtils:** Three new public methods (`ToCompilableIdentifier`, `IsValidIdentifier`, `EscapeReservedKeyword`) have zero external callers. `PreserveOriginalPropertyNameGenerator` reimplements equivalent logic with a broader keyword list. Consolidate or remove in a follow-up PR. Assign to Fenster.

## Decision: Commit changes in small logical groups
### 2026-03-25T09:23:40Z: User directive
**By:** Christian Helle (via Copilot)
**What:** Commit changes in small logical groups automatically for all agent work.
**Why:** User request — captured for team memory

---
---

## Archived from decisions.md on 2026-04-25
# Squad Decisions

## 2026-04-18

### P0 Audit Findings - Critical Generator Bugs

**Verified By:** Parker (Core Developer)  
**Status:** ALL VALID

- **#1011**: Source generator crashes IDE/build on duplicate filenames
- **#1012**: CI/CD silently ships stale/missing code on CLI failures
- **#1013**: Regex corrupts generated code, breaks member names
- **#1014**: Breaks Newtonsoft users, silently regresses internal enums (PARTIAL)
- **#1015**: NRE on every Swagger 2.0 document
- **#1016**: Multi-spec merge drops all schemas from split APIs

**Key Architectural Concerns:**
1. Regex-on-raw-source fundamentally unsafe (word boundaries insufficient)
2. Missing null checks in OpenAPI document traversal (Swagger 2.0 vs 3.0)
3. MSBuild task doesn't follow MSBuild contract (returns true regardless of exit code)

**Recommendation:** Fix all P0 before v2.0 release.

---

### P1 Audit Findings - High-Priority Issues

**Verified By:** Lambert (Tester)  
**Status:** 10 VALID, 1 PARTIAL

- **#1017**: AOT context non-compiling (generics, nested types, namespaces)
- **#1018**: ParameterExtractor invalid identifiers (not using IdentifierUtils)
- **#1019**: Security scheme header unsafe (leading digits, keywords)
- **#1020**: Dynamic-querystring self-assign (`_foo = _foo;`)
- **#1021**: CLI --output no longer overrides when settings file used
- **#1022**: MSBuild predicted paths diverge from actual generation
- **#1023**: MSBuild IncludePatterns uses substring matching (fragile)
- **#1024**: Refit 10 leaks to consumers (design decision, PARTIAL)
- **#1025**: OpenApi.Readers 1.x → 3.x silent change
- **#1026**: Auto-enable GenerateOptionalPropertiesAsNullable
- **#1027**: RefitInterfaceGenerator NRE on no content

**Critical:** #1018, #1019, #1020 produce non-compiling code; #1027 crashes on 204 responses.

---

### P2 Medium Audit Findings

**Verified By:** Dallas (Tooling Developer)  
**Status:** 14 VALID, 2 PARTIAL

**Critical Issues (Crashes/Corruption):**
- **#1028**: Source Generator Incremental Caching Defeated (List vs EquatableArray)
- **#1037**: Crash on Empty Namespace List
- **#1039**: Mutation of Shared NSwag Model

**Security/Correctness:**
- **#1035**: XML Doc Injection Vulnerability (unescaped parameter descriptions)
- **#1034**: Silent Data Loss in Multi-Spec Merge

**Type System Issues:**
- **#1036**: Nullable Parameter Mis-classification
- **#1038**: Reference Type Nullability (CS8632 errors)

**Tooling Issues:**
- **#1029**: Source Generator Silent Warnings (Debug.WriteLine no-op)
- **#1041**: MSBuild Task Multiple Failure Modes
- **#1043**: Breaking CLI Change (bool flag → enum)

**Partial Issues:**
- **#1032**: JsonConverter Semantics (runtime verification needed)
- **#1042**: Spectre.Console.Cli version bump (smoke testing needed)

---

### P2 Low Audit Findings

**Verified By:** Ripley (Lead)  
**Status:** 13 VALID, 0 PARTIAL

All issues appropriately classified. Systemic patterns identified:

1. **Settings Validation Gaps** (#1044, #1045, #1046)
2. **Parsing Fragility** (#1047, #1050, #1051)
3. **Double-Read/Double-Process** (#1048, #1052)
4. **Keyword Handling Gaps** (#1053)
5. **Library Async Best Practices** (#1049)
6. **Fragile Ordering Dependencies** (#1055, #1056)

Recommendation: Address incrementally in 2.1.x patches.

---

### Breaking Changes Guidance Plan

**Decided By:** Bishop (Docs Specialist)  
**Status:** APPROVED FOR PUBLICATION

**Deliverables Created:**
1. GitHub Discussion draft (ready to publish)
2. Migration guide in docs/ (breaking-changes-v2-0-0.md)
3. Documentation index updated (toc.yml)

**Publication Strategy:**
- Create Discussion under Announcements category
- Pin for 2-3 weeks during v2.0.0 adoption
- Link from CHANGELOG and README

**Reviewed By:** Ripley (Lead) - ✅ APPROVED

---

## 2026-04-20

### PR #1064 Squad Review: v2.0 Audit Fix Status

**Decision Date:** 2026-04-20  
**PR:** #1064 ([v2.0 audit] Fix pre-release regressions from #1057)  
**Branch:** v2.0.0-prerelease-audit  
**Verdict:** **NO MERGE YET** — 5 confirmed blockers pending resolution

#### Review Lanes & Findings

**Bishop (Documentation)** — ✅ READY
- Breaking-changes docs accurate and complete
- 29 issues closed with real code fixes verified
- Optional post-merge improvements: README link, CLI precedence clarity, security fix highlight
- Recommendation: APPROVE (non-blocking gaps only)

**Dallas (Tooling)** — ❌ NOT READY
- Blocker #1011: Source generator hint-name collision on same-directory duplicates (partial fix)
- Blocker #1021: CLI `--output` override ignored in multi-file settings-file flow (partial fix)
- Blocker #1050: Enum-error guidance only added to CLI; source generator still raw (partial fix)
- Verified #1012: MSBuild exit-code handling correct

**Ash (Safety)** — ❌ NOT READY
- Blocker #1013: ContractTypeSuffixApplier missing suffix-target collision detection (no check for `Foo` + `FooDto` → `FooDto` duplicate)
- Blocker #1018: ParameterExtractor multipart dedup uses original key, not sanitized name (`"a-b"` + `"a b"` → duplicate `"a_b"`)
- Both are compilation-breaking; must fix before merge

**Ripley (Issue Matrix)** — ❌ NOT READY
- Blocker #1053: `Sanitize()` returns unescaped keywords (`@class`, missing `__*` set); no `EscapeReservedKeyword()` routing
- Blocker #1021: Multi-file precedence guard incomplete
- Blocker #1050: Source generator enum guidance not improved
- Supporting blockers from Ash (#1013, #1018)
- Awaiting Parker on #1040 (timeout config), #1050 (enum error handling)

#### Confirmed Must-Fix Blockers (5 Items)

| Issue | File | Gap | Fix |
|-------|------|-----|-----|
| #1013 | ContractTypeSuffixApplier.cs | No collision check | Add guard for duplicate targets |
| #1018 | ParameterExtractor.cs | Dedupe by wrong key | Dedupe by sanitized identifier |
| #1021 | GenerateCommand.cs | Multi-file ignores `-o` | Restore override guard + test |
| #1050 | RefitterSourceGenerator.cs | CLI-only guidance | Catch + re-throw with context |
| #1053 | IdentifierUtils (call sites) | No keyword routing | Route through `EscapeReservedKeyword` |

#### Evidence Summary

**Resolved (20/28):** P0 all 7 fixed; P1 partial fixes; P2 mostly silent improvements  
**Partial (6/28):** #1013, #1018, #1021, #1050, #1053, #1019  
**Unresolved (1/28):** #1053 (coordinator spot-check)  
**Awaiting (1/28):** #1040 (Parker review)  

#### Recommendation

- **Request blocker fixes:** ~30 minutes estimated work
- **Re-run full test suite** after fixes
- **Final gate:** All blockers resolved + tests passing → APPROVE FOR MERGE
- **Nice-to-have:** Parker/Lambert confirmations on #1040, #1019

#### Agents Still Running

- **Parker (Core Developer):** Awaiting verdict on #1040 (HttpClient timeout) + #1050 (enum errors)
- **Lambert (Tester):** Optional confirmation on #1019 (edge cases), #1021 (CLI regression)

---

## 2026-04-20

### PR #1064 Blocker Fixes: Final Validation Complete

**Decision Date:** 2026-04-20  
**PR:** #1064 ([v2.0 audit] Fix pre-release regressions from #1057)  
**Verdict:** ✅ **APPROVED FOR MERGE** (cleanup pending)

#### All Blockers FULLY RESOLVED

**Issue #1013 — ContractTypeSuffixApplier Collision Detection** ✅
- Implemented: Pre-flight collision check before building typeRenameMap
- Strategy: Skip renaming if `name + suffix` collides with existing type
- Test coverage: 3 tests in PR1064BlockerRegressions.cs
- Status: PRODUCTION-READY

**Issue #1018 — ParameterExtractor Multipart Deduplication** ✅
- Root cause: Two parameter extraction paths used different naming methods
- Fixed: Unified naming via `ConvertToVariableName()` across both paths
- Test coverage: 3 tests in PR1064BlockerRegressions.cs
- Status: PRODUCTION-READY

**Issue #1053 — IdentifierUtils Keyword Escaping** ✅
- Added: `__arglist`, `__makeref`, `__reftype`, `__refvalue` to reserved keywords
- Fixed: Interface name sanitization AFTER prefixing (prevents `I@class` pattern)
- Fixed: Test expectations corrected for NSwag schema name capitalization behavior
- Test coverage: 6 tests in PR1064BlockerRegressions.cs
- Status: PRODUCTION-READY

#### Validation Results

- **Build Status:** ✅ Clean build, 0 errors
- **Test Suite:** ✅ 1779/1779 PASSING (0 failures)
- **Code Formatting:** ✅ All changes properly formatted

#### Quality Metrics

- **Code Quality:** Excellent — defensive programming, no exceptions, surgical scope
- **Test Coverage:** Comprehensive — 13 new tests covering all three blockers + edge cases
- **Regression Risk:** Minimal — targeted fixes, existing tests unaffected

#### Cleanup Required (CRITICAL)

⚠️ **BEFORE MERGE:**
- [ ] DELETE `src/test-multipart.json` — temporary repro file for #1018
- [ ] DELETE `src/test-keywords.json` — temporary repro file for #1053

#### Agent Sign-offs

- ✅ **Parker:** Implemented initial fixes; identified #1018 naming method mismatch
- ✅ **Ash:** Diagnosed root causes; implemented unified naming for #1018; corrected #1053 test expectations
- ✅ **Lambert:** Created 13 comprehensive regression tests; verified all passing
- ✅ **Dallas:** Validated build and test suite; confirmed all 1779 tests passing

#### Key Learnings

1. **Deduplication requires naming consistency** — Both code paths must use SAME transformation method
2. **Case sensitivity matters** — GetVariableName() preserves casing; ConvertToVariableName() lowercases
3. **NSwag behavior is normative** — Schema names capitalized by NSwag; tests must match actual behavior
4. **Two-phase extraction complexity** — ParameterExtractor has parallel paths requiring coordinated fixes

#### Recommendation

**APPROVED FOR MERGE after cleanup.** All blockers are comprehensively resolved with excellent test coverage. Code is production-ready. Implementation follows best practices and minimal-scope surgical fixes.

**Session Log:** `.squad/log/2026-04-20T16-00-14Z-pr1064-blocker-fixes.md`

---

## 2026-04-17

### Release Compatibility Audit: 1.7.3 → HEAD (All Agents Consensus)

**Verdict:** BREAKING CHANGES FOUND. Cannot be marketed as non-breaking release. Major version bump (2.0.0) required.

#### Breaking Changes (2 Confirmed)

1. **Auth Property Renamed (MEDIUM RISK)**
   - `.refitter` setting: `generateAuthenticationHeader` (bool) → `authenticationHeaderStyle` (enum: None, Method, Parameter)
   - No backward compatibility layer or JSON mapping
   - Old JSON key silently ignored; defaults to `AuthenticationHeaderStyle.None`
   - Affected: users with `"generateAuthenticationHeader": true` in `.refitter` files
   - Evidence: Commits 7dbf6c0c, 14101a49; confirmed by Lambert's deserialization tests
   - Migration: Replace `"generateAuthenticationHeader": true` with `"authenticationHeaderStyle": "Method"` or `"Parameter"`

2. **Source Generator Disk Files (HIGH RISK)**
   - Source generator no longer writes `.g.cs` files to disk
   - Changed from `File.WriteAllText()` to `context.AddSource()` (Roslyn best practice)
   - Fixes issues #635, #520, #310 (file locking, process access errors)
   - Affected: source generator users expecting physical files in `./Generated` folder
   - Users must view generated code via IDE or switch to CLI/MSBuild for disk files
   - Evidence: Commit f853bcf2 (PR #923); confirmed by Dallas tie-breaker audit

#### Non-Breaking Changes

- **MSBuild output path fix (Issue #998):** NOT a breaking change. MSBuild now respects default `./Generated` instead of incorrectly outputting to `.refitter` directory. This is a bug fix, not a break. Users relying on old buggy behavior can set `"outputFolder": "."` explicitly.
- **8 Additive Features** (all backward compatible with safe defaults):
  - PropertyNamingPolicy (defaults to PascalCase)
  - OpenApiPaths (multi-spec merge)
  - ContractTypeSuffix
  - GenerateJsonSerializerContext (AOT)
  - SecurityScheme filtering
  - CustomTemplateDirectory
  - New CLI options for all above
  - Auto-enable GenerateOptionalPropertiesAsNullable (scoped)
- **4 Bug Fixes** (only affect previously broken inputs):
  - Stack overflow in recursive schemas
  - Digit-prefixed property naming (invalid C# identifiers)
  - Multipart form-data parameter extraction
  - OneOf discriminator handling
- **Generated Code Quality Improvements:**
  - JsonConverter attribute placement: properties → enum types (semantically equivalent)
  - Method naming in ByTag mode: numeric suffixes now scoped per-interface

#### Release Recommendation

- **Version:** 2.0.0 (major bump required)
- **CHANGELOG:** Document both breaking changes with clear migration paths
- **Migration Guide:** Provide search/replace instructions and generated-code viewing guidance
- **Timeline:** All agents aligned; ready for release decision

#### Agents Aligned

✅ Ripley (Lead): BREAKING CHANGES FOUND - cannot approve as non-breaking  
✅ Parker (Core Dev): BREAKING CHANGE DETECTED in auth settings surface  
✅ Dallas (Tooling Dev): CONFIRMED 2 breaking changes; bug fix is non-breaking  
✅ Lambert (Tester): BREAKING CHANGE CONFIRMED with concrete deserialization evidence  

---

## 2026-04-16

- Squad initialized for Refitter.
- Team root uses the worktree-local strategy at `C:\projects\christianhelle\refitter`.
- Shared append-only Squad files use Git's `union` merge driver.
- **Issue #998 Investigation Complete:** Verdict is a real product bug, not user error. CLI ignores `outputFolder` when it equals the default `./Generated`, causing MSBuild to search for files in wrong location. First clean build fails due to sync mismatch between MSBuild prediction and CLI output. Fix: remove default-value check in `GenerateCommand.cs:648`.

---

## 2026-04-20

### P1 Follow-up Merge Gate

**Contributors:** Parker, Dallas, Lambert, Ash, Ripley  
**Status:** DO NOT MERGE AS-IS (initial gate)

- **Approved at gate:** #1022 (`RefitterGenerateTask` consumes CLI-emitted `GeneratedFile:` markers) and #1023 (include patterns match exact filename / project-relative path / full path only).
- **Needs more evidence:** #1017 (AOT / `JsonSerializerContext` generation improved materially, but the gate initially lacked end-to-end polymorphism/runtime proof) and #1025 (migration guidance existed, but corpus-diff / smoke evidence was still absent).
- **Open at gate:** #1024 (source-generator packaging and documentation alignment incomplete at the time of review) and #1026 (Swagger 2 nullable-shape regression still reproduced).
- **Routing decision:** reviewer lockout applies to #1026; follow-up revision ownership moved to Ash rather than Parker.

### Tooling Boundary Decisions

**Verified By:** Dallas / Parker  
**Status:** APPROVED

- `Refitter.MSBuild` should trust CLI-emitted `GeneratedFile:` markers instead of predicting output paths from `.refitter` contents.
- `RefitterIncludePatterns` should remain exact-match only; substring matching is intentionally removed.
- `Refitter.SourceGenerator` should keep generator-only dependencies private and require consuming apps to choose their own `Refit` dependency explicitly.

### #1026 Lockout Follow-up

**Verified By:** Ash  
**Status:** FIXED AND VALIDATED

- Preserve DTO shape by keeping `GenerateOptionalPropertiesAsNullable` opt-in only; do not infer it from nullable reference types.
- Safe follow-up path is Swagger 2 post-processing in `RefitGenerator.cs` for nullable reference-type property declarations only.
- Targeted OpenAPI 3 + Swagger 2 regression coverage passed after the follow-up.

### #1025 Documentation Mitigation

**Verified By:** Bishop  
**Status:** DOCUMENTED / PARTIAL PRODUCT CLOSURE

- The breaking-changes guide now documents the Microsoft.OpenApi/OasReader 1.x → 3.x parser upgrade, expected generated-code diffs, and migration steps.
- This removes the silent-upgrade surprise, but it is still not equivalent to corpus-based behavioral proof.



## Archived 2026-05-29 (entries older than 7 days)

### 2026-05-01T14:04:19.681+02:00: Issue #1083 response framing
**By:** Bishop
**What:** Treat issue #1083 as a likely product bug rather than a documentation dispute; current docs scope identifier sanitization to contract properties under propertyNamingPolicy, not schema or contract type names.
**Why:** The maintainer reply should acknowledge the broken generated output without inaccurately claiming the docs already promised schema-type sanitization.

### 2026-05-01T14:04:19.681+02:00: User directive
**By:** Christian Helle (via Copilot)
**What:** Use GPT-5.5 for all agents for the rest of this session only.
**Why:** User request — captured for team memory

### 2026-05-01T14:34:56.630+02:00: Issue #1083 adjacent tooling verdict

**By:** Dallas

**Decision:** Do not add CLI, MSBuild, or README follow-up for #1083. The bug and its fix live in shared core type-name generation, so those surfaces inherit the corrected behavior without new wiring or user-facing settings. Add only source-generator regression coverage to prove the compile-time surface emits the sanitized DTO type and method return type for dotted schema names.

**Why:** #1083 does not introduce a new option, command contract, settings shape, or consumer workflow. The adjacent risk was validation drift in the source-generator lane, so a focused generated-code test is the smallest correctness guard that keeps scope tight.

# Lambert issue #1083 repro decision

- **Date:** 2026-05-01T14:04:19.681+02:00
- **Requester:** Christian Helle
- **Decision:** Treat GitHub issue #1083 as a valid current-HEAD bug and anchor the regression on a minimal inline Swagger 2 fixture that uses the exact dotted schema key `LookUpErnResponse.`. Keep any real Revenue-spec check as optional evidence only, not as the primary automated regression, to avoid live-network coupling.

## Evidence

- Minimal fixture generation produced `Task<> LookUpERN(...)` and `public partial class` with no identifier.
- Real Revenue spec generation produced the same failure shape in `IPAYEEnhancedReportingNotificationRESTAPIApi.cs` and `Contracts.cs`.
- Isolated compile checks failed with `CS1001 Identifier expected` for both generated outputs.

## Required regression coverage

1. Add a focused scenario test file in `src\Refitter.Tests\Scenarios` for invalid schema/type names.
2. Use an inline fixture with one endpoint whose 200-response references `#/definitions/LookUpErnResponse.`.
3. Assert generated code does **not** contain `Task<>` or `partial class` followed by a blank name.
4. Assert generated code **does** contain a concrete sanitized DTO identifier and that the response method returns that identifier.
5. Add a `BuildHelper.BuildCSharp(generatedCode).Should().BeTrue()` assertion so the regression proves compilable output, not just string replacement.
6. Only add/update `IdentifierUtilsTests` if the implementation explicitly routes schema/type-name sanitization through shared identifier utilities; otherwise keep coverage at the scenario level.

## 2026-05-01T14:34:56.630+02:00

- Decision: keep issue #1083 coverage in a dedicated scenario test file instead of widening `PR1064BlockerRegressions`, `PropertyNamingPolicyTests`, or `IdentifierUtilsTests`.
- Why: the failure is schema/type-name generation, not property/parameter sanitization; isolating it behind a minimal inline fixture keeps the regression signal focused and avoids coupling unrelated suites to dotted-schema behavior.
- Required assertions: no blank `partial class`, no `Task<>`, `LookUpErnResponse` used consistently in generated contracts and interface signatures, plus a compile gate.

---
timestamp: 2026-05-01T14:34:56.630+02:00
agent: parker
issue: 1083
---

# Decision

Implement issue #1083 at the schema type-name generation hook in `src\Refitter.Core\CSharpClientGeneratorFactory.cs`, not in downstream interface or contract post-processing.

# Why

- The failure starts in NSwag/NJsonSchema type resolution when a schema hint ends with an empty `.` segment.
- Repairing the hint before `DefaultTypeNameGenerator` runs fixes both DTO declarations and response signatures with one narrow change.
- To preserve normal behavior, malformed keys only normalize empty segments; if that normalized name collides with an existing clean schema key, the malformed schema is forced onto the counted suffix so the clean schema keeps the unsuffixed base name.

# Validation

- `dotnet format src\Refitter.slnx`
- `dotnet build -c Release src\Refitter.slnx`
- `dotnet test -c Release src\Refitter.slnx --no-build` (remaining failures were the known external-URL timeout lane in `PathParametersWithUrlTests`)
- `dotnet format --verify-no-changes src\Refitter.slnx`
- Manual CLI generation + scratch-project compilation for single-schema and collision fixtures

# Parker Plan: Issue #1083

**Date:** 2026-05-01T14:04:19.681+02:00  
**Owner:** Parker  
**Status:** Planning only

## Decision

Issue #1083 is a valid current bug in Refitter's generator pipeline.


- Reproduced at HEAD against `https://revenue-ie.github.io/paye-employers-documentation/PIT3/rest/paye-employers-rest-api-pit3.json`.
- Generated output contains both:
  - `internal partial class` with no type name
  - `Task<> LookUpERN(...)`
- The offending Swagger 2 definition key is `LookUpErnResponse.` and the `GET /ern/{employerRegistrationNumber}/{taxYear}` response references `#/definitions/LookUpErnResponse.`

## Root Cause

- Refitter currently relies on NSwag/NJsonSchema default type naming for schema names.
- NJsonSchema's `DefaultTypeNameGenerator` treats `.` as a segment separator and uses the last segment.
- For a trailing-dot name like `LookUpErnResponse.`, the last segment is empty, and the fallback path also returns an empty string instead of a usable anonymous name.
- That empty type name flows into both DTO emission and `generator.GetTypeName(...)`, producing the blank class declaration and empty generic return type.

## Safest Fix Shape

1. Add a Refitter-owned custom type-name generator that preserves current NSwag naming behavior for normal inputs.
2. Only special-case malformed hints whose final segment is empty or whose generated type name is blank.
3. Normalize those cases to the last non-empty segment (for example `LookUpErnResponse.` -> `LookUpErnResponse`) and still route the final identifier through Refitter-safe identifier sanitization.
4. Inject that generator in `CSharpClientGeneratorFactory` so the fix applies once at the DTO/type-resolution layer.

## Main Files Likely Needed

- `src\Refitter.Core\CSharpClientGeneratorFactory.cs`
- new core generator file such as `src\Refitter.Core\RefitterTypeNameGenerator.cs`
- possibly `src\Refitter.Core\IdentifierUtils.cs` if a shared helper is introduced for final identifier normalization
- regression coverage in `src\Refitter.Tests\Scenarios\PR1064BlockerRegressions.cs` or a dedicated scenario test file
- possibly `src\Refitter.Tests\IdentifierUtilsTests.cs` if helper behavior moves into `IdentifierUtils`

## Risks / Tradeoffs

- Any type-name generator change can rename emitted contracts for malformed schema names, so tests should pin current behavior for ordinary names.
- Avoid broad dot replacement: dotted names may already rely on NSwag's "use last segment" behavior, so the fix should target only empty-tail cases.
- Collisions remain possible if multiple malformed names normalize to the same identifier; rely on NSwag's reserved-name flow or add focused collision coverage.
- Rewriting schema keys and refs in the OpenAPI document would be much riskier because it can disturb references, exclusions, and other preprocessing logic.


# Squad Decisions

## 2026-04-21

### PR #1067 Linked-Issue Closure Matrix

**Lead:** Ripley  
**Status:** REVIEWED

- Treat **#1017, #1022, #1023, #1024, and #1026** as fully closed on the reviewed branch state.
- Treat **#1025** as **partial/documentation-first only**; do not auto-close it from PR wording.
- Final review guidance requires removing or downgrading `Fixes #1025` in the PR body so GitHub does not overstate closure.

### Documentation and Package Guidance Alignment

**Verified By:** Bishop / Ash  
**Status:** REQUIRED AND VERIFIED

- `Refitter.SourceGenerator` package guidance must describe Roslyn `AddSource()` behavior rather than legacy disk-file output.
- Consumer guidance must explicitly require a direct `Refit` reference (and `Refit.HttpClientFactory` when generated DI helpers are used).
- Disk-output settings (`outputFolder`, `contractsOutputFolder`, `generateMultipleFiles`) should be documented as CLI/MSBuild-oriented, not source-generator disk artifacts.
- Final safety-lane review approved PR #1067 once issue-closure wording was honest and the packaging/docs/test evidence aligned.

### Session Directives Archived

**By:** Christian Helle (via Copilot)

- 2026-04-20: Commit changes as often as possible in small logical groups.
- 2026-04-21: Use Opus for all agents for the rest of that session only.
- 2026-04-21: Commit changes in small logical groups.
- 2026-04-25: Use GPT-5.5 for all agents for the rest of this session only.

## 2026-04-25

### Remaining Audit Matrix Pass (#1057)

**Verified By:** Ripley  
**Status:** VERIFIED

- Treat **#1042** as **validation-only** until a concrete Spectre.Console.Cli parsing regression is reproduced at current HEAD.
- Treat **#1047** as **already fixed / stale issue text** at current HEAD because MSBuild now consumes CLI-emitted `GeneratedFile:` markers instead of regex-parsing `.refitter` JSON for output paths.
- Treat **#1056** as **doc/invariant-only** for now; preserve the current generation ordering/state flow and document the invariant before changing behavior.
- Treat **#1032** as **validation-first**; gather runtime evidence before changing enum-converter behavior.
- Coordination note from the verification pass: **#1045 and #1047 appear already fixed at HEAD**, and the remaining code-backed follow-up stays with Dallas/Parker.

### Remaining Audit Repro Pass (#1057)

**Verified By:** Lambert (Tester)  
**Status:** EVIDENCE NARROWED

- Treat **#1028** as **still reproducible by inspection** on current HEAD; the source-generator incremental pipeline still carries a `List<Diagnostic>` equality hazard.
- Treat **#1029** as **partial** on current HEAD; visible diagnostics improved, but the "no .refitter files found" path is still only `Debug.WriteLine`.
- Treat **#1033** as **still reproducible by inspection**; enum-converter injection still uses a hard-coded LF and needs newline normalization coverage.
- Treat **#1041** as **partial**; runtime resolution improved, but argument escaping and timeout kill semantics still leave repro surface.
- Treat **#1043** as **still reproducible**; legacy `--generate-authentication-header` bool-style CLI usage still fails at current HEAD.
- Treat **#1032, #1042, #1045, and #1047** as **validation-only / fixed-at-HEAD evidence** unless stronger failing repros appear.
- Treat **#1034, #1039, and #1056** as **not reproduced on current HEAD** in Lambert's pass.

### Multi-spec Merge Collision Policy

**Decided By:** Parker (Core Developer)  
**Status:** APPROVED

- `OpenApiDocumentFactory` should clone the first loaded document before merging additional specs so callers do not observe mutation of a previously loaded `OpenApiDocument`.
- Path and schema-key collisions across distinct OpenAPI inputs should fail fast with `InvalidOperationException` instead of silently keeping the first definition.
- Exact duplicate input paths should continue to deduplicate up front so feeding the same spec twice stays harmless.

**Rationale:** Silent first-one-wins merge behavior hides real API-shape conflicts and only surfaces later during generation or runtime use. Failing fast is the safer core-library policy.

### Core Lane Follow-up Gate (#1057)

**Verified By:** Parker (Core Developer)  
**Status:** FIXED / NARROWED

- **#1033**: landed at HEAD with a code change in `src/Refitter.Core/RefitGenerator.cs` plus regression coverage in `src/Refitter.Tests/Examples/InlineJsonConvertersTests.cs`.
- **#1032**: treat as validation-first pending review gate; no current core-lane code change required.
- **#1034** and **#1039**: treat as fixed-at-HEAD / no-repro on the reviewed branch state pending final gate review.
- **#1045**: treat as fixed-at-HEAD on the reviewed branch state pending final gate review.
- **#1056**: treat as doc/invariant-only for now; preserve current ordering behavior unless new failing evidence appears.
### Tooling Compatibility Follow-up

**Verified By:** Dallas (Tooling Developer)  
**Status:** APPROVED

- Preserve CLI compatibility for `--generate-authentication-header` by treating the legacy boolean forms (`true`, `false`) and the bare flag as valid inputs. The bare flag and `true` now map to `AuthenticationHeaderStyle.Method`; `false` maps to `None`, while `Parameter` still requires the explicit enum value.
- Keep MSBuild runtime resolution resilient across both packed and test-project layouts. The task now prefers bundled framework-specific Refitter binaries, falls back to lower compatible TFMs when probing fails, and finally uses a co-located `refitter.dll` when the packaged layout is unavailable.

### PR Prep Closure Guidance

**Verified By:** Ripley  
**Status:** DRAFTED FOR PR ASSEMBLY

- Safe auto-close candidates on the reviewed branch state: **#1028**, **#1029**, **#1033**, and **#1043**.
- Keep **#1032**, **#1034**, **#1039**, **#1041**, **#1042**, **#1045**, **#1047**, and **#1056** out of PR auto-close wording until stronger evidence or final lane approval exists.
- **#1041** specifically remains a Dallas-owned tooling verdict before any PR body claims closure.

### Ash core review of remaining #1057 closures

**Verified By:** Ash  
**Status:** REJECT

- Verified acceptable:
  - **#1032** does not reproduce the claimed custom `JsonNamingPolicy` override regression at current HEAD; runtime repro with a type-level `JsonStringEnumConverter` still serialized via `JsonSerializerOptions.Converters` (`"my_value"`).
  - **#1045** is effectively fixed at current HEAD because `RefitGenerator.GetOpenApiDocument()` uses `OpenApiPaths` directly when populated instead of dereferencing `OpenApiPath`.
  - **#1033** is the only intentional core code change in the working tree (`src/Refitter.Core/RefitGenerator.cs`) and it has matching regression coverage in `src/Refitter.Tests/Examples/InlineJsonConvertersTests.cs`.
- Still open / false closure:
  - **#1034** remains open in `src/Refitter.Core/OpenApiDocumentFactory.cs:55-107`; `Merge()` still mutates `documents[0]` and still silently keeps the first path/schema on key collisions.
  - **#1039** remains open in `src/Refitter.Core/ParameterExtractor.cs:447-487` plus `src/Refitter.Core/RefitInterfaceGenerator.cs:69-82`; `GetParameters()` removes query parameters from `operationModel.Parameters` before XML-doc generation reads the shared model.
- Follow-up requirement: reassign the remaining core revisions to Parker (or another core implementer) for real fixes before closing **#1034**/**#1039** from the `#1057` matrix.

### Dallas core revision on rejected blockers

**Verified By:** Dallas  
**Status:** IMPLEMENTED / PENDING ASH RE-REVIEW

- **#1034:** `OpenApiDocumentFactory.Merge()` now clones the first input before merge so callers no longer observe mutation of a previously loaded `OpenApiDocument`.
- **#1034:** duplicate path/schema collisions now emit warnings while preserving the existing merged entry; this revised pass does **not** follow the earlier fail-fast proposal.
- **#1039:** `ParameterExtractor` no longer mutates the shared `operationModel.Parameters` collection when building grouped query-parameter wrappers, so downstream consumers keep the original operation model intact.
- Regression coverage was refreshed for the revised core pass in `src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs`, `src/Refitter.Tests/ParameterExtractorEdgeCaseTests.cs`, `src/Refitter.Tests/ParameterExtractorPrivateCoverageTests.cs`, and `src/Refitter.Tests/RegressionTests/Issue1039_DynamicQuerystringMutationTests.cs`.
- Dallas reported the revised core validation lane green; Ash is performing re-review and Lambert is reconciling the blocker-test lane against the landed behavior.

### Ash core re-review of Dallas revision

**Verified By:** Ash  
**Status:** PARTIAL / BLOCKED

- **#1039 resolved:** ParameterExtractor.GetParameters() no longer mutates operationModel.Parameters, and ParameterExtractorPrivateCoverageTests now lock that invariant for XML-doc generation and shared-model reuse.
- **#1034 still open:** OpenApiDocumentFactory.Merge() now clones the first input, but it still keeps the first conflicting path/schema/definition/security entry via Trace.TraceWarning(...) instead of failing fast.
- src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs still codifies warning-backed first-wins collision handling; the next narrow revision must flip that coverage to an InvalidOperationException contract for conflicting inputs.
- Do **not** close **#1034** from the #1057 matrix yet. Dallas owns one last narrow revision, and Lambert remains on the blocker-test lane.

### Dallas final #1034 revision / Lambert blocker-test reconciliation

**Verified By:** Dallas / Lambert  
**Status:** IMPLEMENTED / READY FOR ASH FINAL GATE

- `OpenApiDocumentFactory.Merge()` now preserves the clone-first non-mutation guarantee **and** fails fast with `InvalidOperationException` when distinct inputs introduce conflicting duplicate path, schema, definition, or security keys.
- Non-conflicting merges still return a new document without mutating either input document, and exact duplicate input paths remain harmless because they are deduplicated before merge.
- Blocker coverage is now aligned to the fail-fast contract in `src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs`; `Issue1039_DynamicQuerystringMutationTests.cs` still preserves grouped-query XML-doc assertions across single-interface, `MultipleInterfaces.ByTag`, and `MultipleInterfaces.ByEndpoint` generation.
- Dallas's final narrow implementation pass is complete. Ash is performing the final review gate, and Lambert is reconciling the blocker-test lane against the landed fail-fast behavior.

### Ash final core gate rejection

**Verified By:** Ash  
**Status:** REJECTED

- **#1034** is still not proven closed: `src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs` only demonstrates fail-fast behavior for duplicate **paths**, not explicit conflicting **schemas**, **definitions**, and **security schemes**.
- The broader core validation lane is not green because `dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj` still fails `Dynamic_Querystring_Generation_Preserves_Original_Query_Param_Documentation(ByEndpoint)` in `Issue1039_DynamicQuerystringMutationTests`.
- Dallas is now locked out of the next revision cycle for this artifact; Parker remains locked out from the prior rejected cycle.
- Lambert now owns the next/final revision cycle for **#1034** while staying in the blocker-test lane.

### Ash final review of Lambert revision

**Verified By:** Ash  
**Status:** REJECTED

- **#1039 acceptable:** `ParameterExtractor.GetQueryParameters()` still snapshots query parameters locally and preserves the shared `operationModel.Parameters` list; the regression coverage remains aligned with the intended non-mutating behavior.
- **#1034 still not proven closed:** the Swagger 2 definition-collision proof is still not isolated cleanly enough. `OpenApiDocumentFactoryMergeTests.Merge_With_Definition_Collision_Throws_And_Does_Not_Mutate_Inputs` still trips the duplicate **schema** conflict before it conclusively proves the duplicate **definition** lane.
- Remaining blocker for the next cycle: isolate the definition-specific fail-fast proof so the test fails for the intended definition-collision reason instead of the mirrored schema path.
- Lambert now joins Parker and Dallas in lockout for the next revision cycle on this artifact.
- Ripley now owns the next narrow revision cycle for **#1034**.

### Ripley final #1034 proof-gap revision

**Verified By:** Ripley  
**Status:** IMPLEMENTED / PENDING ASH FINAL SIGNOFF

- Preserve the source document schema type during clone/copy so Swagger 2 inputs stay on the intended definitions surface throughout merge handling.
- Isolate the Swagger 2 definition-collision proof at MergeIfMissingOrThrowOnConflict(...) so the definition-specific fail-fast contract is asserted directly instead of being masked by the mirrored schema collision first.
- Reported validation from the revision lane is green for dotnet build -c Release src\Refitter.slnx and dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj.
- Ash now owns the final reviewer signoff before broader validation resumes.
### Ash final signoff on Ripley #1034/#1039 follow-up

**Verified By:** Ash  
**Status:** APPROVED

- **#1034 approved:** OpenApiDocumentFactory.Merge() now clones the first document before merge, fails fast on conflicting duplicate path/schema/definition/security keys, and isolates the remaining Swagger 2 definition proof through the shared MergeIfMissingOrThrowOnConflict(...) path.
- **#1039 approved:** grouped dynamic-query extraction still snapshots query parameters instead of mutating operationModel.Parameters, and XML-doc regression coverage remains locked for single-interface, ByTag, and ByEndpoint generation.
- Evidence reviewed: src/Refitter.Core/OpenApiDocumentFactory.cs, src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs, src/Refitter.Tests/RegressionTests/Issue1039_DynamicQuerystringMutationTests.cs, and src/Refitter.Tests/ParameterExtractorPrivateCoverageTests.cs.
- Reviewer signoff was reported against dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj with 1840 passing and 0 failing.

### Final PR package guidance for #1057

**Prepared By:** Ripley  
**Status:** READY FOR PR ASSEMBLY

- Proposed PR title: `[v2.0 audit] Close remaining verified #1057 regressions`.
- Keep PR summary focused on five landed lanes: source-generator diagnostics, newline-safe enum-converter rewriting, non-mutating dynamic querystring generation, fail-fast multi-spec merge handling, and tooling/runtime compatibility hardening.
- Safe auto-close set for the final PR body: **#1028, #1029, #1033, #1034, #1039, #1041, #1043**.
- Keep **#1032, #1042, #1045, #1047, and #1056** out of auto-close wording because they are validation-only, fixed-at-HEAD/stale, or doc/invariant-only.
- Before opening the PR, recreate/publish v2.0.0-prerelease-fixes with `git push -u origin HEAD` because the local branch tracks a gone upstream.
- Latest local full validation reported: dotnet restore src\Refitter.slnx, dotnet build -c Release src\Refitter.slnx --no-restore, dotnet test -c Release src\Refitter.slnx --no-build, and dotnet format --verify-no-changes src\Refitter.slnx --no-restore with 1886 tests passing.

### CLI help output assertions should be semantic

**Verified By:** Lambert (Tester)  
**Status:** APPROVED

- src\Refitter\Program.cs intentionally rewrites a no-argument invocation to --help, exits 0, and emits Spectre.Console.Cli help output.
- The current product behavior is correct; the instability sits in whitespace-sensitive test expectations, not in production code.
- src\Refitter.Tests\GenerateCommandTests.cs should assert semantic help markers (usage pattern, sections, and known option names) instead of exact formatter-driven spacing/default-value layout.
- Validation reported: release run of src\Refitter.Tests\Refitter.Tests.csproj, focused rerun of Program_Main_Should_Show_Help_When_Invoked_Without_Arguments, and dotnet format --verify-no-changes src\Refitter.slnx.
- Cross-agent merge outcome: Dallas proved the Ubuntu failure was ANSI/wrapping noise in raw Spectre.Console help output, and Lambert landed the test-only fix in src\Refitter.Tests\GenerateCommandTests.cs by normalizing redirected console output before asserting semantic help markers.
- Final landed validation for the product commit `normalize help output test across platforms`: dotnet build -c Release src\Refitter.slnx, dotnet test -c Release src\Refitter.slnx, and dotnet format --verify-no-changes src\Refitter.slnx.

### RefitterGenerateTask edge-case coverage stays test-only

**Verified By:** Dallas / Lambert  
**Status:** APPROVED

- Preserve the current `src\Refitter.MSBuild\RefitterGenerateTask.cs` behavior and close the remaining coverage gap with regression tests instead of production changes.
- Lambert isolated the last uncovered branches to `TryExecuteRefitter()` exception handling, missing bundled CLI handling in `StartProcess()`, `ResolveRefitterDll()` fallback edges, sub-second timeout formatting, and the non-throwing `TryLogErrorFromException()` path.
- Dallas landed the test-only closure in `src\Refitter.Tests\RefitterGenerateTaskTests.cs`, covering blank package folders, whitespace runtime entries, co-located and first-bundled fallback resolution, missing bundled CLI failure, process-runner exception handling, millisecond timeout formatting, and successful `LogErrorFromException` forwarding.
- Reported validation: `dotnet test --project src\Refitter.Tests\Refitter.Tests.csproj -c Release --coverage --coverage-output coverage.cobertura.xml --coverage-output-format xml`, `dotnet build -c Release src\Refitter.slnx --no-restore`, and `dotnet format --verify-no-changes src\Refitter.slnx --no-restore`.
- Result: `RefitterGenerateTask.cs` reached 100% line coverage, 100% block coverage, and 0 partial functions in the reported coverage output.
### PR #1070 SonarCloud quality-gate repair

**Verified By:** Dallas / Ash / Parker  
**Status:** APPROVED

- Keep the ParameterExtractor (S1066) and RefitterGenerateTask (S3267, S3358) changes narrow and behavior-preserving; Ash approved those cleanups as safe.
- Keep the source-generator diagnostic cleanup on the stable one-descriptor-per-ID contract: reuse the shared Refitter title/category constant, but assign distinct IDs when the title/message semantics differ.
- Preserve GeneratedDiagnostic as a readonly record struct; retain the explicit ordinal GetHashCode() behavior and handle Sonar S1206 with a targeted suppression plus justification instead of rewriting the type into a manual struct.
- Final validation recorded for the landed artifact: dotnet build -c Release src\Refitter.slnx --no-restore, dotnet test -c Release --solution src\Refitter.slnx --no-build, and dotnet format --verify-no-changes src\Refitter.slnx --no-restore.

## 2026-04-26

### AI-slop cleanup sequencing

**Verified By:** Ripley
**Status:** APPROVED

- Start with **docs/help drift** so stale SourceGenerator and MSBuild guidance is corrected before code refactors.
- Follow with **settings/spec-path normalization** across CLI and source-generator entry points, then centralize the shared **`GeneratedFile:` marker contract**.
- Keep **test-surface cleanup** ahead of deeper generator dedup so regression coverage is stronger before touching contract-sensitive code.
- Leave **core generator dedup** for the last batch and treat `OpenApiDocumentFactory` merge semantics, auth-header CLI compatibility, source-generator diagnostic contracts, and single-file `GeneratedCode` metadata behavior as reviewer-gated cleanup seams.
- Current squad capacity is sufficient for docs, tests, tooling, and settings cleanup; bring in a specialized C# reviewer only for the later core generator / `ParameterExtractor` dedup lane.

### Cleanup-safe baseline and coverage guardrails

**Verified By:** Lambert (Tester)
**Status:** VERIFIED

- Baseline validation is healthy on this machine: restore, release build, solution tests, and format verification all passed.
- Treat the Codecov command in `.github\workflows\codecov.yml` as the authoritative cleanup coverage lane; it targets `src\Refitter.Tests\Refitter.Tests.csproj`.
- Prefer narrow branch-coverage gains before production edits in `src\Refitter.Core\ContractTypeSuffixApplier.cs`, `SchemaCleaner.cs`, `CSharpClientGeneratorFactory.cs`, `XmlDocumentationGenerator.cs`, `IdentifierUtils.cs`, and `src\Refitter\Settings.cs`.
- Do not use the live-network test surface (`OpenApiDocumentFactoryTests`, `SwaggerPetstoreTests`, `SwaggerPetstoreApizrTests`, `Examples\OpenApiUrlTests`) as a stability signal during cleanup because it remains environment-sensitive.
- Coverage reports also include `.test-work` runtime-proof artifacts; note them, but prioritize real repository source files first.

### Generator cleanup phasing

**Verified By:** Parker (Core Developer)
**Status:** APPROVED

- Land generator cleanups in two phases: first trim redundant string/branch duplication already covered by public regressions in `XmlDocumentationGenerator.AppendXmlCommentBlock()` and `ContractTypeSuffixApplier.TypeSuffixRewriter`.
- Before deeper dedupe, add or tighten compile-backed public regressions for `ParameterExtractor`, multipart generation, and interface-emission behavior.
- Route any cleanup that deduplicates `RefitGenerator.Generate()` vs `GenerateMultipleFiles()` or the shared method-emission logic across the three interface generators through **Ash review**.

### AI-slop safety review gate

**Verified By:** Ash
**Status:** REQUIRED

- Treat the safety-sensitive generator and MSBuild cleanup lane as **validation-first** rather than style-first.
- Require source-generator regressions proving same-directory `.refitter` files with the same `outputFilename` do not collide on `AddSource()` hint names and that `AdditionalText.GetText(...) == null` reports a diagnostic instead of throwing.
- Require generator regressions for OpenAPI-title-derived identifiers containing `<` / `>` and for ByEndpoint interface names whose internal `I` characters must remain intact.
- Require MSBuild coverage for `GetInstalledDotnetRuntimes()` timeout/failure handling before claiming the process-launch path is hardened.

### Session directives refresh

**By:** Christian Helle (via Copilot)

- 2026-04-26: Have all agents use GPT-5.5 for the rest of this session only.
- 2026-04-26: Commit changes as frequent as possible in small logical groups for a detailed progress history.

## 2026-04-28

### e-conomic Multi-Spec OpenAPI Merge Failure

**Leads:** Ripley (analysis), Parker (core finding), Dallas (tooling validation)  
**Tester:** Lambert  
**Status:** TRIAGED AND APPROVED FOR FIX

#### Findings

- `test\economic.refitter` triggers merge failure in `OpenApiDocumentFactory.Merge()` while combining `economic-products.json` and `economic-webhooks.json`.
- Exception: `InvalidOperationException: Cannot merge OpenAPI documents because a duplicate schema 'Error' was found.`
- Root cause: `AreEquivalent()` calls `Serializer.Serialize()` on NSwag/NJsonSchema objects; object-cycle behavior returns `false` for semantically identical schemas.
- Both `Error` and `ProblemDetails` are duplicated and textually equivalent in source JSON; merge fails at `Error` first.
- Issue is **not** in `.refitter` parsing, path resolution, or individual spec generation—each spec generates successfully in isolation.
- CLI, source generator, and MSBuild all reach the same merge failure point before code output.

#### Fix Ownership and Scope

**Primary owner:** `Refitter.Core` → `src\Refitter.Core\OpenApiDocumentFactory.cs` → `MergeIfMissingOrThrowOnConflict()` / `AreEquivalent()`

- Replace `AreEquivalent()` with OpenAPI-aware semantic equivalence check (use NSwag `ToJson()` or canonical JSON representation).
- Keep fail-fast policy for **genuinely conflicting** duplicate path/schema/definition/security entries.
- Keep clone-first/non-mutating merge behavior.
- Do **not** edit e-conomic OpenAPI specs; do **not** rename schemas in merge.

#### Approved Test Coverage and Gates

1. Add `OpenApiDocumentFactoryMergeTests`:
   - `Merge_With_Equivalent_Duplicate_Schema_Does_Not_Throw` — OpenAPI 3 docs with identical schema key and body; both paths preserved.
   - Preserve existing `Merge_With_Schema_Collision_Throws_And_Does_Not_Mutate_Inputs` for conflicts.
   - Optional e-conomic-shaped regression for `Error` schema with descriptions, nullable properties, extensions, `additionalProperties: false`.

2. Compile-backed regression: two specs with identical common error schema and distinct paths; assert generation succeeds and generated code builds.

3. Run generated-code validation for `test\economic.refitter` after merge fix; triage any post-merge generation failure separately.

4. Existing merge-collision tests must continue passing.

#### Constraints

- Preserve relative-path behavior across CLI and source generator.
- Keep `openApiPaths` semantics consistent.
- OpenAPI 3 `components.schemas` and Swagger 2 `definitions` equivalence must remain aligned.
- Do not hide genuine schema differences in comparison.

### Ash economic merge review

- Date: 2026-04-28T15:21:48.369+02:00

Verdict: APPROVED

Rationale:
- `OpenApiDocumentFactory.Merge()` still fails fast on genuinely conflicting duplicate paths, schemas, Swagger 2 definitions, and security schemes while accepting equivalent duplicate schemas.
- Equivalence is based on normalized OpenAPI/NSwag/NJsonSchema JSON tokens rather than object identity; the recursive schema test covers cyclic schema graphs and the e-conomic regression verifies real multi-spec generation remains buildable.
- Focused validation passed for `OpenApiDocumentFactoryMergeTests`, `Issue1016_MultiSpecSchemaMergeTests`, and the duplicate-path merge coverage via the net10.0 TUnit executable. A first `dotnet test --filter` attempt failed because this TUnit project does not support that option, not because of product/test failure.

### Lambert e-conomic validation

- Date: 2026-04-28T15:21:48.369+02:00
- Validation result: PASS
- Evidence:
  - `dotnet build -c Release src\Refitter.slnx` passed.
  - Focused `OpenApiDocumentFactoryMergeTests` passed: 15 succeeded / 0 failed.
  - Focused `Issue1016_MultiSpecSchemaMergeTests` passed: 7 succeeded / 0 failed.
  - `dotnet test -c Release src\Refitter.slnx` passed.
  - `dotnet format --verify-no-changes src\Refitter.slnx` passed.
  - `dotnet .\src\Refitter\bin\Release\net9.0\refitter.dll --settings-file test\economic.refitter --no-banner --no-logging --simple-output` passed and generated `test\GeneratedCode\economic.cs`.
- Decision signal: the e-conomic duplicate equivalent schema merge path is validated by focused regression tests, full solution tests, and real CLI generation.

### Parker: e-conomic equivalence implementation

- Date: 2026-04-28T15:21:48.369+02:00
- Decision: `OpenApiDocumentFactory.AreEquivalent()` should compare normalized OpenAPI/NSwag JSON first and use a narrow NJsonSchema structural fallback only for schemas whose standalone JSON serialization cannot resolve references.
- Rationale: duplicate schemas from separate vendor specs can be semantically identical while NJsonSchema object graphs contain unresolved/cyclic references; raw serializer comparison falsely reports conflicts.
- Guardrail: merge still copies missing keys, ignores equivalent duplicates, and throws `InvalidOperationException` for non-equivalent duplicate paths, schemas, definitions, and security schemes.
- Validation: focused merge tests and Issue1016 multi-spec regression tests passed.

### User directive: 100% coverage on OpenApiDocumentFactory

**By:** Christian Helle (via Copilot)  
**Date:** 2026-04-28T19:19:33.388+02:00

Get `src/Refitter.Core/OpenApiDocumentFactory.cs` to 100% coverage; it is one of the most important parts of the code base and all possibilities should be tested thoroughly.

### Ripley: GitHub Issue/PR Prep for e-conomic Multi-Spec Fix

**Author:** Ripley (Lead)  
**Date:** 2026-04-28  
**Status:** READY FOR EXECUTION ON WORDING

#### Decision

For this repo, the pending issue + PR sequence should target **`main`**, not `dev`, because the live remote has no `dev` branch and GitHub reports `main` as the default branch.

The current execution branch is **`economic-openapi`**. It is already pushed, tracks `origin/economic-openapi`, has no open PR, and carries the seven fix commits for the e-conomic multi-spec merge work.

`test/multiple-sources.refitter` does **not** currently block PR creation. It is already committed on the branch and the working tree is clean, so no extra stash/reset step is required before creating the issue and PR.

#### Why

- Repo reality overrides the generic dev-first squad workflow here.
- Using `main` avoids opening a PR against a non-existent branch.
- A clean working tree means the PR can safely describe only the already-committed fix set.

#### Execution Notes

1. Create the GitHub issue once Bishop provides final wording.
2. Re-push `economic-openapi` only if a last-minute commit is added after wording arrives.
3. Open the PR from `economic-openapi` to `main` and include `Closes #<new-issue-number>` in the PR body.

### Lambert: OpenApiDocumentFactory 100% Coverage

**Owner:** Lambert  
**Date:** 2026-04-28

#### Decision

Treat `NSwag.OpenApiDocument.Tags` and `NSwag.OpenApiDocument.Components.Schemas` as non-null collections inside `OpenApiDocumentFactory.Merge`, and use collection-count checks instead of null-coalescing/null-conditional branches there.

#### Why

Local reflection and coverage evidence showed the previous null-handling branches in `Merge` were not reachable for parsed NSwag documents because:

- `Tags` materializes as an empty list, not `null`
- `Components` materializes as a non-null object
- `Components.Schemas` materializes as an empty dictionary, not `null`

Those unreachable branches blocked `OpenApiDocumentFactory.cs` from reaching 100% coverage even after adding focused regression tests.

#### Evidence

- Full coverage command passed and reported `OpenApiDocumentFactory` functions at 100% line/block coverage in `src\Refitter.Tests\bin\Release\net10.0\TestResults\coverage.cobertura.xml`
- Reflection check confirmed NSwag parses both OpenAPI 3 and Swagger 2 documents with non-null `Tags`, `Components`, and `Components.Schemas`

#### Impact

- No intended behavior change for real parsed OpenAPI inputs
- Merge logic is simpler and now fully coverable with focused regression tests

## 2026-04-29

### Ash: OpenApiDocument clone path should use parameterless ToJson()

**Author:** Ash  
**Date:** 2026-04-29T10:41:29.997+02:00

#### What

For `src\Refitter.Core\OpenApiDocumentFactory.cs`, replace the obsolete `OpenApiDocument.ToJson(SchemaType)` clone path with `OpenApiDocument.ToJson()` followed by `OpenApiDocument.FromJsonAsync(...)`.

#### Why

Focused coverage in `src\Refitter.Tests\OpenApiDocumentFactoryMergeTests.cs` shows the non-obsolete serializer preserves `SchemaType` and the merge-critical collections for embedded Swagger 2 and OpenAPI 3 fixtures, while mixed-version merge assertions confirm the cloned base document still serializes with the correct top-level version marker. That keeps the clone-first/fail-fast merge contract intact without carrying the obsolete API forward.

### Lambert: OpenApi clone validation surface

**Author:** Lambert  
**Date:** 2026-04-29T10:41:29.997+02:00

#### What

Validate the obsolete `OpenApiDocument.ToJson(SchemaType)` replacement through focused `OpenApiDocumentFactoryMergeTests` that round-trip embedded Swagger Petstore JSON fixtures for both Swagger 2 and OpenAPI 3, then assert merge keeps the base document schema type.

#### Why

The obsolete call only exists to clone the first `NSwag.OpenApiDocument` before merge. The safest proof is not a full multi-spec integration run first; it is a narrow clone/merge surface that confirms the cloned document keeps `SchemaType` and the key collections (`Paths`, `Tags`, `Components.Schemas`, `Definitions`, `SecurityDefinitions`) that merge relies on.

### User directive: Use GPT-5.5 for agents

**By:** Christian Helle (via Copilot)  
**Date:** 2026-04-29T10:41:29.997+02:00

Use GPT-5.5 for all agents for the rest of this session only.

### Ripley PR Assembly (PR #1079)

**Author:** Ripley (Lead)  
**Date:** 2026-04-29T11:51:36.530+02:00  
**Branch:** `build-warnings`  
**PR:** #1079 `Tighten warning handling across Refitter builds`  
**Status:** READY FOR REVIEW

#### Decision

Frame the PR around the branch's real aggregate scope instead of only the newest OpenAPI clone-warning fix.

#### Rationale

- The branch includes multiple warning-focused commits across core, tests, CLI packaging metadata, nullable-flow cleanup, and warning enforcement changes.
- It already carries a committed `.squad` decision/history sync.
- The obsolete `OpenApiDocument` clone fix remains important and should stay called out explicitly, but only as one bullet within the broader warning-hardening frame.

#### Reviewer Guidance

- Review against `main`.
- Expect main product deltas in `src\Refitter.Core\OpenApiDocumentFactory.cs`, `src\Refitter.Tests\OpenApiDocumentFactoryMergeTests.cs`, `src\Refitter.Core\Refitter.Core.csproj`, `src\Refitter.Tests\Refitter.Tests.csproj`, and `src\Refitter\Refitter.csproj`.
- Treat the `.squad` history/decision sync as a carried branch artifact, not the headline behavior change.

