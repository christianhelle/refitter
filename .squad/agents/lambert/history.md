# Lambert History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **2026-04-25 CLI help repro:** src\Refitter\Program.cs intentionally rewrites a no-argument invocation to --help, exits 0, and emits Spectre.Console.Cli help output. Tests in src\Refitter.Tests\GenerateCommandTests.cs should assert semantic help markers like usage, sections, and option names rather than exact formatter-driven spacing.
- **PR #1064 / #1057 testing pattern:** When blocker work is in flux, Lambert's safest lane is minimal repro specs plus compilation gates, then focused test reruns once the implementing lane lands.

## Core Context

- **2026-04-17 release compatibility audit:** Confirmed two real breaking changes from 1.7.3 → HEAD: the silent `.refitter` rename from `generateAuthenticationHeader` to `authenticationHeaderStyle`, and the source generator move from disk-written `.g.cs` files to Roslyn `AddSource()` output.
- **2026-04-18 P1 audit verification:** Validated ten high-priority issues and one partial, with the sharpest failure patterns in serializer-context regex parsing, identifier sanitization, dynamic querystring self-assignment, CLI precedence, and null content handling.
- **2026-04-20 PR #1064 blocker coverage:** Built regression tests around suffix-target collisions, multipart deduplication on sanitized identifiers, and keyword/title handling; later confirmed the blocker suite green once fixes landed.
- **2026-04-20 remaining P1 worktree audit:** Confirmed the tooling path for `GeneratedFile:` markers, flagged the netstandard build break and missing polymorphism/runtime proof for #1017, and kept #1024/#1025 open pending package and smoke-test evidence.

## 2026-04-25: Remaining Audit Repro Pass

- Narrowed the current-HEAD reproducible set to **#1028, #1029 (partial), #1033, #1041 (partial), and #1043**.
- Confirmed **#1032, #1042, #1045, and #1047** as validation-only or fixed-at-HEAD candidates unless stronger failing repros appear.
- Found no current-HEAD repro for **#1034, #1039, and #1056** in the initial tester pass.

## 2026-04-25: Core Blocker-Test Lane

- Ash's rejection kept **#1034** and **#1039** open and initially routed Lambert toward blocker-test coverage for the remaining failures.
- Dallas's later revisions shifted Lambert's lane from "write the first blocker tests" to reconciling blocker expectations against the landed merge and grouped-query behavior.
- After Dallas lockout, Lambert owned the final blocker-test revision for **#1034**, added explicit collision coverage, and reconciled the Issue1039_DynamicQuerystringMutationTests expectation drift.
- Ash still rejected that proof pass because the Swagger 2 definition-collision lane was not isolated cleanly enough, which moved the final narrow revision to Ripley.

## 2026-04-25: CLI Help Output Test Stabilization

- Reproduced the no-argument CLI path and confirmed the product behavior is correct.
- The durable test contract is semantic Spectre.Console.Cli help assertions, not exact whitespace/layout matching.
- Validation reported green for the release Refitter.Tests run, a focused rerun of Program_Main_Should_Show_Help_When_Invoked_Without_Arguments, and format verification.

