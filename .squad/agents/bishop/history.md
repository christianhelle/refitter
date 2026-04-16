# Bishop History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Reviewed documentation vs. implementation. CLI bug in `GenerateCommand.GetOutputPath()` ignores `outputFolder` when matching default value. MSBuild task correctly predicts expected paths, but CLI doesn't honor them. Documented behavior matches intent; implementation is wrong.
