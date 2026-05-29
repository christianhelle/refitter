## 2026-05-29

### Refitter RefitterAutoScan MSBuild Implementation

**Leads:** Dallas (implementation), Bishop (documentation), Lambert (validation)  
**Status:** APPROVED

#### Decision

Gate Refitter's automatic MSBuild hook behind a new wrapper target instead of conditioning RefitterGenerate directly.

#### Why

- RefitterGenerate must remain callable through dotnet build -t:RefitterGenerate even when <RefitterAutoScan>false</RefitterAutoScan>.
- A separate _RefitterGenerateOnBuild target keeps the on-build behavior opt-out while preserving the explicit target contract for CI and local workflows.
- The workflow proof asserts both the absence of task execution during normal clean builds and the continued success of explicit generation with auto-scan disabled.

#### Evidence

- src\Refitter.MSBuild\Refitter.MSBuild.targets defines RefitterGenerate as the explicit generation target and _RefitterGenerateOnBuild as the build hook with BeforeTargets="CoreCompile", DependsOnTargets="RefitterGenerate", and Condition="'$(RefitterAutoScan)' != 'false'".
- src\Refitter.MSBuild\README.md now states that RefitterGenerate remains the on-demand target, _RefitterGenerateOnBuild is the normal-build hook, and RefitterAutoScan controls that wrapper target.
- docs\docfx_project\articles\msbuild.md now describes the same target/property relationship and preserves dotnet build -t:RefitterGenerate as the manual invocation path.
- .github\workflows\msbuild.yml updated with RefitterAutoScan tests.
- Full validation passed: restore, build, test, and format verification.

### User Directive

**By:** Christian Helle (via Copilot)  
**Date:** 2026-05-29T12:44:43.282+02:00  
**What:** Use claude-opus-4.8 for all agent work for the rest of this session only.  
**Why:** User request — captured for team memory
