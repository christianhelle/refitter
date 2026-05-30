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


## 2026-05-29T14:24:16.307+02:00 - RefitterAutoScan build gating

- Context: issue #1094 needs MSBuild users to disable automatic `.refitter` scanning during ordinary builds without losing the explicit `RefitterGenerate` entry point.
- Decision: keep `RefitterGenerate` as the standalone generation target, default `RefitterAutoScan` to `true`, and move the normal-build hook into a separate target that depends on `RefitterGenerate` only when `RefitterAutoScan` is not `false`.
- Consequence: `dotnet build -t:RefitterGenerate -p:RefitterAutoScan=false` still generates code on demand, while later `dotnet build -p:RefitterAutoScan=false` runs compile against the already-generated `.cs` files without re-running Refitter.


## 2026-05-29T14:24:16.307+02:00 - Lambert validation for issue #1094

- Context: Christian Helle requested validation of the real working-tree implementation for the RefitterAutoScan behavior change in issue #1094.
- Decision: approve the current working-tree implementation as matching the requirement.
- Evidence: the actual diff is present in the four expected files; `src\Refitter.MSBuild\Refitter.MSBuild.targets` defaults `RefitterAutoScan` to `true` and gates normal builds through `_RefitterGenerateOnBuild`; local validation passed for auto-scan enabled builds, explicit `dotnet build -t:RefitterGenerate -p:RefitterAutoScan=false`, and normal `dotnet build -p:RefitterAutoScan=false` without `RefitterGenerateTask`; README, docfx docs, and `.github\workflows\msbuild.yml` describe and verify the same behavior.

## 2026-05-29T15:07:47.442+02:00 - Commit grouping for issue #1094

- Context: the issue #1094 working tree already contained the product changes across MSBuild targets, CI coverage, and user-facing docs, plus a stale `.squad\commit-msg.txt` deletion from earlier automation.
- Decision: group the product diff into two user-facing commits (`tooling or CI` and `docs`) and keep the stale automation cleanup plus Squad bookkeeping in a separate housekeeping commit with no co-author trailer.
- Consequence: reviewers can reason about behavior changes separately from documentation and repository hygiene, while the branch still finishes with a clean working tree.

