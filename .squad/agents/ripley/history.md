# Ripley History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- The `generateAuthenticationHeader` boolean setting was renamed to `authenticationHeaderStyle` (enum) between 1.7.3 and HEAD. This is a silent config-file break.
- Source generator switched from `File.WriteAllText` to `context.AddSource()` in PR #923. This is the correct Roslyn pattern but breaks users who relied on physical files.
- Default single-file output location changed from `.refitter` directory to `./Generated` subdirectory (issue #998 fix).
- Key compatibility surfaces: `.refitter` JSON schema, CLI option names, generated code shape, source generator behavior, MSBuild task predicted paths.
- OasReader jumped from v1.x to v3.x — a major dependency version change that affects OpenAPI parsing.
- Version is already bumped to 1.8.0 in commit `983ad149`.

### 2026-04-17: Release Compatibility Audit Completion

**Task**: Lead release compatibility audit for 1.7.3 → HEAD (4-agent parallel team).

**Verdict**: BREAKING CHANGES FOUND. Cannot be marketed as non-breaking. All agents aligned.

**Key Findings**:
- ✗ BREAKING: `generateAuthenticationHeader` → `authenticationHeaderStyle` (silent failure on old keys)
- ✗ BREAKING: Source generator no longer writes disk files (uses Roslyn `context.AddSource()`)
- ✓ NOT BREAKING: MSBuild output path fix (bug fix, makes consistent with CLI)
- ✓ 8 additive features with safe defaults
- ✓ 4 bug fixes for previously broken inputs

**Recommendation**: Major version bump to 2.0.0. Document both breaking changes with clear migration paths.
