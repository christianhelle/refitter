# Lambert History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **2026-04-25 CLI help repro:** src\Refitter\Program.cs intentionally rewrites a no-argument invocation to --help, exits 0, and emits Spectre.Console.Cli help output. Tests in src\Refitter.Tests\GenerateCommandTests.cs should assert semantic help markers like usage, sections, and option names rather than exact formatter-driven spacing.
- **2026-04-25 Linux help-test follow-up:** GitHub Actions on Ubuntu still showed the semantic help text, but the raw redirected Spectre output did not satisfy the single regex assertion. The safe regression contract is to normalize console control sequences/line endings first and then assert semantic help markers (`USAGE`, usage text, sections, known option names).
- **PR #1064 / #1057 testing pattern:** When blocker work is in flux, Lambert's safest lane is minimal repro specs plus compilation gates, then focused test reruns once the implementing lane lands.
- **2026-04-26 baseline scan:** The current local validation baseline is green on `dotnet build -c Release src\Refitter.slnx --no-restore`, `dotnet test -c Release src\Refitter.slnx --no-build`, and `dotnet format --verify-no-changes src\Refitter.slnx --no-restore`; the solution test run passed 1894 tests with 0 failures.
- **2026-04-26 coverage baseline:** The existing repo coverage lane is the Codecov command from `.github\workflows\codecov.yml`: `dotnet test --project src\Refitter.Tests\Refitter.Tests.csproj -c Release --coverage --coverage-output coverage.cobertura.xml --coverage-output-format xml`. That run passed 1848 tests and reported module coverage of `Refitter.Core.dll` 94.65% line / 96.04% block, `refitter.dll` 97.10% / 99.11%, `Refitter.MSBuild.dll` 100% / 100%, plus `.test-work` `RuntimeProof.dll` at 80.93% / 71.58% from generated proof artifacts.
- **2026-04-26 low-risk coverage targets:** The safest real source files to extend without behavior changes are `src\Refitter.Core\ContractTypeSuffixApplier.cs`, `src\Refitter.Core\SchemaCleaner.cs`, `src\Refitter.Core\CSharpClientGeneratorFactory.cs`, `src\Refitter.Core\XmlDocumentationGenerator.cs`, `src\Refitter.Core\IdentifierUtils.cs`, and `src\Refitter\Settings.cs`, because they already have focused tests nearby (`CoverageGapTests`, `SchemaCleanerTests`, `CSharpClientGeneratorFactoryTests`, `XmlDocumentationGeneratorTests`, `IdentifierUtilsTests`, `SettingsTests`) and remaining gaps are narrow branch cases.
- **2026-04-26 environment-sensitive tests:** External URL coverage still lives in `src\Refitter.Tests\OpenApiDocumentFactoryTests.cs`, `src\Refitter.Tests\SwaggerPetstoreTests.cs`, `src\Refitter.Tests\SwaggerPetstoreApizrTests.cs`, and `src\Refitter.Tests\Examples\OpenApiUrlTests.cs`; these should stay out of flaky-baseline conclusions during cleanup because they depend on live network access.

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

## 2026-04-25: Linux Help Output Fix Landed

- Dallas's Ubuntu log analysis proved the failure was raw ANSI/wrapping noise from Spectre.Console help output rather than a CLI product bug.
- Lambert changed only src\Refitter.Tests\GenerateCommandTests.cs, normalizing redirected console output and asserting semantic help markers instead of formatter-specific layout.
- Reported final validation: dotnet build -c Release src\Refitter.slnx, dotnet test -c Release src\Refitter.slnx, and dotnet format --verify-no-changes src\Refitter.slnx.

## 2026-04-25: RefitterGenerateTask coverage gap analysis

- Coverage evidence from `src\Refitter.Tests\bin\Release\net10.0\TestResults\coverage-all.xml` leaves `src\Refitter.MSBuild\RefitterGenerateTask.cs` at **91.86% line / 94.29% block** coverage.
- Exact uncovered lanes are: `TryExecuteRefitter()` exception handling (lines 143-147), missing bundled CLI handling in `StartProcess()` (171-173), unresolved package-folder / co-located CLI / final-first-bundle fallbacks in `ResolveRefitterDll()` (304, 344-348, 351-353), the `<1000 ms` arm of `FormatTimeout()` (357-361 partial), and the non-throwing `Log.LogErrorFromException(e)` path (370).
- Full `Refitter.Tests` coverage run still hit the known network-dependent failures (`IsHttp_Detects_Https_Protocol` and `Can_Build_Generated_Code_From_Url(...)`), but it produced the decisive per-function coverage report needed for Dallas.

## 2026-04-25: RefitterGenerateTask coverage closure landed

- Dallas used the isolated branch list to add only regression coverage in `src\Refitter.Tests\RefitterGenerateTaskTests.cs`; no product behavior changes were needed in `src\Refitter.MSBuild\RefitterGenerateTask.cs`.
- The landed test pass covered exception handling, missing bundled CLI failure, blank package folder handling, whitespace runtime probing, co-located and first-bundled fallback resolution, millisecond timeout formatting, and successful `LogErrorFromException` forwarding.
- Reported validation: `dotnet test --project src\Refitter.Tests\Refitter.Tests.csproj -c Release --coverage --coverage-output coverage.cobertura.xml --coverage-output-format xml`, `dotnet build -c Release src\Refitter.slnx --no-restore`, and `dotnet format --verify-no-changes src\Refitter.slnx --no-restore`.
- Reported end state: `src\Refitter.MSBuild\RefitterGenerateTask.cs` at 100% line coverage, 100% block coverage, and 0 partial functions.

## 2026-04-26: Shared AI-slop cleanup context

- Ripley's triage keeps docs/help drift as the first low-risk cleanup batch, followed by settings/spec-path normalization and the shared `GeneratedFile:` marker contract.
- Dallas has already landed commit `f6374210` (`docs: clarify source generator setup`) with narrow validation green, so the doc-drift lane is actively moving without disturbing the baseline.
- Keep using the local green baseline plus focused coverage-safe files as the main cleanup signal; live-URL tests remain environment-sensitive and should stay out of pass/fail conclusions.

