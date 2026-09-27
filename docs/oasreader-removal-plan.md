# Plan: Removing the OasReader and Microsoft.OpenApi Dependencies

Tracking issue: [#1311](https://github.com/christianhelle/refitter/issues/1311). Builds on the NSwag removal
([nswag-removal-plan.md](nswag-removal-plan.md)), whose document model, YAML loader and `$ref` resolver make
Microsoft.OpenApi redundant for loading documents.

## Goal

Remove the `OasReader` package, and with it `Microsoft.OpenApi` and `Microsoft.OpenApi.YamlReader`, from
`Refitter.Core` and `Refitter.SourceGenerator` without losing a single feature.

## Where Microsoft.OpenApi was used

| Area | What it did | Replacement |
|---|---|---|
| `OpenApiReaderDocumentStrategy` | Last-resort document loading strategy. It re-serialized multi-file documents without inlining external `$ref`s, which the primary loader resolves anyway | Deleted |
| `OpenApiValidator` | Reader diagnostics, the default validation rule set (URL inputs only), statistics | Refitter's own validator |
| `AttributeStringValidator` | Rejects paths, header names and content types that could break out of Refit attribute strings | Ported to Refitter's validator |
| Public API | `OpenApiDiagnostic`, `OpenApiError`, `OpenApiSpecVersion`, `OpenApiUnsupportedSpecVersionException` and `OpenApiVisitorBase` exposed on Refitter's public surface | Refitter-owned types, see below |

## Decisions

- **Parity bar.** The same specs pass or fail, with the same diagnostics (pointer and message) and statistics.
  `ValidatorParityTests` validates every spec in `test/OpenAPI`, the test resources, the embedded scenario specs
  and the deliberately broken specs in `src/Refitter.Tests/Parity/ValidatorFixtures`, both as a local file and
  served over HTTP. It compares the results to snapshots recorded from the Microsoft.OpenApi implementation
  before it was replaced.
- **Rule set.** Microsoft.OpenApi's default validation rules only ran for URL inputs, because OasReader's
  multi-file reader passes an empty rule set for local files. This behavior is kept as is.
- **Public API.** The Microsoft.OpenApi types are replaced with Refitter-named types. `PublicApiNSwagIndependenceTests`
  guards the public API against exposing Microsoft.OpenApi types again.

## Breaking changes for Refitter.Core library consumers

The CLI, MSBuild task and source generator are not affected. Code that uses `Refitter.Core.Validation` directly
needs these changes:

| Before | After |
|---|---|
| `OpenApiValidationResult.Diagnostics` is `Microsoft.OpenApi.Reader.OpenApiDiagnostic` | `Refitter.Core.Validation.ValidationDiagnostics` |
| `Microsoft.OpenApi.OpenApiError` (`Pointer`, `Message`) | `Refitter.Core.Validation.ValidationIssue` (`Pointer`, `Message`) with the same `ToString()` format |
| `OpenApiDiagnostic.SpecificationVersion` is `Microsoft.OpenApi.OpenApiSpecVersion` | `Refitter.Core.Validation.OpenApiSpecificationVersion`, with the same member names |
| `OpenApiValidator.Validate` throws `Microsoft.OpenApi.OpenApiUnsupportedSpecVersionException` | Throws `Refitter.Core.Validation.UnsupportedSpecificationVersionException`, with the same message and `SpecificationVersion` |
| `OpenApiStats` derives from `Microsoft.OpenApi.OpenApiVisitorBase` and exposes `Visit(...)` overrides | `OpenApiStats` is a plain class with the same count properties and `ToString()` output |
