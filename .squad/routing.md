# Routing

| Signal | Route |
| --- | --- |
| Issue triage, scope decisions, reviewer gates, cross-cutting changes | Ripley |
| Core generation logic, schema traversal, NSwag or NJsonSchema behavior | Parker |
| CLI options, `.refitter` handling, source generator, MSBuild, CI | Dallas |
| Reproduction steps, regression tests, validation, edge cases | Lambert |
| README, docs, examples, issue wording, usage guidance | Bishop |
| Roslyn rewriting, source generator hint names, MSBuild process handling, safety-sensitive sanitization | Ash |
| Team memory, orchestration logs, decision merges | Scribe |
| Backlog monitoring, issue pickup, PR flow checks | Ralph |

## Default Collaboration

- Bug or regression investigation: Ripley + Lambert + Bishop
- Core generator changes: Parker + Lambert
- Tooling or build pipeline changes: Dallas + Lambert
- High-risk generator safety review: Parker + Ash
- MSBuild or source-generator failure analysis: Dallas + Ash
- Documentation mismatches: Bishop + Ripley
