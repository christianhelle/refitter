# Hockney — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** C# / .NET, xUnit, FluentAssertions  
**Repo root:** `C:/projects/christianhelle/refitter`  
**Solution:** `src/Refitter.slnx`  

My domain: `src/Refitter.Tests/`, `src/Refitter.SourceGenerator.Tests/`.

Test pattern: class in `Refitter.Tests.Examples`, const OpenAPI spec string, async `[Fact]` methods using `GenerateCode()`, assertions via FluentAssertions, `BuildHelper.BuildCSharp(code).Should().BeTrue()` to verify generated code compiles.

Test resources (OpenAPI specs): `src/Refitter.Tests/Resources/V2/` and `V3/`. Use `SwaggerPetstore.json` for general testing.

Run tests: `dotnet test -c Release src/Refitter.slnx` (~5 min — NEVER cancel). Network test failures in sandboxed environments are acceptable.

## Learnings

_Session learnings will be appended here._
