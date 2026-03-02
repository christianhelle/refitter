# Keaton — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** C# / .NET (multi-target: 8.0, 9.0, 10.0), NSwag, Spectre.Console, xUnit, FluentAssertions  
**Ships as:** .NET CLI tool (`Refitter`), C# Source Generator (`Refitter.SourceGenerator`), MSBuild task (`Refitter.MSBuild`)  
**Repo root:** `C:/projects/christianhelle/refitter`  
**Solution:** `src/Refitter.slnx`  

Key paths: Core generation logic in `src/Refitter.Core/RefitInterfaceGenerator.cs`. Settings model at `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`. CLI entry in `src/Refitter/Program.cs`. CLI options in `src/Refitter/Settings.cs` with `[CommandOption]` attributes mapped in `GenerateCommand.cs`.

Build: `dotnet build -c Release src/Refitter.slnx` (~22s). Tests: `dotnet test -c Release src/Refitter.slnx` (~5min). Format: `dotnet format src/Refitter.slnx`. CI runs on Windows.

## Learnings

_Session learnings will be appended here._
