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

### Code Review — 2025 (Core + CLI)

#### Architecture
- **RefitGenerator.cs** is the top-level orchestrator. It creates `CSharpClientGeneratorFactory`, runs NSwag to get contracts, then delegates to one of three interface generators based on `MultipleInterfaces` enum.
- Interface generator hierarchy: `RefitInterfaceGenerator` (base, single interface) → `RefitMultipleInterfaceGenerator` (ByEndpoint) and `RefitMultipleInterfaceByTagGenerator` (ByTag) both extend it.
- `ParameterExtractor` is a pure static class — handles all 6 parameter kinds: Route, Query, Body, Header, FormData, BinaryBody.
- `CSharpClientGeneratorFactory` wraps NSwag, applies custom integer type, fixes missing format fields, and injects a `CustomTemplateFactory` to patch the `JsonPolymorphic` attribute (workaround for NSwag limitations).
- `OpenApiDocumentFactory` has a layered strategy: tries `OpenApiMultiFileReader` (Microsoft.OpenApi) first for external $ref resolution, falls back to NSwag on failure.
- `SchemaCleaner` performs tree-shaking on OpenAPI components — used only when `TrimUnusedSchema = true`.
- `DependencyInjectionGenerator` and `ApizrRegistrationGenerator` are output-only static classes that produce the DI extension method code.

#### Settings Architecture
- `RefitGeneratorSettings` is the core model (netstandard2.0, ~40 properties).
- Sub-models: `CodeGeneratorSettings` (NSwag passthrough), `DependencyInjectionSettings`, `ApizrSettings`, `NamingSettings`.
- `Settings.cs` in CLI is the Spectre.Console `CommandSettings` — NOT 1:1 with `RefitGeneratorSettings`. Some core settings are only accessible via `.refitter` file.

#### Confirmed Code Issues
- `IncludeTags` in `RefitGeneratorSettings` has a copy-pasted `[Description("Generate a Refit interface for each endpoint.")]` — wrong description.
- `ResponseTypeOverride` description starts with "AddAcceptHeaders dictionary..." — copy-paste artifact.
- `defaultNamespases` typo in `RefitInterfaceImports.cs` (should be `defaultNamespaces`).
- `RefitMultipleInterfaceGenerator.GetInterfaceName` uses `.Replace("I", string.Empty)` on the full interface name — replaces ALL capital-I characters, not just the leading prefix.
- `ParameterExtractor.GetQueryParameters` mutates `operationModel.Parameters` (a NSwag-owned collection) via `.Remove()` during dynamic querystring parameter generation — fragile side effect.
- `GenerateCommand.cs` line 314: `ContractsOutputFolder = settings.ContractsOutputPath ?? settings.OutputPath` sets ContractsOutputFolder to the default "Output.cs" when ContractsOutputPath is null — a file path used where a folder path is expected.
- `RefitGenerator.Generate()` and `RefitGenerator.GenerateMultipleFiles()` share ~20 identical setup lines (factory, generator, docGenerator, contracts, sanitize) — DRY violation.
- `CSharpClientGeneratorFactory` has two near-identical recursive schema traversal methods: `FixMissingTypesWithIntegerFormat` and `ApplyCustomIntegerType` — should share a generic schema visitor.
- `RefitGenerator.GenerateClient` single-file path returns an array where only the first item has content, remaining items have empty strings — subtle and confusing to future maintainers.

#### CLI Option Gaps
- `GenerateAuthenticationHeader` — present in `RefitGeneratorSettings`, NO CLI option in `Settings.cs`.
- `AddContentTypeHeaders` — `AddAcceptHeaders` has `--no-accept-headers` but `AddContentTypeHeaders` has NO CLI option.
- `DependencyInjectionSettings` — the entire DI configuration is only accessible via `.refitter` settings file, not CLI.
- `ResponseTypeOverride` — a dictionary, so understandable it's file-only, but undocumented as such.

#### Naming / Documentation Issues
- `RefitInterfaceImports.defaultNamespases` — typo persists (has been there since initial code).
- XML doc on `XmlDocumentationGenerator` constructor is `internal` but the class itself is `public` — minor inconsistency.
- `IdentifierUtils.Sanitize` comment says "@ can be used and still make valid methode names" — typo ("methode").
