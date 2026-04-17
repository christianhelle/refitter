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
