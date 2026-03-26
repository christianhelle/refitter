# McManus — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** GitHub Actions, MSBuild, .NET multi-target (8.0, 9.0, 10.0), PowerShell  
**Repo root:** `C:/projects/christianhelle/refitter`  

My domain: `.github/workflows/`, `src/Refitter.MSBuild/`, `src/Directory.Build.props`, `test/smoke-tests.ps1`, `test/smoke-tests.bat`.

CI must pass `dotnet format --verify-no-changes src/Refitter.slnx` — formatting is a hard gate.  
Primary CI environment: Windows.  

Smoke test note: `test/ConsoleApp/Directory.Build.props` uses `SmokeTest=true` MSBuild property to disable the Refit source generator and `TreatWarningsAsErrors` during batch builds (avoids filename-too-long errors).

Key workflows: `build.yml` (main), `smoke-tests.yml` (quick), `release.yml` + `release-preview.yml` (releases), `msbuild.yml`, `regression-tests.yml`, `production-tests.yml`, `docfx.yml`, `codecov.yml`.

## Learnings

### Session: Full CI/CD Review

**Key Findings:**

1. **Workflow Structure:** 25 total workflows — 5 core CI/CD + 7 extended testing + 13 squad/automation. Good separation of concerns.
   - `build.yml` (PR/push to main) ✅
   - `smoke-tests.yml` (13+ OpenAPI specs) ✅
   - `release.yml` + `release-template.yml` (NuGet + Docker) ✅
   - `regression-tests.yml` (macOS/Windows/Ubuntu) ✅
   - `production-tests.yml` (daily live package testing) ✅

2. **Build Config (Directory.Build.props):**
   - Preview C# version, nullable enable, implicit usings
   - Auto-pack on Release, SourceLink embedded, TieredCompilation on
   - Modern, defensive defaults

3. **SDK (global.json):** .NET 10.0.100 pinned with latestFeature rollForward. Modern Testing Platform runner.

4. **Smoke Tests (test/smoke-tests.ps1):** Comprehensive. Tests 13+ specs × 2 versions (v2/v3) × 2 formats (JSON/YAML) × 20+ CLI flags. Validates CLI generation + build success. ~5–10 min runtime.

5. **MSBuild Task:** Clean. Scans for `.refitter` files, runs CLI, integrates into build. Tested on Windows via `msbuild.yml`. Packed in NuGet with net8.0/9.0/10.0 binaries.

6. **NuGet Packaging:** 4 packages — Refitter (CLI tool), Refitter.Core (lib), Refitter.SourceGenerator (SG), Refitter.MSBuild (task). OIDC-based auth for NuGet push. Docker image also published.

**Risks Found:**

- 🔴 **Codecov token hardcoded in YAML** (codecov.yml:24) — IMMEDIATE rotation needed
- 🟠 **squad-ci.yml not configured** — placeholder "no build commands" but still runs on PR/push to main
- 🟠 **No explicit `dotnet format --verify-no-changes` in CI** — only validated indirectly via test success
- 🟡 **Version hardcoded in 3 places** (build.yml, codecov.yml, release.yml) — should centralize
- 🟡 **MSBuild tests Windows-only** — should test macOS/Ubuntu too given netstandard2.0 target

**Strengths:**

- Multi-OS regression tests (macOS/Windows/Ubuntu)
- 20+ feature combinations tested in smoke suite
- Daily live package verification (NuGet + Docker)
- Clean decoupling (CLI, SG, MSBuild as independent packages)
- Modern security (OIDC, SourceLink, embedded symbols)

**Grade:** B+ (87%) — Production-ready, but token exposure and non-functional squad-ci.yml are immediate concerns.

Full assessment written to `.squad/decisions/inbox/mcmanus-cicd-review.md`

## Validation Gate: Issue #944 (2026-03-06)

**Objective:** Validate patch for non-ASCII XML comment handling via the 3-step PR gate.

**Results:**

1. **Build (`dotnet build -c Release src/Refitter.slnx`):** ✅ PASS
   - All projects compiled successfully
   - Exit code: 0

2. **Tests (`dotnet test --solution src/Refitter.slnx -c Release`):** ✅ PASS
   - **1,451 tests total:** 1,415 (Refitter.Tests) + 18 (SourceGenerator net8.0) + 18 (SourceGenerator net10.0)
   - **Failed:** 0, **Skipped:** 0
   - Duration: 38s 178ms
   - Exit code: 0

3. **Format Verification (`dotnet format --verify-no-changes src/Refitter.slnx`):** ✅ PASS
   - No violations detected
   - Exit code: 0

**Conclusion:** Issue #944 patch is **READY FOR MERGE**. All validation gates passed.

## Investigation: Issue #967 — Stack Overflow in MSBuild (2025-04-01)

**Objective:** Determine if `Refitter.MSBuild 1.8.0-preview.100` stack overflow with `propertyNamingPolicy: PreserveOriginal` + `excludedTypeNames` is surface-specific or shared core bug.

**Key Findings:**

1. **Root Cause: Shared Core Bug** (Not MSBuild-Specific)
   - Stack trace points to `CSharpClientGeneratorFactory.ProcessSchemaForMissingTypes()` and `ProcessSchemaForIntegerType()`
   - Both methods recursively traverse schemas (AllOf, AnyOf, OneOf) **without cycle detection**
   - Circular/self-referential schemas cause unbounded recursion → stack overflow
   - MSBuild task is just a thin wrapper; the bug is in shared code generation logic

2. **PreserveOriginal Not the Culprit**
   - `PreserveOriginalPropertyNameGenerator` is trivial (10 lines, non-recursive)
   - Delegates to `IdentifierUtils.ToCompilableIdentifier()` which only does character sanitization
   - Confirmed: character-level operations cannot cause recursive stack traces

3. **ExcludedTypeNames Root Factor**
   - `CodeGeneratorSettings.ExcludedTypeNames` filtering happens **after** schema preprocessing
   - If an excluded type has circular references, preprocessing loops infinitely
   - Fix: Apply `excludedTypeNames` filter **before** recursive schema processing

4. **Validation Coverage Gaps Found:**
   - **CLI:** No `--property-naming-policy PreserveOriginal` variants in smoke tests (20+ flags tested, 0 naming variants)
   - **MSBuild:** Only tests Windows + single petstore.refitter (no property naming, no excludedTypeNames)
   - **Source Generator:** No property naming tests at all
   - **Unit Tests:** PropertyNamingPolicyTests covers basic cases but **no recursive schema + PreserveOriginal test**
   - **Smoke Tests:** 13+ OpenAPI specs × 20+ flag variants — **MISSING PreserveOriginal combinations**

5. **MSBuild Task Surface Assessment:**
   - RefitterGenerateTask.cs (297 lines) — thin wrapper only
   - Spawns `dotnet refitter.dll` as subprocess; no custom code generation
   - All generation logic is in Core library (shared by CLI, SourceGenerator)
   - **Conclusion:** Bug fix must be in Core, not MSBuild task

**Rollout Strategy:**

1. Fix: Add cycle detection via `HashSet<JsonSchema>` visited set in schema preprocessing methods
2. Validation: Add unit test with self-referential schema + PreserveOriginal
3. MSBuild CI: Update `msbuild.yml` to test PreserveOriginal configuration
4. Smoke Tests: Add PreserveOriginal variants to standardVariants array
5. Preview Release: 1.8.0-preview.101 with full test coverage before final 1.8.0

**Deliverables Created:**
- `.squad/decisions/inbox/mcmanus-issue-967-stack-overflow.md` — Full investigation report with 7-phase verification checklist
- Identified validation surfaces: CLI, MSBuild, SourceGenerator, Smoke Tests, Unit Tests
- Quantified test gaps: PreserveOriginal property naming variants missing everywhere
- Risk assessment and mitigation strategy included

**Status:** Investigation complete. Ready for developer implementation + McManus workflow updates.

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
