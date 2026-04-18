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

### 2026-04-17: Breaking Changes Discussion Post Planning

**Task**: Plan GitHub Discussions post to communicate breaking changes to end users.

**Approach**: 4-phase execution (Verification → Content → Review → Publish)

**Outcomes**:
- Created `plan-breaking-changes-discussion.md` with full execution plan
- Created `.squad/decisions/inbox/ripley-breaking-post-plan.md` decision record
- Identified 4 user ambiguities requiring Christian Helle's confirmation:
  1. Release timing (post before or with 2.0.0 stable?)
  2. Target scope (Refitter-only vs. broader with Refit v10?)
  3. Migration guide companion (Discussion-only vs. formal docs/?)
  4. Automation preference (search-and-replace vs. migration tool?)

**Status**: PLAN READY FOR APPROVAL. Awaiting user input on ambiguities.

### 2026-04-17: Breaking Changes Guidance Review & Approval

**Task**: Review finalized breaking changes discussion draft and migration guide for scope, accuracy, and alignment with squad consensus.

**Deliverables Reviewed**:
1. Discussion post (session artifact): GitHub Discussions format with encouraging tone
2. Migration guide (docs article): Formal reference documentation
3. TOC update (docs/docfx_project/articles/toc.yml): Breaking Changes placed as item 4

**Findings**:
- ✅ Scope correct: Refitter-only changes (v1.7.3 → HEAD), excludes external deps (Refit v10, NSwag)
- ✅ Accuracy verified: Both breaking changes documented with correct migration paths
  - Auth config rename: enum values (None/Method/Parameter) correct, evidence links valid
  - Source generator disk files: IDE alternatives + CLI/MSBuild options documented, issue refs accurate
- ✅ Non-breaking items correctly identified (MSBuild path fix, 8 additive features, 4 bug fixes)
- ✅ Guidance structure complementary: Discussion is community-focused (emoji, tone), docs is reference-focused
- ✅ TOC placement logical (post-capabilities, pre-MSBuild)

**Decision**: ✅ APPROVED FOR PUBLICATION. Both artifacts are production-ready with clear, actionable migration guidance.

**Record Created**: `.squad/decisions/inbox/ripley-review-breaking-guidance.md`

**Key Pattern**: Breaking change communication requires dual artifacts—high-energy discussion post (GitHub Discussions) + formal reference guide (docs site). Docs article also enables persistent link from CHANGELOG and main README.

### 2026-04-18: Scribe Orchestration Consolidation

**Task**: Log orchestration and session artifacts; consolidate team work.

**Completion**:
- ✓ Orchestration logs written for Bishop and Ripley (2026-04-18T10:46:51Z)
- ✓ Session log written for breaking-changes-guidance phase (2026-04-18T10:46:51Z)
- ✓ No decision inbox entries to merge (all decisions already recorded in decisions.md)
- ✓ Cross-agent history updates applied
- ✓ All artifacts ready for git commit
