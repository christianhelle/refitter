---
name: "multi-source-openapi-tooling"
description: "Diagnose Refitter tooling behavior for .refitter files with openApiPaths"
domain: "tooling"
confidence: "high"
source: "earned"
last_updated: "2026-04-28T15:21:48.369+02:00"
---

## Context

Use this when a `.refitter` file contains `openApiPaths` and a failure might come from settings parsing, path resolution, CLI/source-generator/MSBuild behavior, or core multi-document merge.

## Pattern

- First prove the settings file is accepted: `SettingsValidator` supports `openApiPaths`, rejects only simultaneous `openApiPath` + `openApiPaths`, applies settings-file defaults, and resolves relative entries against the `.refitter` file directory.
- Generate each OpenAPI spec independently through a scratch `.refitter` file before blaming path resolution; successful single-spec generation proves loading is fine.
- Run the original multi-spec `.refitter` with CLI `--simple-output` to see whether failure occurs before `GeneratedFile:` markers.
- Remember `--skip-validation` skips `OpenApiValidator` only; it does not bypass `RefitGenerator.CreateAsync(...)` document loading or merge.
- Source generator and MSBuild are downstream tooling surfaces: source generator calls `RefitGenerator.CreateAsync(settings)` directly, while MSBuild shells out to CLI and trusts `GeneratedFile:` markers.
- For duplicate schema failures, distinguish real conflicts from equivalent common schemas shared across specs. The core merge policy should fail fast for different shapes but allow semantically equivalent duplicates without requiring edits to vendor OpenAPI files.

## Anti-patterns

- Treating a core merge exception as a `.refitter` parse failure after settings validation has already reached generation.
- Expecting source-generator `outputFolder` to affect physical layout; source generator emits Roslyn hint names instead of disk files.
- Suggesting `--skip-validation` for document load/merge exceptions.
