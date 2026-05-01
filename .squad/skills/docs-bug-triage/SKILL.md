---
name: "docs-bug-triage"
description: "Decide whether a report is a docs mismatch, a real product bug, or both"
domain: "documentation, triage"
confidence: "medium"
source: "observed"
---

## Context

Use this when an issue report cites expected behavior and you need to judge whether the expectation came from current docs or from the broader product promise.

## Pattern

1. Reproduce or confirm the current behavior first.
2. Read the exact doc section that could have created the expectation and note its scope.
3. Separate **explicit docs promise** from **implicit product expectation**:
   - If the docs scope the behavior to one surface (for example, property names), do not extend that promise to another surface (for example, schema type names).
   - If the product emits broken output, still treat it as a real bug even when the docs never promised that exact sanitization behavior.
4. In maintainer wording, say whether the docs are wrong, correct-but-easy-to-misread, or unrelated.

## Output shape

- Evidence: one reproduction fact
- Docs verdict: what current docs do and do not promise
- Communication verdict: confirmed bug / needs verification / docs clarification only
