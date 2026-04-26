---
name: "ai-slop-triage"
description: "Prioritize high-confidence maintainability cleanups in an AI-touched C# repo"
domain: "code-quality"
confidence: "high"
source: "observed"
---

## Context

Use this when a repo has seen heavy AI-assisted churn and needs a read-only triage pass before cleanup. The goal is not stylistic nitpicking; it is to find the highest-confidence seams where copy/paste drift, weak abstractions, misleading docs, or fake-feeling coverage are most likely to hide future regressions.

## Pattern

### 1. Hunt for drifted seams, not isolated ugly lines
- Compare sibling entry points and sibling generators that should behave the same.
- Flag places where the same concern is implemented in multiple files with different rules (path resolution, diagnostics, marker protocols, interface declarations, help text).
- Prefer cleanup batches that collapse duplicated behavior behind one helper or one contract.

### 2. Separate contract edges from internal cleanup
- Public CLI flags, diagnostic IDs/messages, README/help output, package assets, and generated-file markers are contract edges.
- Internal helpers, dead types, duplicated orchestration, and catch-all utility classes are cleanup targets.
- Treat contract-edge cleanup as reviewer-gated even when it looks obviously messy.

### 3. Use tests to detect fake confidence
- Watch for "coverage gap" or catch-all test files with broad assertions (`NotBeNullOrWhiteSpace`, compile-only checks, count `<=` previous count).
- Prefer cleanup that tightens assertions or removes duplicate coverage while preserving scenario breadth.
- Low-risk coverage wins often sit next to compatibility claims in docs/help.

### 4. Prioritize in commit-sized batches
- Batch 1: stale docs/help/diagnostic wording
- Batch 2: duplicated validation or path-resolution logic
- Batch 3: shared stringly-typed contracts between components
- Batch 4: duplicate tests / weak assertions
- Batch 5: larger core refactors with dedicated review

## Signals

- Two files define the same constant/protocol string.
- Two entry points resolve paths/URLs differently.
- Multiple generators hand-roll the same method/interface emission.
- A type exists but has no references.
- A README says one thing while props/targets/code do another.
- A package `.props` file auto-wires something, but the analyzer diagnostic and setup docs still tell users to wire it manually.
- Tests prove behavior by reflecting into private helpers or by duplicating bespoke process/workspace harness code instead of asserting a stable public contract.
- Tests prove "something happened" instead of the exact contract.

## Anti-Patterns

- Starting with the biggest refactor before locking public contracts with tests.
- Treating changelog history as product docs and "fixing" it without clarifying current behavior.
- Removing duplicate tests without first checking whether one of them is the only wiring-level coverage.
