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

_Session learnings will be appended here._
