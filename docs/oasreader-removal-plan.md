# Plan: Removing the OasReader and Microsoft.OpenApi Dependencies

Tracking issue: [#1311](https://github.com/christianhelle/refitter/issues/1311). Builds on the NSwag removal
([nswag-removal-plan.md](nswag-removal-plan.md)), whose document model, YAML loader and `$ref` resolver make
Microsoft.OpenApi redundant for loading documents.

## Goal

Remove the `OasReader` package, and with it `Microsoft.OpenApi` and `Microsoft.OpenApi.YamlReader`, from
`Refitter.Core` and `Refitter.SourceGenerator` without losing a single feature.

## Outcome

- `OasReader`, `Microsoft.OpenApi` and `Microsoft.OpenApi.YamlReader` are no longer dependencies of any project.
  `DependencyGraphTests` fails if they come back.
- `OpenApiValidator` reads documents with its own reader in `Refitter.Core/Validation`. It has one reader for
  Swagger 2.0 and one for OpenAPI 3.0, 3.1 and 3.2, and parses YAML with YamlDotNet. On top of that it reproduces
  Microsoft.OpenApi's rule set, statistics walker, reference resolution and OasReader's multi-file merge.
- `ValidatorParityTests` validates 404 documents both as a local file and over HTTP. It matches the snapshots
  recorded from the Microsoft.OpenApi implementation, including its crashes on malformed values (for example
  `required: yes`) and its quirks (for example rules only running for URL inputs).
- A cycle of `$ref`s between components that are not schemas, such as two parameters referring to each other,
  made Microsoft.OpenApi overflow the stack and crash the process. The new validator treats such a reference as
  unresolved instead.
- YAML syntax errors come from YamlDotNet instead of SharpYaml. Positions are converted to SharpYaml's zero-based
  format, and messages that differ between the two parsers are translated where known (see
  `YamlToJsonConverter`). A YAML syntax error without a known translation keeps YamlDotNet's wording.

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
