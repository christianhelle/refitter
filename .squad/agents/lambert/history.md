# Lambert History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Reproduced on clean .NET 10 build. Output.cs written to project root instead of Generated folder. Settings honored, but file path logic broken. Non-default folders work. First build fails due to sync mismatch; second build succeeds. Specific to default single-file output path behavior.
