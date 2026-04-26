# Ripley History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.

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

