# Scribe History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **2026-04-25: Lambert help-output consolidation:** Active decisions now archive older sections once decisions.md grows past ~20 KB, and Spectre.Console.Cli help regressions should be recorded as semantic-marker expectations rather than exact layout snapshots.
- **2026-04-25: Linux help-output merge:** When Dallas proves a formatter/noise failure and Lambert lands the test-only stabilization, record the root cause as ANSI/wrapping noise in raw Spectre output and keep the decision phrased as a semantic-assertion contract.
- **2026-04-25: Coverage-closure merge:** When Lambert isolates residual coverage gaps and Dallas closes them with test-only coverage, merge the inboxes into a single approved decision that preserves the no-production-change rationale and the final validation evidence.
- **2026-04-26: AI-slop kickoff merge:** When Ripley sets cleanup order, Lambert confirms the green baseline, and Dallas lands a docs-only clarification commit, record the lane order in decisions.md, mirror the shared state into the affected agent histories, and keep the session log focused on readiness rather than implementation detail.
- **2026-05-04T06:59:00Z squad logging cycle:** When decisions.md crosses 20 KB, archive stale decision sections before merging new inbox items; for agent-planning sessions, log one orchestration file per spawned agent, keep the session log brief, and summarize any agent history that grows past 15 KB before appending cross-agent context.
- **2026-05-04T13:23:00Z issue #1088 repro-gate logging:** decisions.md stayed below the archive threshold before merge (14383 bytes), two inbox entries were merged, orchestration logs were written for Ripley, Lambert, and Coordinator, and cross-agent histories now record that issue #1088 remains blocked pending an exact failing repro on the same consumer dependency lane.
