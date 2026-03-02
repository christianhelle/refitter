# Fenster — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** C# / .NET (multi-target: 8.0, 9.0, 10.0), NSwag, Spectre.Console  
**Repo root:** `C:/projects/christianhelle/refitter`  
**Solution:** `src/Refitter.slnx`  

My domain: `src/Refitter.Core/`, `src/Refitter/`, `src/Refitter.SourceGenerator/`, `src/Refitter.MSBuild/`.

Adding a CLI option follows this pattern:
1. `src/Refitter/Settings.cs` → add `[CommandOption("--option-name")]` property
2. `src/Refitter/GenerateCommand.cs` → map in `CreateRefitGeneratorSettings()`
3. `src/Refitter.Core/Settings/RefitGeneratorSettings.cs` → add core property
4. Implement logic in generator classes
5. Update `README.md`

Run CLI: `dotnet run --project src/Refitter --configuration Release --framework net9.0 -- [args]`  
Format REQUIRED before commit: `dotnet format src/Refitter.slnx`

## Learnings

_Session learnings will be appended here._
