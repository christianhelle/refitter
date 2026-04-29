---
name: "pr-scope-framing"
description: "Assemble honest PR titles/bodies when a branch contains multiple landed commit lanes"
domain: "github, review"
confidence: "high"
source: "earned"
last_updated: "2026-04-29T11:51:36.530+02:00"
---

## Context

Use this when opening a PR from a branch that has accumulated several already-committed changes, especially if the most recent commit is narrower than the full diff reviewers will receive.

## Pattern

- Check whether a PR already exists for the branch before drafting new wording.
- Summarize the full branch diff against the target base branch, not just `HEAD~1..HEAD`.
- Choose a title/body that matches the branch's aggregate scope and call out any especially important fix as a bullet, not the whole frame, unless it truly dominates the diff.
- If branch metadata or team-history commits are already present, disclose them briefly so reviewers are not surprised by non-product files in the PR.

## Anti-patterns

- Titling a PR after only the latest fix when the branch contains multiple unrelated or semi-related commits.
- Hiding already-committed coordination files that will obviously appear in review.
- Reusing stale issue-closure language without re-checking the actual branch contents and current PR state.
