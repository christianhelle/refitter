# Decisions — Refitter Squad

## Active Decisions
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
✅ **Tests:** PASS (1450+ tests including recursive schema coverage)  
✅ **Format:** PASS  

#### Decision

**APPROVED:** Fix root cause with visited-set cycle detection. Minimal, safe, matches established pattern. Ready for preview release 1.8.0-preview.101.

