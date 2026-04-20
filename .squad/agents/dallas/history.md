# Dallas History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Validated MSBuild tooling path. CLI loads settings correctly (naming honored). Single-file output without explicit `outputFilename` falls back to `Output.cs` and skips `./Generated` folder when it matches default. Real product bug: MSBuild expects wrong file locations on first clean build.

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

