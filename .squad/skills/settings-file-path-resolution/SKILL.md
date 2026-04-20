---
name: "settings-file-path-resolution"
description: "Keep Refitter tooling consistent when resolving .refitter-relative OpenAPI paths and output locations."
domain: "tooling"
confidence: "high"
source: "earned"
---

## Context
Use this when changing Refitter CLI, source generator, or MSBuild behavior that reads `.refitter` files. The main risk is letting one tooling surface resolve paths or output folders differently than another.

## Patterns
- Resolve every non-URL, non-rooted `openApiPath` and every `openApiPaths[]` entry relative to the `.refitter` file directory before validation or generation.
- Centralize CLI settings-file path resolution in a shared helper so validation and execution do not drift.
- For multi-file generation, treat explicit CLI `--output` as the highest-precedence destination, including `Contracts.cs`.
- For source-generator hint names, hash the full `.refitter` path, not just the directory, when `outputFilename` can collide.

## Examples
- Shared CLI resolver: `src\Refitter\SettingsFilePathResolver.cs`
- CLI application points: `src\Refitter\SettingsValidator.cs`, `src\Refitter\GenerateCommand.cs`
- Source generator application point: `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs`

## Anti-Patterns
- Do not validate relative OpenAPI paths against the process working directory when a `.refitter` file is present.
- Do not let settings-file defaults override an explicit CLI output directory in multi-file mode.
- Do not hash only the directory path for source-generator hint names when multiple `.refitter` files can share the same `outputFilename`.
