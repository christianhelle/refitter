# Dallas History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- **2026-05-05T14:18:05.572+02:00 issue #1088 rebuild failure mode:** the strongest proven source-generator repro is the reporter-style rebuild harness that imports package props, keeps a manual `<AdditionalFiles Include=".refitter" />`, enables `EmitCompilerGeneratedFiles`, and writes compiler output under a project-local `Generated\` folder. First build succeeds with `FormGet` / `FormPost`; rebuild fails when stale emitted `Generated\...\*.g.cs` is compiled again.
- **2026-05-05T14:18:05.572+02:00 issue #1088 source-generator dedupe seam:** the safest tooling fix is a packaged `Refitter.SourceGenerator.targets` target that deduplicates only `.refitter` `AdditionalFiles` before `CoreCompile` and strips `$(CompilerGeneratedFilesOutputPath)\**\*.cs` from `Compile` so stale compiler-generated artifacts cannot shadow fresh source-generator output.
- **2026-05-05T13:02:25Z squad update:** The packaged `Refitter.SourceGenerator.targets` fix now deduplicates `.refitter` `AdditionalFiles` and removes compiler-generated `Generated\...\*.g.cs` files from `Compile` before `CoreCompile`; source-generator tests and format verification passed, and Lambert/Ash are validating the same package/build seam rather than reopening the shared naming path.
- **2026-05-05T14:18:05.572+02:00 issue #1088 validation matrix:** CLI settings-file mode and the source generator both deserialize into `RefitGeneratorSettings` and feed the same core naming pipeline; MSBuild only shells the CLI. Treat `generateMultipleFiles` as adjacent tooling parity, not as the likely root cause of a casing-only mismatch.
- **2026-05-04T15:23:00.022+02:00 issue #1088 tooling fixture split:** keep the legacy Refit 8.x `Net80App` compile helpers for broad coverage, and use the current-lane fixture only for tooling-specific compatibility checks against Refit 10.1.6.
- **2026-05-04T08:59:00.400+02:00 issue #1088 tooling triage:** CLI, source generator, and MSBuild all converge on `Refitter.Core.RefitGenerator`, so method-name casing regressions are shared-core or downstream-Refit compatibility issues unless a tooling-only repro proves otherwise.
- **2026-05-01T14:34:56.630+02:00 issue #1083 tooling follow-up:** dotted-schema-name repair lived in the shared core generator; the only worthwhile adjacent tooling addition was source-generator regression coverage proving compile-time generation matches the CLI path.
- **2026-04-26 source-generator drift seam:** packaged props auto-include `**\*.refitter`, so docs/tests must account for duplicate `AdditionalFiles` risks and keep source-generator diagnostics focused on user-visible failures.
- **Reliable validation pattern:** when the shared NuGet cache is locked, prefer the repo-local cache at `C:\projects\christianhelle\refitter\.nuget\packages`, then validate with targeted tests, packed artifacts, and formatter/build gates.

## Archived Detail

- Older detailed tooling notes were summarized into `history-archive.md` on 2026-05-05T13:02:25Z to keep this file scannable.
