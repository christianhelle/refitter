## 2026-05-29T15:07:47.442+02:00 - Commit grouping for issue #1094

- Context: the issue #1094 working tree already contained the product changes across MSBuild targets, CI coverage, and user-facing docs, plus a stale `.squad\commit-msg.txt` deletion from earlier automation.
- Decision: group the product diff into two user-facing commits (`tooling or CI` and `docs`) and keep the stale automation cleanup plus Squad bookkeeping in a separate housekeeping commit with no co-author trailer.
- Consequence: reviewers can reason about behavior changes separately from documentation and repository hygiene, while the branch still finishes with a clean working tree.
