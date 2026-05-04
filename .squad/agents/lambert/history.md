# Lambert History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- **2026-05-04T08:59:00.400+02:00 issue #1088 method-casing gap:** I could not reproduce the reported `FormGet`/`FormGET` mismatch on current HEAD with minimal inline specs. The durable gap is exact acronym/HTTP-verb casing coverage: nearby naming tests still permit alternate casing and do not pin the expected `GET` -> `Get` contract.
- **2026-05-04T06:59:00Z issue #1088 squad alignment:** Ripley, Parker, Dallas, and Lambert converged on a repro-first plan: add exact casing assertions plus compile-backed core/source-generator parity coverage before any shared naming patch. Current evidence still points to a contract-sensitive generated-interface boundary or downstream Refit interaction, not separate CLI/MSBuild/source-generator wiring bugs.
- **2026-05-01 issue #1083 regression contract:** Keep dotted-schema-name coverage isolated in a minimal compile-backed scenario that proves no blank class names, no `Task<>`, and consistent sanitized DTO usage.
- **2026-05-01 issue #1083 repro:** Current HEAD reproduces the trailing-dot schema failure both on the Revenue spec and on a minimal inline Swagger 2 fixture, with `CS1001 Identifier expected` on the generated output.
- **2026-04-28 e-conomic multi-spec merge evidence:** `test\economic.refitter` originally failed before code generation because shared `Error` / `ProblemDetails` schemas tripped duplicate-schema merge handling; merge regressions need equivalent-schema and compile-backed coverage.
- **2026-04-26 validation baseline:** The durable local baseline is release build + release tests + format verification; live-URL tests stay environment-sensitive and should not drive pass/fail conclusions.
- **2026-04-25 CLI help contract:** Spectre.Console.Cli help regressions should be asserted semantically after output normalization, not by exact formatter spacing.
- **2026-04-25 blocker-test pattern:** When blocker work is still moving, prefer minimal repro specs plus compilation gates first, then tighten focused assertions after the implementation lane lands.

## Archived Detail

- Older detailed tester notes and evidence were summarized into `history-archive.md` on 2026-05-04T06:59:00Z to keep this file scannable.
