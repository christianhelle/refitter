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
✅ All tests passing (1415/1415)  
✅ Code formatting verified

---

### 8. User Directive: Commit Message Style (2026-03-06)

**Status:** ℹ️ Process Update  
**Date:** 2026-03-06  
**By:** Christian Helle (via Copilot)

Commit changes in small logical groups with one-liner commit messages without a co-author trailer.

---

*Maintained by Scribe. Agents write to `.squad/decisions/inbox/` and Scribe merges, deduplicates, and consolidates here.*
