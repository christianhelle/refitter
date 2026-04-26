# McManus — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** GitHub Actions, MSBuild, .NET multi-target (8.0, 9.0, 10.0), PowerShell  
**Repo root:** `C:/projects/christianhelle/refitter`  

- Domain focus: `.github/workflows/`, `src/Refitter.MSBuild/`, `src/Directory.Build.props`, `test/smoke-tests.ps1`, and `test/smoke-tests.bat`.
- CI guardrails: `dotnet format --verify-no-changes src/Refitter.slnx` is a hard gate, Windows is the primary CI environment, and `test/ConsoleApp/Directory.Build.props` uses `SmokeTest=true` to keep batch smoke builds stable.
- Durable workflow map: main validation runs through `build.yml`, `smoke-tests.yml`, `msbuild.yml`, `regression-tests.yml`, `production-tests.yml`, `release.yml`, `release-preview.yml`, `docfx.yml`, and `codecov.yml`.
- Earlier CI/CD review takeaways: smoke coverage is broad, packaging/release automation is modern, but workflow triggers, explicit format enforcement, and version centralization are recurring operational review seams.
- #944 validation rule: repo changes are only merge-ready after build, full tests, and format verification all pass.
- #967 MSBuild rule: the reported stack overflow was a shared Core recursive-schema bug, not an MSBuild-specific one; PreserveOriginal naming was not causal, and recursive schema coverage should be added across CLI, MSBuild, SourceGenerator, and smoke lanes.
- Confidentiality rule: `tmp/` must remain protected from git via layered ignore rules because that folder may contain private user artifacts.

## Learnings

- Team initialized on 2026-04-16.
