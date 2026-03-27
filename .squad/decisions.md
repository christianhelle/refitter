# Decisions — Refitter Squad

## Active Decisions

### 11. Protect Confidential `tmp/` Folder (2026-03-26)

**Assigned:** McManus (DevOps)  
**Status:** ✅ IMPLEMENTED  
**Severity:** P1 — Security/Confidentiality  

#### Problem

User directive (2026-03-26T23:15:37Z – 23:16:00Z): Clarified that files under `tmp/` are **confidential** and must **NEVER** reach the public repository.

**Current Risk:**
- `tmp/` folder exists locally with confidential files (email.txt, api.json, api.refitter)
- Folder was never committed but is untracked and could be accidentally added via `git add .`
- Not in `.gitignore` — vulnerable to developer or CI/CD accidents

#### Solution Implemented

**Layer 1: Root `.gitignore` Protection**
- Added `tmp/` rule to `.gitignore` (line 287)
- Comment: `# Confidential temporary files — must not reach public repository`

**Layer 2: Nested `.gitignore` (Defense in Depth)**
- Created `tmp/.gitignore` with wildcard `*`
- Catches any file added to `tmp/` regardless of root config
- Matches existing codebase pattern

#### Verification

- ✅ `git check-ignore -v tmp/ tmp/email.txt tmp/.gitignore` — all paths ignored
- ✅ Confidential files remain locally available
- ✅ Git will refuse to stage any files under `tmp/`

#### Decision

**APPROVED:** Two-layer ignore protection eliminates risk of accidental public exposure. Confidential assets secured. No further action required.

---

### 10. Issue #967 — Stack Overflow in Recursive Schema Traversal (2026-03-26)

**Status:** ✅ APPROVED FOR MERGE  
**Date:** 2026-03-26  
**Severity:** P1 — Pre-existing bug causing crashes with circular $ref specs

#### Root Cause Analysis

**Verdict: Pre-existing bug, NOT caused by PR #969**

Stack overflow occurs in CSharpClientGeneratorFactory.ProcessSchemaForMissingTypes() (line 179) and ProcessSchemaForIntegerType() (line 285). Both methods recursively traverse the NJsonSchema graph through Properties, Item, AdditionalPropertiesSchema, and AllOf/OneOf/AnyOf without any visited-set to prevent revisiting schemas. Any OpenAPI spec with circular $ref references triggers infinite recursion → stack overflow.

**Evidence:**
- ProcessSchemaForMissingTypes introduced in commit 8ce7259d (Jan 2026)
- ProcessSchemaForIntegerType introduced in commit dc53d898 (earlier)
- PR #969 only added property-name-generator routing; zero changes to vulnerable methods
- Existing safe pattern in SchemaCleaner.FindUsedJsonSchema() uses HashSet<JsonSchema> with cycle detection

#### Implementation Summary

**By Fenster:**
- Replaced duplicated recursive preprocessing with one shared iterative document-level schema visitor
- Implemented using Stack<JsonSchema> to prevent stack overflow
- Resolved schema.ActualSchema once per visit
- Traversal covers Properties, Item, AdditionalPropertiesSchema, AllOf, OneOf, AnyOf

**By Hockney:**
- Three-layer regression matrix with recursive schema fixtures
- Tests anchored to public behavior, not implementation details
- Source-generator parity via reflection-based assertions

**By McManus:**
- MSBuild test coverage with PreserveOriginal config
- CLI smoke-test PreserveOriginal variant
- Workflow update with separate PreserveOriginal step
- Format blocker fix: DesignTimeBuild exclusion for source-generator tests

#### Validation Status

✅ **Build:** PASS  
✅ **Tests:** PASS (1,473 tests including recursive schema coverage)  
✅ **Format:** PASS  

#### Real-World Repro Validation (2026-03-26)

**By Hockney (Tester):**
- Validated fix against user's real 666KB OpenAPI 3.0.4 spec with 59 paths
- Stack overflow resolved: CLI completed in 2.17 seconds (previous versions crashed)
- Generated 18 files (53.3 KB, 1,426 lines) with all 22 excludedTypeNames respected
- Generated code compiles successfully with test stubs
- PreserveOriginal property naming applied correctly; all features functional
- Regression suite: 1,473/1,473 tests passed (net8.0 + net10.0)

**By McManus (DevOps):**
- Validated MSBuild/build-surface integration with same repro bundle
- CLI direct generation: 497 KB output (12,626 lines) in 1.63 seconds ✅
- PreserveOriginal feature test: 3.0 KB metadata in 1.02 seconds ✅
- MSBuild petstore integration: generated, compiled, executed successfully ✅
- Full test suite: 1,473/1,473 tests passed in 37.9 seconds ✅
- No design-time, build-time, or functional caveats identified
- Coverage confirmed: CLI, property naming policy, excluded types, complex schemas, MSBuild, source generator

#### Decision

**APPROVED FOR RELEASE:** Fix root cause with visited-set cycle detection. Minimal, safe, matches established pattern. Real-world repro validates iterative visitor handles edge cases beyond synthetic fixtures. Production-ready. Include in preview release 1.8.0-preview.101.

