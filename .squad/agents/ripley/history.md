# Ripley History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- 2026-04-28T12:02:17.298+02:00: Issue #1045's new multi-path `.refitter` comment does not reproduce at current HEAD; current CLI validation accepts `openApiPaths`, normalizes relative entries, and the live repro instead surfaced expected merge-collision behavior only when duplicate path specs were used.
- 2026-04-28T15:21:48.369+02:00: The e-conomic multi-spec failure is a core `OpenApiDocumentFactory` merge-equivalence problem: `economic-products.json` and `economic-webhooks.json` share equivalent `Error` and `ProblemDetails` component schemas, but current NSwag-object serialization comparison false-negatives and throws before validation/generation.

## Core Context

- The durable v2 breaking changes are the settings rename from `generateAuthenticationHeader` to `authenticationHeaderStyle` and the source-generator move from disk-written files to Roslyn `context.AddSource()` output.
- Key compatibility surfaces stay the same across audits: `.refitter` JSON schema, CLI option names, generated code shape, source-generator behavior, MSBuild output prediction/marker handling, and OasReader parsing behavior after the v1.x → v3.x jump.
- Reliable packaging guidance keeps `OasReader` as `PrivateAssets="all"` and `Refit` as `PrivateAssets="compile"` for the source-generator package, with validation grounded in packed artifacts and `SourceGeneratorPackageReferenceTests`.
- Common audit patterns: settings validation/normalization drift, regex-heavy parsing in tooling, generated-file marker duplication, keyword escaping gaps, and fragile generator ordering should be treated as contract-sensitive cleanup areas.
- Final #1057 PR-assembly guidance keeps **#1028, #1029, #1033, #1034, #1039, #1041, and #1043** as safe auto-close candidates while **#1032, #1042, #1045, #1047, and #1056** remain validation-only, fixed-at-HEAD, or doc/invariant-only.
- Cleanup sequencing for the current AI-slop lane is: docs/help drift first, settings/spec-path normalization second, shared `GeneratedFile:` contract third, test-surface cleanup fourth, and core generator dedup last.

## 2026-04-26: Shared cleanup kickoff

- Lambert's latest baseline remains green for restore/build/test/format, but live-URL tests stay environment-sensitive and should not be used as cleanup stability signals.
- Dallas landed commit `f6374210` (`docs: clarify source generator setup`) with narrow validation green, which keeps docs/help drift as the safest first batch.
- Current squad capacity is sufficient for docs, tooling, test-surface, and settings cleanup; only the later core generator / `ParameterExtractor` dedup lane needs a specialized reviewer.


## 2026-04-26: Post-cleanup reassessment
- First-wave landings cleared several earlier high-signal seams: XML-doc split-line reuse, source-generator setup/docs drift, suffix rewriter dedup, source-generator file-read hardening, by-endpoint query-parameter naming, title angle-bracket sanitization, and bounded MSBuild runtime discovery.
- The best remaining behavior-safe seam is still split settings/spec-path normalization across `src\Refitter\SettingsValidator.cs`, `src\Refitter\GenerateCommand.cs`, and `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs`; those paths still disagree on URL detection and only some flows normalize `OpenApiPaths`.
- The next best tooling seam is the duplicated `GeneratedFile:` marker contract between `src\Refitter\GenerateCommand.cs` and `src\Refitter.MSBuild\RefitterGenerateTask.cs`.
- After those two, the backlog drops into lower-priority cleanup/refactor territory such as source-generator info-diagnostic shaping, duplicate test pruning, dead output-model cleanup, and deeper core generator dedup.

## 2026-04-28: e-conomic Multi-Spec Merge Failure (APPROVED FOR FIX)
- Squad triaged the `test\economic.refitter` failure to `OpenApiDocumentFactory.Merge()` throwing on duplicate equivalent schemas.
- `economic-products.json` and `economic-webhooks.json` share identical `Error` and `ProblemDetails` component schemas; current `AreEquivalent()` calls `Serializer.Serialize()` on NJsonSchema objects and false-negatives on object cycles.
- Fix approved: replace `AreEquivalent()` with OpenAPI-aware semantic comparison using NSwag canonical JSON representation; keep fail-fast for genuine conflicts.
- Gates: merge equivalence tests, e-conomic-shaped regressions, compile-backed validation; existing collision tests must pass.
- Constraints: preserve relative-path behavior, don't edit specs, don't rename schemas in merge.
