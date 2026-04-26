# Fenster — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** C# / .NET (multi-target: 8.0, 9.0, 10.0), NSwag, Spectre.Console  
**Repo root:** `C:/projects/christianhelle/refitter`  
**Solution:** `src/Refitter.slnx`  

- Domain focus: `src/Refitter.Core/`, `src/Refitter/`, `src/Refitter.SourceGenerator/`, and `src/Refitter.MSBuild/`.
- Adding a CLI option still follows the stable five-step path: `Settings.cs` option, `GenerateCommand.cs` mapping, `RefitGeneratorSettings.cs` core setting, generator implementation, and `README.md` docs.
- Durable architecture issues from earlier audits: CLI and `.refitter` settings are not 1:1, `ParameterExtractor` mutates contract-sensitive NSwag state, `RefitGenerator` setup and interface emission have duplicate logic, single-file generation uses asymmetric metadata/content records, and docs/schema surfaces can drift behind the implementation.
- Documentation/feature audit takeaways: the repo exposes materially more `.refitter` settings than CLI options, README/schema drift tends to cluster around newer flags (`--json-serializer-context`, contract type suffix, format mappings, deprecated Polly settings), and new options/settings should always land with README plus schema updates.
- #944 rule: decode JSON-style escape sequences in user-sourced response descriptions before XML-escaping, while leaving hardcoded XML doc fragments untouched.
- #967 property-naming outcome: `PropertyNamingPolicy` is the user-facing setting, it is safe to expose through CLI + `.refitter` + schema, `PreserveOriginalPropertyNameGenerator` sanitizes/escapes/deduplicates identifiers, and `[JsonPropertyName]` must stay emitted for binding correctness.
- #967 stack-overflow outcome: the real bug was duplicate recursive schema preprocessing without a visited-set in `CSharpClientGeneratorFactory`; the fix was a shared iterative visitor (`Stack<JsonSchema>` + `HashSet<JsonSchema>`) and was not caused by the property-naming feature.

## Learnings

- Team initialized on 2026-04-16.
