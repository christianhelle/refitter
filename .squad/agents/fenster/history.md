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

### Comprehensive Feature Analysis — 2025-01-XX

Conducted deep source code analysis covering:
- **All 56 CLI options** from `Settings.cs` with types, defaults, descriptions
- **All 70+ .refitter settings** from `RefitGeneratorSettings.cs` including sub-objects (DependencyInjectionSettings, ApizrSettings, CodeGeneratorSettings, NamingSettings)
- **9 enumerations** with all values documented (MultipleInterfaces, OperationNameGeneratorTypes, TypeAccessibility, IntegerType, CollectionFormat, AuthenticationHeaderStyle, CacheProviderType, MappingProviderType, TransientErrorHandler)
- **README.md analysis** - identified 4 undocumented CLI flags and 2 inaccuracies in documentation
- **Feature gaps** - 10 major features available ONLY via .refitter file (no CLI exposure): multiple OpenAPI merge, naming settings, status code comments, content-type headers toggle, response type override, optional parameters ordering, full DI config, most Apizr config, most NSwag code generator settings, contract type suffix

**Key Findings:**
1. CLI has 56 options but .refitter file has 70+ settings - significant feature gap
2. `--json-serializer-context`, `--security-scheme` implemented but not in README
3. Format-mappings feature exists (schema files in `docs/`) but undocumented
4. Code quality issues: 2 copy-paste errors in XML docs, 1 logic bug in `GenerateCommand.cs` line 314
5. Deprecated features (`usePolly`, `pollyMaxRetryCount`) still shown in README without deprecation notice

**Output:** Complete catalog written to `.squad/temp-fenster-feature-catalog.md` - 700+ lines covering every setting, enum value, and documentation gap.

### Documentation Updates — 2025-01-XX

Applied 9 specific changes to `README.md` and `docs/json-schema.json` based on feature audit:

**README.md changes:**
1. Added `--no-xml-doc-comments` CLI option to OPTIONS section
2. Added `--ignored-operation-headers` CLI option to OPTIONS section
3. Added `--json-serializer-context` CLI option to OPTIONS section
4. Added `generateJsonSerializerContext` to .refitter JSON example
5. Added `returnIObservable` to .refitter JSON example
6. Added `collectionFormat` and `contractTypeSuffix` to .refitter JSON example
7. Added 7 missing entries to .refitter settings description list: `addContentTypeHeaders`, `returnIObservable`, `generateJsonSerializerContext`, `generateDisposableClients`, `usePolymorphicSerialization`, `collectionFormat`, `contractTypeSuffix`

**json-schema.json changes:**
1. Added `generateJsonSerializerContext` property definition
2. Added `contractTypeSuffix` and `collectionFormat` property definitions with proper JSON Schema validation (enum for collectionFormat, nullable string for contractTypeSuffix)

All changes validated:
- README .refitter JSON example remains valid JSON
- json-schema.json validated with PowerShell ConvertFrom-Json
- All whitespace/indentation preserved matching existing style

### Code Coverage Improvements — 2025

Applied `[ExcludeFromCodeCoverage]` to genuinely untestable code following existing patterns in the codebase:

**DependencyInjectionSettings.cs:**
- Excluded obsolete `UsePolly` property (backward compat wrapper for `TransientErrorHandler`)
- Excluded obsolete `PollyMaxRetryCount` property (backward compat wrapper for `MaxRetryCount`)

These properties are pass-through wrappers marked `[Obsolete]` that exist purely for backward compatibility. They contain no testable logic and cannot be meaningfully covered in isolation from the properties they wrap. Exclusion improves the coverage denominator while preserving accurate metrics.

### Issue #944 — Unicode XML documentation sanitization — 2026-03-06

- `src\Refitter.Core\XmlDocumentationGenerator.cs` is the shared production sink for OpenAPI response descriptions: normal `<returns>` docs use `method.ResultDescription`, while status-code tables render `response.ExceptionDescription` through `BuildResponseDescription()`.
- Response descriptions can reach the generator in JSON-escaped form (`\uXXXX`, `\"`, `\n`) even when the original OpenAPI document contains readable Unicode, so generator code should decode JSON-style escapes before emitting XML comments.
- Only sanitize user-sourced response description text; keep hardcoded XML doc fragments like `<see cref="Task"/>` and `<list>` markup untouched, then XML-escape reserved characters (`&`, `<`, `>`) at the point where response text is inserted.
- Focused Refitter tests are fastest through `src\Refitter.Tests\bin\Release\net10.0\Refitter.Tests.exe` with `--treenode-filter`, since `dotnet test --filter ...` is not supported by this TUnit/Microsoft.Testing.Platform setup.

### Issue #944 Implementation — 2026-03-06

Successfully implemented fix for non-ASCII characters in XML status-code comments:
- Added `DecodeJsonEscapedText()` method to decode `\uXXXX` escapes before XML-escaping in response descriptions
- Tests confirmed: Cyrillic Unicode renders correctly, escape sequences absent, compilation succeeds
- All 1415 tests pass with no regressions

