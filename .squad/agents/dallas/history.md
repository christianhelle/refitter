# Dallas History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Validated MSBuild tooling path. CLI loads settings correctly (naming honored). Single-file output without explicit `outputFilename` falls back to `Output.cs` and skips `./Generated` folder when it matches default. Real product bug: MSBuild expects wrong file locations on first clean build.
