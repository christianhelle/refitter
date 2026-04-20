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

### 2026-04-18: P2-Low Audit Verification

**Task**: Verify all 13 P2-Low issues from v2.0 audit against current codebase to confirm validity and severity classification.

**Methodology**: Read each GitHub issue, locate referenced code, check if bug exists, classify as VALID/INVALID/PARTIAL.

**Key Findings**:
- **Severity Calibration**: P2-Low classification generally appropriate for all issues
  - Most are edge-case handling, cosmetic issues, or performance optimizations
  - None threaten data integrity or core functionality
  - ConfigureAwait(false) absence is correctly classified P2 (library best practice but no confirmed deadlocks)
  
- **Patterns Identified**:
  1. **Settings validation gaps** (#1044, #1045, #1046): Settings precedence and normalization inconsistencies
  2. **Parsing fragility** (#1047, #1050, #1051): Regex-based JSON parsing, poor enum error messages, malformed escape sequence handling
  3. **Double-read/double-process** (#1048, #1052): CLI reads .refitter twice; OperationNameGenerator runs full pipeline twice
  4. **Keyword handling gaps** (#1053): Missing contextual keywords and `__*` keywords; Sanitize() doesn't escape
  5. **Library async best practices** (#1049): No ConfigureAwait(false) in library code (pre-existing pattern expanded)
  6. **Fragile ordering dependencies** (#1055, #1056): Interface generator construction order and de-dup state management

**Audit Quality Assessment**: 
- ✅ All 13 issues verified as VALID
- ✅ Severity classifications accurate (all P2-Low appropriate)
- ✅ Code references precise (all line numbers and file paths correct)
- ✅ Suggested fixes reasonable and scoped appropriately

**Systemic Issues**: 
- Settings validation is reactive rather than proactive (multiple validation gaps)
- Parsing relies on regex in MSBuild task instead of proper JSON deserialization
- Performance optimizations deferred (double-parsing, double-processing)
- Library async patterns inconsistent (no ConfigureAwait discipline)

**Recommendation**: Audit quality is high. These issues can be addressed incrementally in 2.1.x patch releases after 2.0 stable.

### 2026-04-20: PR #1064 Issue-to-Evidence Matrix

**Task**: Build comprehensive issue-to-evidence matrix for 28 closed issues in PR #1064 v2.0 prerelease fix.

**Methodology**:
- Collated existing audit findings (Parker P0, Dallas P0/P1/P2, Lambert P1, Ripley P2-Low)
- Cross-referenced with Coordinator spot-checks on #1013, #1018, #1053
- Integrated Ash's automated review comments from PR thread
- Classified each issue as resolved/partial/unresolved/awaiting-reviewer

**Key Findings**:

**Resolution Breakdown**:
- 20 issues fully resolved (7 P0, 2 P1, 11 P2)
- 6 issues partially resolved (critical gaps in 3)
- 1 unresolved (#1053)
- 1 awaiting-reviewer (#1040)

**Blocker Set (No-Merge Condition)**:
1. **#1053: IdentifierUtils.Sanitize() not escaping keywords** — UNRESOLVED
   - Sanitize() returns invalid identifiers like @class, @record
   - Missing __arglist, __makeref, __reftype, __refvalue keywords
   - No routing through EscapeReservedKeyword
   
2. **#1013: ContractTypeSuffixApplier no collision detection** — PARTIAL
   - Missing guard for duplicate FooDto when both Foo and FooDto exist
   - No skip for already-suffixed names
   
3. **#1018: ParameterExtractor multipart collision not deduped** — PARTIAL
   - Different property names sanitizing to same C# identifier not deduplicated
   - Example: "a-b" and "a b" both → "a_b"
   
4. **#1021: CLI --output precedence logic unclear** — PARTIAL
   - Guard restoration from #998 fix may be incomplete
   - Needs regression test
   
5. **#1050: Enum deserialization errors not actionable** — PARTIAL
   - Generic JsonException; unclear if caught + re-thrown with context

**Evidence Sources**:
- Parker's P0 audit (6 confirmed fixes)
- Dallas's P0/P1/P2 audit (comprehensive)
- Lambert's P1 audit (2 confirmed fixes)
- Ripley's P2-Low audit (13 confirmed valid)
- Ash's automated PR review (3 collision gaps identified)
- Coordinator spot-check (keyword escaping gap confirmed)

**Provisional Verdict**: DO NOT MERGE until blockers cleared. Recommendation: fix 3 critical gaps (#1013, #1018, #1053 one-liners) in ~30 minutes, then merge.

**Record Created**: `.squad/decisions/inbox/ripley-pr1064-evidence-matrix.md`
