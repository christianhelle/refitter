# Dallas History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Validated MSBuild tooling path. CLI loads settings correctly (naming honored). Single-file output without explicit `outputFilename` falls back to `Output.cs` and skips `./Generated` folder when it matches default. Real product bug: MSBuild expects wrong file locations on first clean build.

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

