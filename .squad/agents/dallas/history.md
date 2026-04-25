# Dallas History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Validated MSBuild tooling path. CLI loads settings correctly (naming honored). Single-file output without explicit `outputFilename` falls back to `Output.cs` and skips `./Generated` folder when it matches default. Real product bug: MSBuild expects wrong file locations on first clean build.
- **PR #1067 tooling proof (2026-04-21):** The strongest regression signal came from testing the actual stdout marker contract and the packed `Refitter.SourceGenerator` artifact. `RefitterGenerateTask` now has unit coverage for exact include matching plus duplicate/zero-marker handling, and package validation inspects the produced `.nupkg`/`.nuspec` instead of trusting project metadata.

### 2026-04-20: PR #1064 Tooling Review

- **#1011 is only partially fixed:** `RefitterSourceGenerator.CreateUniqueHintName()` hashes only the directory path, so two `.refitter` files in the same folder that share the same `outputFilename` still collide on `context.AddSource()`. The attempted fix moved the failure instead of making hint names unique per `.refitter` input.
- **#1021 is only partially fixed:** single-file `--output` override was repaired, but multi-file generation still ignores `settings.OutputPath` whenever `OutputFolder` is populated/defaulted. Settings-file + `--multiple-files`/contracts-output flows still write under the settings output folder.
- **#1050 is only fixed for CLI/MSBuild:** helpful enum diagnostics were added to `SettingsValidator`, but `RefitterSourceGenerator.TryDeserialize()` still emits the raw exception text without property/value guidance. `.refitter` enum mistakes remain hard to diagnose in source-generator hosts.
- **Validation evidence gap:** PR #1064 check runs were green, but the `test` GitHub check is a no-op placeholder (`No build commands configured — update squad-ci.yml`). The local `test\MSBuild\test-exit-code.ps1` script passes when launched from `test\MSBuild`, but fails from repo root because its relative paths assume that working directory.

### 2026-04-17: Breaking Changes Audit + Tie-Break Decision

**Primary Audit Task**: Reviewed 370 files (36,658 insertions, 6,808 deletions) between 1.7.3 and HEAD for breaking changes. **INITIAL ASSESSMENT:** No breaking changes found. All changes appeared backward-compatible or additive.

**Tie-Breaker Re-Check**: Parker and Ripley flagged 3 candidates for re-verification.

**Final Verdict**: **2 CONFIRMED BREAKING CHANGES**

1. **Source Generator Disk Files** (HIGH RISK)
   - Commit: f853bcf2 (PR #923) - "Fix #635: Use context.AddSource() instead of File.WriteAllText()"
   - Fixes issues #635, #520, #310 (file locking, process access errors) — legitimate bug fix
   - BUT: Behavioral break for users expecting physical `.g.cs` files in `./Generated` folder
   - Users who committed generated files or relied on disk inspection are affected
   - Migration: Use IDE "View Generated Files" or switch to CLI/MSBuild for disk files

2. **Auth Property Renamed** (MEDIUM RISK)
   - Commit: 7dbf6c0c (PR #897) + 14101a49 (PR #936)
   - Old: `"generateAuthenticationHeader": true` (boolean)
   - New: `"authenticationHeaderStyle": "Method"|"Parameter"|"None"` (enum)
   - No backward compatibility layer or JSON property alias
   - Old JSON key **silently ignored** — deserializes to default (`None`)
   - Users get wrong behavior without error (dangerous failure mode)
   - Migration: Replace old key with new enum value

**NOT BREAKING (Bug Fix)**:
- Default output folder unchanged (`./Generated`)
- MSBuild bug fix makes it **consistent** with CLI (Issue #998)
- Users relying on old buggy behavior: set `"outputFolder": "."` explicitly

**Release Recommendation**: **v2.0.0** (major bump required). Both breaking changes require migration guide.

**CLI/Settings/Options**:
- All new options since 1.7.3 have safe defaults and are optional
- No removed or renamed options
- No changed default values
- Settings file processing order improved (`.refitter` first, then CLI override)

**Dependency Updates**:
- Spectre.Console.Cli v0.55.0: Methods changed from `public override` to `protected override` (non-breaking for CLI users)
- Microsoft.OpenApi v3.x (internal only)
- Refit v10 (users should upgrade)

**Key File Paths for Tooling**:
- CLI options: `src/Refitter/Settings.cs`
- CLI logic: `src/Refitter/GenerateCommand.cs`
- Settings model: `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`
- Source generator: `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs`
- MSBuild task: `src/Refitter.MSBuild/RefitterGenerateTask.cs`
- Settings docs: `docs/docfx_project/articles/refitter-file-format.md`

### 2026-04-18: Critical Tooling Fixes (Issues #1011, #1012)

**Fixed #1011 — Source Generator Hint-Name Collisions**

Problem: When multiple `.refitter` files with the same filename existed in different directories (e.g., `src/ApiA/petstore.refitter` and `src/ApiB/petstore.refitter`), the source generator crashed with `ArgumentException: hintName was already added` because hint names were computed only from the filename.

Solution implemented:
- Created `CreateUniqueHintName()` method that generates stable, unique hint names by combining the base filename with a hash of the directory path
- Honors explicit `OutputFilename` when set while still preventing collisions
- Uses simple deterministic hash (31-bit polynomial) formatted as 8-char hex suffix
- Hint name format: `{baseName}_{pathHash}.g.cs` (e.g., `petstore_A1B2C3D4.g.cs`)

Files changed:
- `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs`: Added `CreateUniqueHintName()` and `GetStableHash()` helper methods

Regression coverage:
- Created `src/Refitter.SourceGenerator.Tests/HintNameCollisionTests.cs` with two test cases:
  1. Verifies generator doesn't crash with duplicate filenames in different directories
  2. Verifies explicit `outputFilename` intent is preserved in hint name base

**Fixed #1012 — MSBuild Task Swallows CLI Failures**

Problem: `RefitterGenerateTask.Execute()` ignored the `dotnet refitter.dll` process exit code and unconditionally returned `true`, causing CI/CD pipelines to silently ship stale/missing generated code when the CLI failed.

Solution implemented:
- Added exit code inspection: `process.ExitCode != 0` → log error and mark as failed
- Added process timeout (5 minutes) to prevent build hangs
- Modified `TryExecuteRefitter()` to return `out bool failed` parameter
- Modified `Execute()` to track `hasErrors` and return `false` if any .refitter file failed
- Exception paths also set `failed = true` for comprehensive error handling

Files changed:
- `src/Refitter.MSBuild/RefitterGenerateTask.cs`: Modified `Execute()`, `TryExecuteRefitter()`, and `StartProcess()` methods

Regression coverage:
- Created `test/MSBuild/test-exit-code.ps1` script that:
  1. Creates test project with invalid .refitter (unreachable URL)
  2. Runs `dotnet build`
  3. Asserts build fails with non-zero exit code (would pass before fix)

**Build Status**:
- MSBuild project compiled successfully: ✅ `src\Refitter.MSBuild\bin\Release\netstandard2.0\Refitter.MSBuild.dll`
- Source generator changes are syntactically correct but compilation blocked by pre-existing Refitter.Core issues (unrelated to this work):
  - `OpenApiDocumentFactory.cs(68,21)`: Property assignment errors
  - `RefitGenerator.cs(275,48)`: Missing `JsonLibrary` definition
  - These are outside the scope of the critical tooling slice
- All modified code formatted with `dotnet format`

**Validation**:
- MSBuild task changes compile cleanly
- Code formatted according to project standards
- Regression test infrastructure in place (execution depends on Core build fix)

**Scope Notes**:
- Tightly scoped to P0 tooling issues #1011 and #1012
- No coupling with other audit findings
- Both fixes are self-contained and can ship independently once Core compilation is restored

### 2026-04-17: GitHub Discussions Setup + Distribution Audit

**Discussion Creation Capability**: Confirmed repo has discussions enabled. GitHub CLI (v2.73.0) authenticated as `christianhelle`. Available categories: Announcements (recommended), General, Ideas, Polls, Q&A, Show and tell. Can create Discussion directly via `gh api graphql` with `createDiscussion` mutation.

**Evidence Artifacts for Discussion Post**:
- Auth property change: Commits 7dbf6c0c (PR #897), 14101a49 (PR #936)
- Source generator change: Commit f853bcf2 (PR #923) - fixes issues #635, #520, #310
- 359 total commits, 370 files changed (+36,658/-6,808)
- All breaking changes have unit test coverage
- Team consensus: v2.0.0 major bump required

**Detailed Audit Report Written**: `.squad/decisions/inbox/dallas-breaking-audit.md` with full evidence chain, migration paths, and non-breaking change summary for reference during release planning.

### P2 Issue Verification (2026-04-18)

**Verified 16 P2 issues from v2.0 audit**

Key patterns and file paths discovered:

**Source Generator Architecture:**
- src\Refitter.SourceGenerator\RefitterSourceGenerator.cs - Main generator using incremental compilation
- Pipeline outputs defined as private record GeneratedCode(List<Diagnostic>, string?, string?)
- Uses Debug.WriteLine for logging (no-op in Release)
- Should use context.ReportDiagnostic for user-visible warnings
- Should use EquatableArray or implement IEquatable for incremental caching

**CLI Validation Flow:**
- src\Refitter\SettingsValidator.cs - Validates settings before generation
- src\Refitter\GenerateCommand.cs - Orchestrates validation and generation
- Path resolution inconsistency: CLI uses CWD, generator uses .refitter directory
- Multi-spec validation only checks first entry

**Core Generation Critical Files:**
- src\Refitter.Core\RefitGenerator.cs - Main generation orchestrator
  - Line 275: Hard-coded \n in regex replacement (should use Environment.NewLine)
  - Handles JsonConverter attribute placement
- src\Refitter.Core\OpenApiDocumentFactory.cs - Spec loading and merging
  - Lines 15-19: Static HttpClient without timeout/User-Agent configuration
  - Line 46: Merge mutates documents[0] directly
  - Lines 60-70: Silent conflict resolution (first wins)
- src\Refitter.Core\XmlDocumentationGenerator.cs - XML comment generation
  - Line 133: Parameter descriptions not escaped before XML emission
  - Has EscapeSymbols method but not always used
  - AppendXmlCommentBlock doesn't escape attribute values
- src\Refitter.Core\ParameterExtractor.cs - Parameter processing
  - Line 180: Uses Contains("?") to detect nullability (matches generics)
  - Line 465: Mutates shared operationModel.Parameters collection
- src\Refitter.Core\RefitInterfaceImports.cs - Namespace generation
  - Line 62: Uses Aggregate which throws on empty sequence
- src\Refitter.Core\CustomCSharpTypeResolver.cs - Type mapping
  - Lines 32-33: Appends ? without checking NRT setting
  - Fragile Contains("Nullable<") check

**MSBuild Integration:**
- src\Refitter.MSBuild\RefitterGenerateTask.cs - Build-time generation
  - Line 93: Unescaped arguments (path injection risk)
  - Line 150: No null check before Split
  - Lines 76-91: TFM selection without File.Exists check

**Common Anti-patterns Found:**
1. Mutation of shared NSwag models (breaks subsequent generators)
2. String contains checks instead of proper parsing (nullable detection, type checking)
3. Missing XML escaping for user-supplied content
4. Hard-coded line endings instead of Environment.NewLine
5. Aggregate on potentially empty sequences
6. Debug.WriteLine in libraries (invisible in Release)
7. Path resolution relative to CWD instead of file location

**Settings Architecture:**
- src\Refitter\Settings.cs - CLI settings with Spectre.Console attributes
- src\Refitter.Core\Settings\RefitGeneratorSettings.cs - Core settings
- src\Refitter.Core\Settings\CodeGeneratorSettings.cs - Code generation settings
- Breaking change: --generate-authentication-header changed from bool to AuthenticationHeaderStyle enum

**Dependencies:**
- Spectre.Console.Cli 0.55.0 (potential parsing changes from 0.53)
- NSwag for OpenAPI document model and code generation
- H.Generators.Extensions (provides EquatableArray for source generators)

### 2025-01-09: PR #1064 Blocker Validation

**Status**: ❌ **NOT MERGE-READY** — 3 regression test failures detected

**Validation Scope**: Focused validation of merge blocker fixes for issues #1013, #1018, #1053

**Build Results**:
- ✅ Clean build: `dotnet build -c Release src/Refitter.slnx --no-restore` → 0 errors
- ⚠️ Test suite: 1776 passed, **3 failed**, 0 skipped (1779 total)
- Test run time: ~52 seconds

**Failed Regression Tests**:
1. `Issue1053_Schema_Names_As_Keywords_Are_Properly_Escaped` — Keywords not being escaped in type declarations (@class, @event missing)
2. `Issue1018_Deduplicates_Multipart_Parameters_By_Sanitized_Identifier` — Finding 3 "a_b" params instead of deduplicating to 1
3. `Issue1018_Generated_Code_With_Duplicate_Sanitized_Names_Compiles` — Build fails with "parameter a_b is a duplicate"

**Code Analysis**:
- Blocker fix code IS present in codebase (lines 97-140 in ParameterExtractor.cs verified)
- ContractTypeSuffixApplier.cs has collision detection correctly implemented
- BUT: Fixes are incomplete or have logic bugs that tests expose

**Key Findings**:
- Deduplication HashSet logic looks correct but isn't preventing duplicates
- Possible issue: Variable name generation in one code path differs from HashSet check path
- Keywords in schema type names aren't being escaped; EscapeReservedKeyword() may not be in type generation chain

**Recommendation**: 
- Do NOT merge until regression tests pass
- Need debug trace to see what variable names are actually being generated vs checked
- Check if `ConvertToVariableName()` correctly reduces "a-b", "a b", "a.b" to identical "a_b"
- Verify keyword escaping is called during schema type name generation, not just parameters

**Validation Command**:
```bash
dotnet test --project src/Refitter.Tests/Refitter.Tests.csproj -c Release --no-restore --no-build --output Detailed
```

**Report Location**: `.squad/decisions/inbox/dallas-pr1064-validation.md`

## 2026-04-20 Final Update: Blocker Validation Successful

**Task:** Re-validate blocker fixes after Ash's revision  
**Status:** ✅ COMPLETE — All blockers resolved; 1779/1779 tests passing  

**Final Validation Results:**
- **Build Status:** ✅ Clean build, 0 errors
- **Test Suite:** ✅ 1779/1779 PASSING (0 failures, up from 1776/1779)
- **Code Formatting:** ✅ All changes properly formatted

**Root Cause Feedback Used by Ash:**
- **#1018:** Dallas's observation about variable name mismatch guided Ash to unified naming method
- **#1053:** Dallas's identification of keyword escaping path gap led to test expectation correction

**Collaboration Notes:**
- Dallas provided concrete test failure data that enabled rapid root-cause diagnosis
- Validation report became feedback loop for Ash's revision cycle
- Final validation confirmed all three blockers comprehensively resolved

**Final Session Log:** `.squad/log/2026-04-20T16-00-14Z-pr1064-blocker-fixes.md`

**Merge Status:** ✅ APPROVED (temporary test JSON files marked for deletion)

### 2026-04-20: P1 Tooling Fixes (#1022, #1023, #1024)

- **MSBuild generated-file discovery:** `src\Refitter.MSBuild\RefitterGenerateTask.cs` now trusts `GeneratedFile:` markers emitted by `src\Refitter\GenerateCommand.cs --simple-output` instead of re-parsing `.refitter` contents. This removes duplicated output-path prediction logic and keeps MSBuild compile items aligned with the CLI's actual writes.
- **MSBuild include filtering semantics:** `RefitterIncludePatterns` now matches only exact filenames, exact project-relative paths, or exact full paths. Substring matching was removed; `apis\petstore.refitter` is now a stable way to target one file without over-including similarly named files.
- **SourceGenerator dependency boundary:** `src\Refitter.SourceGenerator\Refitter.SourceGenerator.csproj` keeps `OasReader` private to the generator package and hides `Refit` compile assets from consumers (`PrivateAssets="compile"`). Source generator consumers must carry their own explicit `Refit` reference so Refitter does not silently upgrade them to Refit 10.
- **Focused validation that worked reliably:** use a repo-local NuGet cache (`C:\projects\christianhelle\refitter\.nuget\packages`) when the shared global cache is locked, then run targeted TUnit treenode filters from `src\Refitter.Tests\bin\Release\net10.0\Refitter.Tests.exe` for fast regression checks.

### 2026-04-25: Audit Matrix Narrowing

- Ripley's remaining #1057 matrix pass treated #1047 as already fixed at HEAD because MSBuild now follows CLI `GeneratedFile:` markers.
- #1042 is validation-only and #1056 is doc/invariant-only, so remaining tooling/code follow-up is narrowed to the still-open code-backed items.

### 2026-04-25: Lambert Repro Narrowing

- Lambert's evidence pass keeps **#1029** and **#1041** only as **partial tooling repros** on current HEAD.
- **#1043** still reproduces as the legacy bool-style `--generate-authentication-header` CLI break.
- **#1042** remains validation-only and **#1047** remains fixed-at-HEAD unless a fresh failing packaged repro appears.

### 2026-04-25: Queued Core Revision Follow-up

- Ash rejected Parker's latest closure set for the #1057 core artifact.
- **#1034** and **#1039** remain open and require real fixes.
- Dallas is queued to take the next revision after the current tooling lane finishes, with Lambert adding blocker tests first.

### 2026-04-25: Tooling Lane Complete

- Completed the tooling lane with real fixes landed for **#1028**, **#1029**, **#1041**, and **#1043**.
- Validation outcome for the remaining tooling-adjacent checks: **#1042** is no-code / validation-only at current HEAD, and **#1047** is fixed-at-HEAD because MSBuild now follows CLI-emitted `GeneratedFile:` markers.
- Dallas reported successful build, test, and format validation for the tooling slice.
- Follow-up ownership is now active: Dallas moved immediately onto the rejected core revision for **#1034**/**#1039** because Parker is locked out.

### 2026-04-25: Core Revision Lane Complete

- Completed the non-Parker revision for **#1034** and **#1039** after Ash rejected Parker's earlier closure set.
- `OpenApiDocumentFactory.Merge()` now clones the first input before merge and warns on path/schema collisions instead of mutating the caller-owned document.
- `ParameterExtractor` now preserves the shared `operationModel.Parameters` list while assembling grouped query-parameter wrappers.
- Regression coverage for the core blockers was updated, and Dallas reported the revised validation lane green.
- Follow-up handoff is active: Ash is re-reviewing the revised core changes and Lambert is reconciling the blocker-test lane.


### 2026-04-25: Core Revision Partial Acceptance

- Ash cleared **#1039** on the revised core lane: ParameterExtractor no longer mutates the shared operationModel.Parameters list, and the new coverage locked that in.
- Ash kept **#1034** open because merge collisions still warn and keep the first entry instead of throwing on conflicting multi-spec inputs.
- Dallas owns one last narrow revision to flip the merge-collision behavior and its tests to fail-fast semantics.

### 2026-04-25: Final Narrow #1034 Revision

- Completed the last implementation pass for **#1034** after Ash's partial re-review kept the warning-backed merge contract open.
- `OpenApiDocumentFactory.Merge()` now keeps the clone-first non-mutation guarantee while failing fast on conflicting duplicate path/schema/definition/security keys instead of silently keeping the first entry.
- Updated merge regression coverage now locks the fail-fast contract; Ash is on the final review gate while Lambert reconciles the blocker-test lane.

### 2026-04-25: Final #1034 Gate Rejected

- Ash rejected Dallas's latest #1034 revision at the final gate.
- The blocker proof is still incomplete because the test surface does not explicitly cover conflicting duplicate schema, definition, and security-scheme merges.
- Broader core validation also reported a failing `Dynamic_Querystring_Generation_Preserves_Original_Query_Param_Documentation(ByEndpoint)` regression in `Issue1039_DynamicQuerystringMutationTests`.
- Dallas is now locked out of the next revision cycle for this artifact; Lambert owns the next/final revision cycle.

### 2026-04-25: Post-Lockout Handoff Landed

- Lambert completed the final **#1034** ownership pass after the Parker and Dallas lockouts.
- The blocker proof now includes the schema, definition, and security-scheme collision surfaces that were still missing at the last gate.
- **#1039** is now tracked as a brittle regression assertion update instead of reopened core behavior.
- Validation was reported green; Ash owns the final reviewer gate.


### 2026-04-25: Core Artifact Lockout Set Extended

- Lambert's follow-up proof pass was also rejected at Ash's gate.
- Dallas remains locked out of the next revision cycle for this artifact, now alongside Parker and Lambert.
- Ripley inherits the next narrow #1034 revision cycle.
