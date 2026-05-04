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
- **2026-05-04T13:23:00Z issue #1088 execution gate:** Ripley has now blocked code/test work until the team has the full user repro (spec + .refitter + package versions) or a minimized repo-local case that fails on the same consumer dependency lane. The current local matrix still compiles against Refit.HttpClientFactory 8.0.0, 9.0.2, and 10.1.6.
- **2026-05-04T15:23:00.022+02:00 issue #1088 reconstructed user snippet:** Rebuilding the reporter's GET path and `Form` schema verbatim, then adding the smallest same-path POST sibling because the comment omitted POST details, shows the split is driven by multi-verb path suffixing rather than tags or a proven consumer compile break. Repo-local repro assets live under `.test-work\issue-1088-repro`; GET-only and POST-only each stay `Form`, default plus `MultipleClientsFromFirstTagAndPathSegments` produce `FormGet`/`FormPost`, `SingleClientFromPathSegments` expands to `ApiProcessElementFormGet`/`ApiProcessElementFormPost`, and an operationId-based probe preserves explicit `FormGET`/`FormPOST`. Refit-generated implementation stubs matched the interface names on Refit.HttpClientFactory 8.0.0, 9.0.2, and 10.1.6.
- **2026-05-04T15:23:00.022+02:00 issue #1088 test-shape decision:** To minimize overlap with Parker, I anchored the new coverage in a dedicated scenario file `src\Refitter.Tests\Scenarios\Issue1088SamePathMultiVerbMethodCasingTests.cs` and only touched `MultipleInterfacesByTagsWithSamePathSegmentTests.cs` where permissive `InfoGet`/`InfoGET` assertions were masking the exact casing contract. The durable pattern is to pair same-path multi-verb exact-name assertions (`FormGET`/`FormPOST`) with compile-backed checks, while sibling single-verb interfaces (`IAccountApi`, `IUserApi`) must be asserted as unsuffixed `Info(...)` methods so adjacency tests cannot hide casing drift.

- **2026-05-04T13:23:00.022Z issue #1088 completion:** The durable regression contract is now exact same-path multi-verb legacy casing (`FormGET`/`FormPOST`) plus compile-backed validation; nearby by-tag tests should never allow alternate `InfoGet`/`InfoGET` shapes if they are meant to protect consumer-visible method-name casing.

## Archived Detail

- Older detailed tester notes and evidence were summarized into `history-archive.md` on 2026-05-04T06:59:00Z to keep this file scannable.
- **2026-05-04T15:23:00.022+02:00 issue #1088 repro matrix:** The exact FormGet interface vs FormGET implementation mismatch is still not locally reproducible. A minimized GET+POST-on-the-same-path spec confirms the naming boundary instead: src\Refitter at 1.7.3 emits FormGET/FormPOST, while 2.0.0 and current HEAD emit FormGet/FormPost; local consumer builds against Refit.HttpClientFactory 8.0.0, 9.0.2, and 10.1.6 all compiled successfully.
