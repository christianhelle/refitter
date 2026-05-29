---
name: "commit-grouping-hygiene"
description: "Split landed work into focused commits and isolate stale automation cleanup"
domain: "git, review"
confidence: "high"
source: "earned"
last_updated: "2026-05-29T15:07:47.442+02:00"
---

## Context

Use this when a working tree already contains a completed feature or fix plus docs and leftover automation artifacts, and your job is to turn that diff into reviewable commits without rewriting history.

## Pattern

- Inspect the whole diff before staging anything so you can group files by behavior lane instead of by directory alone.
- Put executable behavior changes first (for example tooling targets and CI proof), then documentation that explains the shipped behavior.
- If the branch contains stale automation debris such as `.squad\commit-msg.txt`, remove it in a final housekeeping commit together with any required Squad history or decision updates.
- Keep housekeeping out of the product commits unless the cleanup is required for the feature to function.
- Use direct `git add <paths>` and `git commit -m "<message>"` commands so the grouping stays deterministic and non-interactive.

## Anti-patterns

- Mixing docs, product logic, and Squad bookkeeping into one commit when the diff naturally separates.
- Leaving temporary commit-message files or other automation leftovers dangling after the product commits are created.
- Amending or force-rewriting commits when the task only requires focused grouping of the current working tree.
