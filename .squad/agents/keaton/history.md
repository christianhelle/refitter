# Keaton — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** C# / .NET (multi-target: 8.0, 9.0, 10.0), NSwag, Spectre.Console, xUnit, FluentAssertions  
**Ships as:** .NET CLI tool (`Refitter`), C# Source Generator (`Refitter.SourceGenerator`), MSBuild task (`Refitter.MSBuild`)  
**Repo root:** `C:/projects/christianhelle/refitter`  
**Solution:** `src/Refitter.slnx`  

- Stable pipeline model: `OpenApiDocumentFactory.CreateAsync` loads specs, `RefitGenerator` filters and orchestrates generation, `CSharpClientGeneratorFactory` bridges settings into NSwag, and one of the three interface generators emits Refit interfaces while NSwag handles contract generation.
- Key entry points: `src/Refitter.Core/RefitInterfaceGenerator.cs`, `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`, `src/Refitter/Program.cs`, and CLI option mapping in `src/Refitter/GenerateCommand.cs`.
- Durable architectural cautions: source generator still blocks on async work and compiles shared Core source directly, MSBuild still shells out to the CLI and historically leaned on regex config parsing, `RefitGeneratorSettings` is a large cross-surface contract object, `GenerateCommand` and `ParameterExtractor` remain oversized, and reflection-based NSwag setting mapping is version-fragile.
- Documentation audit outcome: README/schema drift clusters around newer CLI/settings surfaces, especially `--no-xml-doc-comments`, `--json-serializer-context`, `contractTypeSuffix`, and stale `propertyNameGenerator` schema exposure. Policy remains that new CLI options need README examples and new settings need `.refitter` docs/schema support.
- #944 review rule: when NSwag feeds C#-escaped response descriptions into XML docs, decode the JSON/C# escapes first and then XML-escape the user text.
- #967 product-shape rule: `PropertyNamingPolicy` belongs on top-level `RefitGeneratorSettings`, is safe to expose through CLI plus `.refitter`, should sanitize rather than reject invalid identifiers, and must keep `[JsonPropertyName]` emission intact.
- #967 root-cause rule: the reported stack overflow was a pre-existing recursive-schema traversal bug in `CSharpClientGeneratorFactory`, not a property-naming regression; the approved fix copies the `SchemaCleaner` visited-set pattern.

## Learnings

- Team initialized on 2026-04-16.
