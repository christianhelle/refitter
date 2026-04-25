# Dallas History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** MSBuild's first-clean-build path was the real tooling bug; CLI settings loading and the default single-file `Output.cs` behavior were otherwise correct.
- **PR #1067 tooling proof (2026-04-21):** The most reliable tooling evidence comes from validating real stdout marker contracts and packed artifacts (`.nupkg`/`.nuspec`), not just project metadata or happy-path check runs.
- **2026-04-25 Linux help-output analysis:** The Ubuntu help failure was a test portability problem caused by raw Spectre.Console ANSI/wrapping noise, not a product regression in the CLI help path.

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
