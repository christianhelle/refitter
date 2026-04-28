# Dallas History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- **2026-04-28T15:21:48.369+02:00 e-conomic multi-spec tooling verdict:** `test\economic.refitter` parses correctly and resolves both relative `openApiPaths`; CLI/MSBuild/source-generator all reach core multi-document merge, where duplicate equivalent schemas (`Error`, then `ProblemDetails`) trigger the fail-fast merge path before validation, so `--skip-validation` cannot help.

- Team initialized on 2026-04-16.
- **2026-04-28 issue #1045 tooling verdict:** current HEAD CLI accepts `.refitter` files that use only `openApiPaths`; the quoted `'openApiPath' is required` failure string is stale pre-#1057 behavior, while the remaining normalization gap is a core/library consistency concern rather than a current CLI/MSBuild break.
- **Issue #998 findings (2026-04-16):** MSBuild's first-clean-build path was the real tooling bug; CLI settings loading and the default single-file `Output.cs` behavior were otherwise correct.
- **PR #1067 tooling proof (2026-04-21):** The most reliable tooling evidence comes from validating real stdout marker contracts and packed artifacts (`.nupkg`/`.nuspec`), not just project metadata or happy-path check runs.
- **2026-04-25 Linux help-output analysis:** The Ubuntu help failure was a test portability problem caused by raw Spectre.Console ANSI/wrapping noise, not a product regression in the CLI help path.
- **2026-04-26 tooling audit shortlist:** The best behavior-preserving cleanup seam is the duplicated settings/output-path pipeline split across `src\Refitter\SettingsValidator.cs` and `src\Refitter\GenerateCommand.cs` (`ValidateFilePath`, `ApplySettingsFileDefaults`, both `GetOutputPath` overloads, and `ResolveRelativeSpecPaths`).
- **2026-04-26 source-generator drift seam:** `src\Refitter.SourceGenerator\Refitter.SourceGenerator.props` auto-includes `**\*.refitter`, but `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs` diagnostic `REFITTER003`, `src\Refitter.SourceGenerator\README.md`, `docs\docfx_project\articles\source-generator.md`, and `README.md` still partly speak as if consumers must wire `AdditionalFiles` manually.
- **2026-04-26 tooling test smell:** `src\Refitter.Tests\GenerateCommandTests.cs`, `src\Refitter.Tests\Examples\SettingsFileOutputPathTests.cs`, and `src\Refitter.Tests\RefitterGenerateTaskTests.cs` lean heavily on reflection into private helpers and duplicated process/workspace harness code, which is a high-confidence cleanup target before larger tooling refactors.
- **2026-04-26 workflow audit note:** `.github\workflows\build.yml`, `msbuild.yml`, `pr.yml`, `smoke-tests.yml`, `regression-tests.yml`, and `production-tests.yml` use `paths-ignore` with negated `!` patterns; GitHub's documented include/exclude model expects `paths` for mixed include/exclude filters, so those trigger rules are a tooling-contract review seam.

## Core Context

- **Breaking-change audit:** The two durable v2 break signals were the source generator's move from disk-written `.g.cs` files to `AddSource()` output and the settings rename from `generateAuthenticationHeader` to `authenticationHeaderStyle` without a compatibility alias.
- **Tooling architecture:** `src\Refitter.MSBuild\RefitterGenerateTask.cs` should trust CLI-emitted `GeneratedFile:` markers, exact include-pattern matches, and real process exit codes; `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs` should keep hint names unique per input and surface user-visible diagnostics instead of relying on `Debug.WriteLine`.
- **Common audit hazards:** shared NSwag model mutation, hard-coded newlines, path resolution tied to CWD, and silent/weak conflict handling were recurring root causes across the tooling/core lanes.
- **Reliable validation patterns:** when the shared NuGet cache is locked, prefer the repo-local cache at `C:\projects\christianhelle\refitter\.nuget\packages`, then validate with targeted tests, packed artifacts, and formatter/build gates.

## 2026-04-25: Remaining Audit Lanes

- Lambert's repro pass narrowed the tooling concerns to **#1029** and **#1041** as partial repros plus the legacy bool-form **#1043** CLI break; **#1042** stayed validation-only and **#1047** stayed fixed-at-HEAD.
- Dallas completed the tooling lane for **#1028**, **#1029**, **#1041**, and **#1043**, then moved onto the rejected core revision for **#1034**/**#1039** after Parker lockout.
- Dallas's core pass fixed the shared-parameter mutation for **#1039** and iterated the multi-spec merge contract for **#1034** from warning-backed cloning to fail-fast collision handling before final proof ownership moved to Lambert and then Ripley.

## 2026-04-25: Linux Help Output Analysis

- Investigated the persistent Ubuntu Actions failure for `Program_Main_Should_Show_Help_When_Invoked_Without_Arguments`.
- Confirmed the CLI/help path is correct: `Program.cs` rewrites no-args to `--help`, and `Settings.cs` metadata is rendered by `Spectre.Console.Cli` without platform-specific product logic.
- Proved the failure was formatter noise in the raw redirected help payload: ANSI sequences and host-dependent wrapping differed across Linux Actions and local Windows captures while preserving the same semantic help content.
- Handed off the recommended fix to Lambert: normalize redirected console output first, then assert semantic help markers in `src\Refitter.Tests\GenerateCommandTests.cs`.
- Lambert's final landed change stayed test-only and validated green with `dotnet build -c Release src\Refitter.slnx`, `dotnet test -c Release src\Refitter.slnx`, and `dotnet format --verify-no-changes src\Refitter.slnx`.

## 2026-04-25: RefitterGenerateTask Coverage Closure

- Closed the remaining `src\Refitter.MSBuild\RefitterGenerateTask.cs` coverage gaps with test-only changes in `src\Refitter.Tests\RefitterGenerateTaskTests.cs`.
- Lambert's gap analysis isolated the exact missing defensive branches first, which let Dallas keep the implementation pass scoped to regression coverage instead of production changes.
- Added direct coverage for the defensive tooling paths: blank package folder, whitespace runtime entries, co-located CLI fallback, first-bundled-path fallback, missing bundled CLI failure, process-runner exception handling, millisecond timeout formatting, and successful `LogErrorFromException` forwarding.
- Validation reported green with `dotnet test --project src\Refitter.Tests\Refitter.Tests.csproj -c Release --coverage --coverage-output coverage.cobertura.xml --coverage-output-format xml`, `dotnet build -c Release src\Refitter.slnx --no-restore`, and `dotnet format --verify-no-changes src\Refitter.slnx --no-restore`.
- The reported end state for `src\Refitter.MSBuild\RefitterGenerateTask.cs` was 100% line coverage, 100% block coverage, and 0 partial functions.

## 2026-04-25: PR #1070 SonarCloud Quality Gate Repair

- Pulled the live SonarCloud PR issue feed for `christianhelle_refitter` / PR `1070` and confirmed five leak-period findings: one reliability bug plus one maintainability finding in `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs`, one maintainability finding in `src\Refitter.Core\ParameterExtractor.cs`, and two maintainability findings in `src\Refitter.MSBuild\RefitterGenerateTask.cs`.
- Collapsed the nested dynamic-query guard in `ParameterExtractor.GetQueryParameters()` to satisfy the clumsy-condition finding without changing the non-mutating grouped-query behavior.
- Simplified `ResolveRefitterDll()` to pre-filter whitespace runtime rows / missing bundled binaries and rewrote `FormatTimeout()` as straight-line conditionals so the MSBuild task no longer trips the Sonar loop and nested-ternary smells.
- Hardened source-generator diagnostics by reusing the shared `Refitter` title constant, assigning distinct stable IDs to the found-file and file-contents diagnostics, and replacing the `GeneratedDiagnostic` record struct with an explicit readonly struct that pairs `Equals(...)` with the custom hash code.
- Validation reported green with `dotnet build -c Release src\Refitter.slnx --no-restore`, `dotnet test -c Release src\Refitter.slnx --no-build`, and `dotnet format --verify-no-changes src\Refitter.slnx --no-restore` (1894 tests passed).
## 2026-04-25: Scribe consolidation of PR #1070

- Ash approved Dallas's S1066, S3267, and S3358 cleanups plus the distinct diagnostic-ID/source-title cleanup as the safe parts of the Sonar response.
- Ash rejected Dallas's first S1206 direction because converting GeneratedDiagnostic away from a readonly record struct was risky semantic churn for an analyzer-only complaint.
- Parker superseded only the source-generator S1206 artifact; the final merged state keeps Dallas's ParameterExtractor and RefitterGenerateTask changes alongside Parker's approved record-struct suppression revision.

## 2026-04-26: Shared AI-slop cleanup context

- Commit `f6374210` (`docs: clarify source generator setup`) is now the landed docs/help cleanup reference point; the associated narrow validation was reported green.
- Ripley's triage keeps docs/help drift first, then settings/spec-path normalization, then shared `GeneratedFile:` marker cleanup, which matches Dallas's current tooling/doc seams.
- Lambert's baseline scan stays green for restore/build/test/format, but live-URL tests remain environment-sensitive and should not be used as tooling stability evidence during cleanup.


## 2026-04-28: e-conomic Multi-Spec Tooling Validation (PRIMARY FIX IN CORE)
- `test\economic.refitter` parses correctly, resolves both relative `openApiPaths` under `test\OpenAPI\v3.0\`, each spec generates independently.
- CLI/MSBuild/source-generator all reach core merge path where duplicate equivalent schemas (`Error`, `ProblemDetails`) trigger fail-fast before validation.
- Root cause confirmed in `Refitter.Core` not tooling; `--skip-validation` cannot help because merge happens before validation.
- Proposal: primary fix belongs in core merge semantics (allow duplicate paths/schemas when semantically equivalent); do not split `.refitter` into multiple generation runs.
- Tooling follow-up (post-core-fix): improve CLI error guidance for merge failures, add `openApiPaths` regression coverage, add relative-path resolution tests, add merge-failure diagnostic tests.

