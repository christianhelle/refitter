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
