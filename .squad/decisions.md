# Squad Decisions

## 2026-04-18

### P0 Audit Findings - Critical Generator Bugs

**Verified By:** Parker (Core Developer)  
**Status:** ALL VALID

- **#1011**: Source generator crashes IDE/build on duplicate filenames
- **#1012**: CI/CD silently ships stale/missing code on CLI failures
- **#1013**: Regex corrupts generated code, breaks member names
- **#1014**: Breaks Newtonsoft users, silently regresses internal enums (PARTIAL)
- **#1015**: NRE on every Swagger 2.0 document
- **#1016**: Multi-spec merge drops all schemas from split APIs

**Key Architectural Concerns:**
1. Regex-on-raw-source fundamentally unsafe (word boundaries insufficient)
2. Missing null checks in OpenAPI document traversal (Swagger 2.0 vs 3.0)
3. MSBuild task doesn't follow MSBuild contract (returns true regardless of exit code)

**Recommendation:** Fix all P0 before v2.0 release.

---

### P1 Audit Findings - High-Priority Issues

**Verified By:** Lambert (Tester)  
**Status:** 10 VALID, 1 PARTIAL

- **#1017**: AOT context non-compiling (generics, nested types, namespaces)
- **#1018**: ParameterExtractor invalid identifiers (not using IdentifierUtils)
- **#1019**: Security scheme header unsafe (leading digits, keywords)
- **#1020**: Dynamic-querystring self-assign (`_foo = _foo;`)
- **#1021**: CLI --output no longer overrides when settings file used
- **#1022**: MSBuild predicted paths diverge from actual generation
- **#1023**: MSBuild IncludePatterns uses substring matching (fragile)
- **#1024**: Refit 10 leaks to consumers (design decision, PARTIAL)
- **#1025**: OpenApi.Readers 1.x → 3.x silent change
- **#1026**: Auto-enable GenerateOptionalPropertiesAsNullable
- **#1027**: RefitInterfaceGenerator NRE on no content

**Critical:** #1018, #1019, #1020 produce non-compiling code; #1027 crashes on 204 responses.

---

### P2 Medium Audit Findings

**Verified By:** Dallas (Tooling Developer)  
**Status:** 14 VALID, 2 PARTIAL

**Critical Issues (Crashes/Corruption):**
- **#1028**: Source Generator Incremental Caching Defeated (List vs EquatableArray)
- **#1037**: Crash on Empty Namespace List
- **#1039**: Mutation of Shared NSwag Model

**Security/Correctness:**
- **#1035**: XML Doc Injection Vulnerability (unescaped parameter descriptions)
- **#1034**: Silent Data Loss in Multi-Spec Merge

**Type System Issues:**
- **#1036**: Nullable Parameter Mis-classification
- **#1038**: Reference Type Nullability (CS8632 errors)

**Tooling Issues:**
- **#1029**: Source Generator Silent Warnings (Debug.WriteLine no-op)
- **#1041**: MSBuild Task Multiple Failure Modes
- **#1043**: Breaking CLI Change (bool flag → enum)

**Partial Issues:**
- **#1032**: JsonConverter Semantics (runtime verification needed)
- **#1042**: Spectre.Console.Cli version bump (smoke testing needed)

---

### P2 Low Audit Findings

**Verified By:** Ripley (Lead)  
**Status:** 13 VALID, 0 PARTIAL

All issues appropriately classified. Systemic patterns identified:

1. **Settings Validation Gaps** (#1044, #1045, #1046)
2. **Parsing Fragility** (#1047, #1050, #1051)
3. **Double-Read/Double-Process** (#1048, #1052)
4. **Keyword Handling Gaps** (#1053)
5. **Library Async Best Practices** (#1049)
6. **Fragile Ordering Dependencies** (#1055, #1056)

Recommendation: Address incrementally in 2.1.x patches.

---

### Breaking Changes Guidance Plan

**Decided By:** Bishop (Docs Specialist)  
**Status:** APPROVED FOR PUBLICATION

**Deliverables Created:**
1. GitHub Discussion draft (ready to publish)
2. Migration guide in docs/ (breaking-changes-v2-0-0.md)
3. Documentation index updated (toc.yml)

**Publication Strategy:**
- Create Discussion under Announcements category
- Pin for 2-3 weeks during v2.0.0 adoption
- Link from CHANGELOG and README

**Reviewed By:** Ripley (Lead) - ✅ APPROVED

---

## 2026-04-20

### PR #1064 Squad Review: v2.0 Audit Fix Status

**Decision Date:** 2026-04-20  
**PR:** #1064 ([v2.0 audit] Fix pre-release regressions from #1057)  
**Branch:** v2.0.0-prerelease-audit  
**Verdict:** **NO MERGE YET** — 5 confirmed blockers pending resolution

#### Review Lanes & Findings

**Bishop (Documentation)** — ✅ READY
- Breaking-changes docs accurate and complete
- 29 issues closed with real code fixes verified
- Optional post-merge improvements: README link, CLI precedence clarity, security fix highlight
- Recommendation: APPROVE (non-blocking gaps only)

**Dallas (Tooling)** — ❌ NOT READY
- Blocker #1011: Source generator hint-name collision on same-directory duplicates (partial fix)
- Blocker #1021: CLI `--output` override ignored in multi-file settings-file flow (partial fix)
- Blocker #1050: Enum-error guidance only added to CLI; source generator still raw (partial fix)
- Verified #1012: MSBuild exit-code handling correct

**Ash (Safety)** — ❌ NOT READY
- Blocker #1013: ContractTypeSuffixApplier missing suffix-target collision detection (no check for `Foo` + `FooDto` → `FooDto` duplicate)
- Blocker #1018: ParameterExtractor multipart dedup uses original key, not sanitized name (`"a-b"` + `"a b"` → duplicate `"a_b"`)
- Both are compilation-breaking; must fix before merge

**Ripley (Issue Matrix)** — ❌ NOT READY
- Blocker #1053: `Sanitize()` returns unescaped keywords (`@class`, missing `__*` set); no `EscapeReservedKeyword()` routing
- Blocker #1021: Multi-file precedence guard incomplete
- Blocker #1050: Source generator enum guidance not improved
- Supporting blockers from Ash (#1013, #1018)
- Awaiting Parker on #1040 (timeout config), #1050 (enum error handling)

#### Confirmed Must-Fix Blockers (5 Items)

| Issue | File | Gap | Fix |
|-------|------|-----|-----|
| #1013 | ContractTypeSuffixApplier.cs | No collision check | Add guard for duplicate targets |
| #1018 | ParameterExtractor.cs | Dedupe by wrong key | Dedupe by sanitized identifier |
| #1021 | GenerateCommand.cs | Multi-file ignores `-o` | Restore override guard + test |
| #1050 | RefitterSourceGenerator.cs | CLI-only guidance | Catch + re-throw with context |
| #1053 | IdentifierUtils (call sites) | No keyword routing | Route through `EscapeReservedKeyword` |

#### Evidence Summary

**Resolved (20/28):** P0 all 7 fixed; P1 partial fixes; P2 mostly silent improvements  
**Partial (6/28):** #1013, #1018, #1021, #1050, #1053, #1019  
**Unresolved (1/28):** #1053 (coordinator spot-check)  
**Awaiting (1/28):** #1040 (Parker review)  

#### Recommendation

- **Request blocker fixes:** ~30 minutes estimated work
- **Re-run full test suite** after fixes
- **Final gate:** All blockers resolved + tests passing → APPROVE FOR MERGE
- **Nice-to-have:** Parker/Lambert confirmations on #1040, #1019

#### Agents Still Running

- **Parker (Core Developer):** Awaiting verdict on #1040 (HttpClient timeout) + #1050 (enum errors)
- **Lambert (Tester):** Optional confirmation on #1019 (edge cases), #1021 (CLI regression)

---

## 2026-04-17

### Release Compatibility Audit: 1.7.3 → HEAD (All Agents Consensus)

**Verdict:** BREAKING CHANGES FOUND. Cannot be marketed as non-breaking release. Major version bump (2.0.0) required.

#### Breaking Changes (2 Confirmed)

1. **Auth Property Renamed (MEDIUM RISK)**
   - `.refitter` setting: `generateAuthenticationHeader` (bool) → `authenticationHeaderStyle` (enum: None, Method, Parameter)
   - No backward compatibility layer or JSON mapping
   - Old JSON key silently ignored; defaults to `AuthenticationHeaderStyle.None`
   - Affected: users with `"generateAuthenticationHeader": true` in `.refitter` files
   - Evidence: Commits 7dbf6c0c, 14101a49; confirmed by Lambert's deserialization tests
   - Migration: Replace `"generateAuthenticationHeader": true` with `"authenticationHeaderStyle": "Method"` or `"Parameter"`

2. **Source Generator Disk Files (HIGH RISK)**
   - Source generator no longer writes `.g.cs` files to disk
   - Changed from `File.WriteAllText()` to `context.AddSource()` (Roslyn best practice)
   - Fixes issues #635, #520, #310 (file locking, process access errors)
   - Affected: source generator users expecting physical files in `./Generated` folder
   - Users must view generated code via IDE or switch to CLI/MSBuild for disk files
   - Evidence: Commit f853bcf2 (PR #923); confirmed by Dallas tie-breaker audit

#### Non-Breaking Changes

- **MSBuild output path fix (Issue #998):** NOT a breaking change. MSBuild now respects default `./Generated` instead of incorrectly outputting to `.refitter` directory. This is a bug fix, not a break. Users relying on old buggy behavior can set `"outputFolder": "."` explicitly.
- **8 Additive Features** (all backward compatible with safe defaults):
  - PropertyNamingPolicy (defaults to PascalCase)
  - OpenApiPaths (multi-spec merge)
  - ContractTypeSuffix
  - GenerateJsonSerializerContext (AOT)
  - SecurityScheme filtering
  - CustomTemplateDirectory
  - New CLI options for all above
  - Auto-enable GenerateOptionalPropertiesAsNullable (scoped)
- **4 Bug Fixes** (only affect previously broken inputs):
  - Stack overflow in recursive schemas
  - Digit-prefixed property naming (invalid C# identifiers)
  - Multipart form-data parameter extraction
  - OneOf discriminator handling
- **Generated Code Quality Improvements:**
  - JsonConverter attribute placement: properties → enum types (semantically equivalent)
  - Method naming in ByTag mode: numeric suffixes now scoped per-interface

#### Release Recommendation

- **Version:** 2.0.0 (major bump required)
- **CHANGELOG:** Document both breaking changes with clear migration paths
- **Migration Guide:** Provide search/replace instructions and generated-code viewing guidance
- **Timeline:** All agents aligned; ready for release decision

#### Agents Aligned

✅ Ripley (Lead): BREAKING CHANGES FOUND - cannot approve as non-breaking  
✅ Parker (Core Dev): BREAKING CHANGE DETECTED in auth settings surface  
✅ Dallas (Tooling Dev): CONFIRMED 2 breaking changes; bug fix is non-breaking  
✅ Lambert (Tester): BREAKING CHANGE CONFIRMED with concrete deserialization evidence  

---

## 2026-04-16

- Squad initialized for Refitter.
- Team root uses the worktree-local strategy at `C:\projects\christianhelle\refitter`.
- Shared append-only Squad files use Git's `union` merge driver.
- **Issue #998 Investigation Complete:** Verdict is a real product bug, not user error. CLI ignores `outputFolder` when it equals the default `./Generated`, causing MSBuild to search for files in wrong location. First clean build fails due to sync mismatch between MSBuild prediction and CLI output. Fix: remove default-value check in `GenerateCommand.cs:648`.
