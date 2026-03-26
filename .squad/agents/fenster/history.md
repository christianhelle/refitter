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

## Architecture & Code Issues (Archived from 2025)

Core generator pattern: `RefitGenerator` orchestrates `CSharpClientGeneratorFactory` → NSwag contracts → one of three interface generators (single, ByEndpoint, ByTag). Settings architecture: `RefitGeneratorSettings` (netstandard2.0) with sub-models for DI/Apizr/Naming; CLI `Settings.cs` is not 1:1 (some settings file-only). Confirmed issues: wrong `[Description]` attributes on `IncludeTags`/`ResponseTypeOverride`, `defaultNamespases` typo, `.Replace("I")` replaces all capital-I not just prefix, `ParameterExtractor` mutates NSwag collections, `ContractsOutputFolder` can be set to file path, DRY violation in `RefitGenerator` setup (~20 lines duplicated), `CSharpClientGeneratorFactory` has two identical recursive schema traversals (fixed in #967), `GenerateClient` single-file path returns array where only first item has content.

---

## Issue #967 — Property Naming Implementation (2026-03-25)

**Status:** ✅ DELIVERED & APPROVED

Implemented property naming policy feature across product surfaces:
- Created `PropertyNamingPolicy` enum (`PascalCase`, `PreserveOriginal`) on `RefitGeneratorSettings`
- Built `PreserveOriginalPropertyNameGenerator` with edge-case handling (reserved keywords, invalid identifiers)
- Exposed via CLI (`--property-naming-policy`), `.refitter` file, and updated schema
- All validation gates pass: build, 1468 tests, format

**Collaborators:**
- Hockney provided comprehensive regression coverage
- Keaton reviewed and approved for merge

**Known Follow-up:** IdentifierUtils contains three public methods with zero external callers (Keaton flagged as non-blocking; consolidate or remove in next PR).

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

### Issue #967 Investigation — Snake_case Property Names — 2026-03-06

**Feasibility: TECHNICALLY FEASIBLE, requires product surface changes**

Investigated whether Refitter can generate snake_case C# property names instead of mandatory PascalCase conversion.

**Key Findings:**

1. **Current Access Paths:**
   - NOT accessible via CLI (no `--property-name-generator` option)
   - NOT accessible via `.refitter` files (`CodeGeneratorSettings.PropertyNameGenerator` marked `[JsonIgnore]`)
   - NOT accessible via source generator
   - Only accessible programmatically by injecting custom `IPropertyNameGenerator` into core settings

2. **Root Cause:**
   - `CustomCSharpPropertyNameGenerator` is hardcoded in `CSharpClientGeneratorFactory.Create()` line 33
   - Performs mandatory `ConvertToUpperCamelCase()` + `ConvertSnakeCaseToPascalCase()` (line 46)
   - Falls back to this generator when user doesn't provide custom one
   - `CodeGeneratorSettings.PropertyNameGenerator` explicitly marked `[JsonIgnore]` to prevent `.refitter` deserialization
   - Historical #516 added support for custom generators but never exposed to end users

3. **Edge Cases Requiring Handling:**
   - Invalid C# identifiers (spaces → `_`, hyphens → `_`, leading digits → prepend `_`)
   - C# reserved keywords (detect & add `@` prefix or `_` suffix)
   - **Critical:** `[JsonPropertyName]` attribute MUST still be generated for deserialization to work; user's example omitted it, would require `PropertyNameCaseInsensitive = true` to avoid binding failures
   - Potential name collisions when sanitization produces duplicates

4. **Schema/Serialization Complexity:**
   - `IPropertyNameGenerator` is an external NJsonSchema interface, difficult to expose via JSON without type discriminator
   - Creating polymorphic deserialization for `.refitter` files would require either whitelist strategy or reflection-based instantiation
   - Recommenda­tion: CLI-only implementation to avoid schema complexity

5. **Product Surfaces Requiring Changes (if approved):**
   - Add `--use-property-name-as-is` CLI option to `Settings.cs`
   - Map option in `GenerateCommand.cs` to inject `PreservingPropertyNameGenerator`
   - Create `PreservingPropertyNameGenerator` in core (sanitizes invalid identifiers & reserved keywords)
   - Update README.md with new option and behavior description

**Full detailed feasibility report written to `.squad/decisions/inbox/fenster-issue-967-investigation.md`**


## 2026-03-25: Issue #967 Team Assessment

Team consensus reached on GitHub issue #967 (Preserve Original Property Names):
- ✅ APPROVED FOR IMPLEMENTATION
- Recommended surface: CLI option --property-naming-policy with enum values
- Implementation surfaces identified: CLI, settings mapper, new generator class
- Edge-case handling: reserved keywords, hyphens, leading digits, collisions
- No .refitter file support in Phase 1 (defer polymorphic deserialization)

Consolidated decision entry created in decisions.md. See orchestration logs for full team assessment.

### Issue #967 Implementation Planning — 2026-03-25

Completed comprehensive implementation plan (`.squad/implementation-plan-issue-967.md`) covering:

**Recommended Implementation Shape:**
- CLI surface: `--property-naming-policy [PascalCase|Preserve|CamelCase|SnakeCase]`
- Architecture: Pluggable `IPropertyNameGenerator` hierarchy; factory-based routing
- Scope: Phase 1 = CLI only; Phase 2 = .refitter/.refitter files (defer polymorphic JSON deserialization)
- Default: PascalCase (backward compatible)

**File Changes (13 tasks, ~5 hours estimated):**

**New Files (5):**
1. `PropertyNamingPolicy.cs` — Enum (PascalCase, Preserve, CamelCase, SnakeCase)
2. `IdentifierUtils.cs` — Shared validation: `IsValidIdentifier()`, `SanitizeInvalidIdentifier()`, `EscapeReservedKeyword()`
3. `PreservingPropertyNameGenerator.cs` — Generator: use exact OpenAPI names, sanitize invalid identifiers
4. `CamelCasePropertyNameGenerator.cs` — Generator (optional MVP)
5. `PropertyNamingPolicyTests.cs` — 10+ test cases per SKILL validation checklist

**Modified Files (6):**
1. `src/Refitter/Settings.cs` — Add `[CommandOption("--property-naming-policy")]`
2. `src/Refitter/GenerateCommand.cs` — Map setting in `CreateRefitGeneratorSettings()`
3. `src/Refitter.Core/Settings/RefitGeneratorSettings.cs` — Add property + serialization
4. `src/Refitter.Core/CSharpClientGeneratorFactory.cs` — Update line 33 generator routing logic
5. `README.md` — Document new option with examples
6. `docs/json-schema.json` — Add `propertyNamingPolicy` definition

**Work Breakdown (Ordered):**

*Phase 1: Core Infrastructure* (1 hour)
- Task 1.1: PropertyNamingPolicy enum (5 min)
- Task 1.2: IdentifierUtils sanitization (30 min)
- Task 1.3: PreservingPropertyNameGenerator (15 min)
- Task 1.4: CamelCasePropertyNameGenerator (15 min, optional)

*Phase 2: Settings & Routing* (45 min)
- Task 2.1: RefitGeneratorSettings property (10 min)
- Task 2.2: CSharpClientGeneratorFactory routing (15 min)
- Task 2.3: Settings.cs CLI option (5 min)
- Task 2.4: GenerateCommand mapper (5 min)

*Phase 3: Testing* (2.5 hours)
- Task 3.1: PropertyNamingPolicyTests (10+ tests, 2 hours)
- Task 3.2: Regression validation (15 min)

*Phase 4: Documentation* (1 hour)
- Task 4.1: README updates (30 min)
- Task 4.2: json-schema.json (15 min)
- Task 4.3: Format validation (5 min)

**Critical Edge Cases & Mitigations:**

| Case | Risk | Mitigation |
|------|------|-----------|
| Hyphens (`pay-method`) | Invalid C# | Reject in Preserve mode + clear error |
| Reserved keywords (`class`) | Compilation error | Auto-escape → `@class` |
| Leading digits (`123abc`) | Invalid C# | Prepend underscore → `_123abc` |
| Collisions (`Class`, `_class`) | Silent errors | Detect + warn; use sequential suffix |
| No `[JsonPropertyName]` | Deserialization fails | Always emit regardless of mode |

**Test Strategy:**
- 5+ core unit tests: basic, PascalCase regression, invalid handling, keyword escaping, compilation
- Integration: CLI help, generation, serialization roundtrip
- Regression: Full 1415+ test suite must pass

**Success Criteria:**
- ✅ CLI option functional and documented
- ✅ Preserve mode generates exact property names (snake_case example)
- ✅ All edge cases handled safely
- ✅ 10+ unit tests pass; 0 regressions
- ✅ Code compiles; serialization verified
- ✅ Format validation passes

**Phase 2 (Deferred):**
- `.refitter` file support: Add `propertyNamingPolicy` to schema; implement enum-based factory in SourceGenerator/MSBuild
- Complexity: JSON polymorphic deserialization deferred; enum routing sufficient

**Key Design Decisions Ratified:**
1. CLI-only Phase 1 (simpler than polymorphic .refitter deserialization)
2. Enum-based routing (cleaner than interface discriminator pattern)
3. Reject invalid identifiers in Preserve mode (safer UX)
4. Use `@` prefix for reserved keywords (standard C# pattern)
5. Default to PascalCase (backward compatible)
6. Always emit `[JsonPropertyName]` (ensures correct binding)

**Documentation Plan:**
- README: Add "Property Naming Policies" section with use cases for each enum value
- Examples: PascalCase vs Preserve side-by-side output
- Schema: Note that `[JsonPropertyName]` is auto-generated for binding

**Implementation Ready:** ✅ Plan complete, approved by team, ready for Phase 1 execution.

### Issue #967 Implementation — 2026-03-25

- Added a top-level `PropertyNamingPolicy` enum to `RefitGeneratorSettings` and wired it through the CLI, `.refitter` JSON surface, Source Generator, and MSBuild/shared serializer flow with `PascalCase` as the default.
- `CSharpClientGeneratorFactory` now resolves property name generation by precedence: programmatic `CodeGeneratorSettings.PropertyNameGenerator` first, then the user-facing `PropertyNamingPolicy`.
- `PreserveOriginalPropertyNameGenerator` preserves valid identifiers, escapes reserved C# keywords with `@`, minimally sanitizes invalid shapes with underscores, and de-duplicates sibling collisions with `IdentifierUtils.Counted` against `ParentSchema.Properties`.
- Removed the dead `propertyNameGenerator` JSON schema surface and documented the replacement setting in `README.md`.
- Validation that succeeded on this slice: CLI `--help` shows `--property-naming-policy`; direct CLI generation and `.refitter` generation both preserved raw property names and kept `[JsonPropertyName]`; full repo gate passed with `dotnet build -c Release src\Refitter.slnx`, `dotnet test -c Release --solution src\Refitter.slnx`, and `dotnet format --verify-no-changes src\Refitter.slnx`.

### Issue #967 follow-up investigation — stack overflow in recursive schemas — 2026-03-26

- `CSharpClientGeneratorFactory.ProcessSchemaForMissingTypes()` (`src\Refitter.Core\CSharpClientGeneratorFactory.cs:179-211`) recursively walks `Properties`, `Item`, `AdditionalPropertiesSchema`, and `AllOf`/`OneOf`/`AnyOf` with no visited-set, so a normal self-referential schema (`Node -> child/next/items -> Node`) re-enters the same `ActualSchema` forever.
- NJsonSchema's `JsonSchema.ActualSchema` has its own per-call cycle detection, but that only protects a single reference-resolution chain; because Refitter calls `schema.ActualSchema` afresh on every recursive descent, recursive object graphs still overflow at the visitor level rather than throwing NJsonSchema's cyclic-reference exception.
- The sibling method `ProcessSchemaForIntegerType()` (`src\Refitter.Core\CSharpClientGeneratorFactory.cs:285-321`) has the same traversal shape and should be fixed in the same patch or the next recursive-spec crash will simply move there.
- `PropertyNamingPolicy` is not the direct cause: `Create()` performs `ConvertOneOfWithDiscriminatorToAllOf()`, `FixMissingTypesWithIntegerFormat()`, and `ApplyCustomIntegerType()` before property-name-generator selection, and the #967 change in this file only swapped `PropertyNameGenerator = ...` to `CreatePropertyNameGenerator()`.
- `CodeGeneratorSettings.ExcludedTypeNames` is only copied later by `MapCSharpGeneratorSettings()`, so excluded types are still preprocessed today; future fixes around excluded recursive models should remember that the exclusion does not currently short-circuit schema walking.

## Issue #967 — Stack Overflow in Recursive Schema Traversal (2026-03-26)

**Team Execution:** Fenster (implementation) + Hockney (regression) + McManus (CI/harness) + Keaton (architecture/review)

### Fenster's Work
- Root cause: ProcessSchemaForMissingTypes() and ProcessSchemaForIntegerType() in CSharpClientGeneratorFactory.cs recursively traverse schemas without visited-set
- Solution: Replaced duplicated recursive preprocessing with one shared iterative visitor using Stack<JsonSchema> and HashSet<JsonSchema> cycle detection
- Key insight: Pre-existing bug predating PR #969; not caused by property-naming work
- Files: src\Refitter.Core\CSharpClientGeneratorFactory.cs

### Shared Knowledge
- Duplicated recursive traversal pattern was the root of both overflow paths
- Iterative approach with instance-based visited-set matches existing SchemaCleaner pattern in codebase
- netstandard2.0 compatible (no custom equality comparer needed)
- PreserveOriginal + recursive schemas now validated across CLI, MSBuild, and SourceGenerator paths
