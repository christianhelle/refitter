# Parker History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.

### 2026-04-17: Release Compatibility Audit (1.7.3 to HEAD)

**Task**: Audit generator-core changes between 1.7.3 and HEAD for breaking behavior ahead of major release.

**Key Finding - CONFIRMED BREAKING CHANGE**: 
- `GenerateAuthenticationHeader` property changed from `bool` to `AuthenticationHeaderStyle` enum (commit 7dbf6c0c, 14101a49).
- **Impact**: Source compatibility break. Existing .refitter files using `"generateAuthenticationHeader": true` will fail JSON deserialization.
- **Runtime impact**: `AuthenticationHeaderStyle` defaults to `None` (0), whereas old `bool` defaulted to `false`. Users who explicitly set `true` will see authentication headers **disappear** after upgrade unless they migrate to `"authenticationHeaderStyle": "Parameter"`.
- **Confidence**: 100% - This is a hard breaking change affecting settings deserialization.

**Non-Breaking Changes (Bug Fixes, Safe Additions)**:
1. **JsonConverter attribute placement** (1b9a76c8): Moved from enum properties to enum types. Fixes user ability to override converters. Not breaking - only affects previously broken scenarios (hyphened enum values).
2. **Stack overflow fix** (3d9cdb6c): Schema traversal cycle detection. Only affects previously crashing inputs (recursive schemas).
3. **PropertyNamingPolicy** (76230c9e): New optional setting, defaults to `PascalCase` (existing behavior). No break.
4. **Auto-enable GenerateOptionalPropertiesAsNullable** (29de01ed): Only activates when `GenerateNullableReferenceTypes` is already true. Behavioral change but makes nullable reference types actually work correctly - likely expected behavior.
5. **Digit-prefixed property names** (fdcf675b): Now prefixed with underscore for C# compilation. Fixes previously invalid generated code.
6. **Multipart form-data extraction** (57498ea5): Fixes missing parameters. Only affects previously incomplete code generation.
7. **Method naming in ByTag mode** (79939948): Scopes per-interface instead of global. Prevents unexpected numeric suffixes - improves output quality.
8. **OneOf with discriminator transformation** (CSharpClientGeneratorFactory): Converts to allOf pattern for NSwag. Fixes undefined anonymous types.
9. **ContractTypeSuffix, SecurityScheme, OpenApiPaths, GenerateJsonSerializerContext**: New optional features with safe defaults.

**Audit Result**: BREAKING CHANGE confirmed. Team consensus: major version 2.0.0 required.

**Code Patterns Learned**:
- Schema traversal must use instance-based visited tracking with `HashSet<JsonSchema>` and `ActualSchema` resolution to prevent stack overflow.
- Settings format changes (type changes, not just new properties) constitute hard breaks for .refitter file users.
- NSwag's oneOf/anyOf handling requires preprocessing transformation to allOf for proper inheritance generation.

**File Locations**:
- Core settings: `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`
- Schema preprocessing: `src/Refitter.Core/CSharpClientGeneratorFactory.cs`
- Generator logic: `src/Refitter.Core/RefitGenerator.cs`
- Parameter extraction: `src/Refitter.Core/ParameterExtractor.cs`

### 2026-04-18: P0 Audit Issue Verification

**Task**: Verify 6 critical (P0) issues from v2.0 audit by examining current codebase.

**Verified Issues**:

1. **#1011 - SourceGenerator hint-name collisions** (VALID)
   - Location: `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs:155-160`
   - Bug: Uses `Path.GetFileNameWithoutExtension(file.Path)` only for hint name, causing collisions when multiple .refitter files share filename in different directories
   - Impact: `ArgumentException: hintName 'X.g.cs' was already added` crashes analyzer
   - Notes: `filename` variable from `OutputFilename` (line 148) is computed but never used in hint name

2. **#1012 - MSBuild task swallows CLI failures** (VALID)
   - Location: `src/Refitter.MSBuild/RefitterGenerateTask.cs:22-52, 105-129`
   - Bug: `Execute()` unconditionally returns `true` (line 51), never checks `process.ExitCode` (line 125)
   - Impact: Build succeeds with stale/missing output when CLI fails; no errors logged to MSBuild
   - Notes: `TryExecuteRefitter` returns null on exception (line 63) but caller ignores it

3. **#1013 - ContractTypeSuffixApplier regex corruption** (VALID)
   - Location: `src/Refitter.Core/ContractTypeSuffixApplier.cs:36-56`
   - Bug: Line 50-55 uses raw `\b{typeName}\b` word-boundary regex on entire generated source
   - Impact: Renames type references in comments, strings, member names; no collision detection; no double-suffix protection
   - Notes: Does use `OrderByDescending(t => t.Length)` to reduce partial match risk, but still unsafe

4. **#1014 - Forced JsonStringEnumConverter injection** (PARTIAL)
   - Location: `src/Refitter.Core/RefitGenerator.cs:12-20, 264-283`
   - Bug CONFIRMED: Enum regex `^(\s*)(public\s+(?:partial\s+)?enum\s+\w+\b)` (line 18) only matches `public` enums, not `internal` → internal enums silently lose converter attributes
   - Bug CONFIRMED: Hard-coded STJ converter injection (line 275) ignores `JsonLibrary` setting
   - Bug NOT FOUND: Generic form stripping claim appears outdated - regex includes `(?:<[\w.]+>)?` (line 13)
   - Impact: Newtonsoft users get STJ references; internal enums serialize as integers
   - Note: `CSharpClientGeneratorFactory.cs:38` forces `JsonLibrary = CSharpJsonLibrary.SystemTextJson` (hard-coded, user setting ignored)

5. **#1015 - ConvertOneOfWithDiscriminatorToAllOf NRE** (VALID)
   - Location: `src/Refitter.Core/CSharpClientGeneratorFactory.cs:97-131`
   - Bug: Line 99 `foreach (var kvp in document.Components.Schemas)` has no null check
   - Impact: NRE on every Swagger 2.0 doc (uses `definitions`, not `components`) and OpenAPI 3.0 docs without components
   - Notes: Sibling methods like `EnumerateDocumentSchemaRoots()` (line 200) properly check `document.Components?.Schemas != null`

6. **#1016 - Multi-spec merge drops schemas** (VALID)
   - Location: `src/Refitter.Core/OpenApiDocumentFactory.cs:64-72`
   - Bug: Line 68 checks `baseDocument.Components?.Schemas != null` before adding schemas from later docs
   - Impact: If first spec lacks components/schemas, all schemas from subsequent specs are silently dropped
   - Notes: No lazy initialization of `baseDocument.Components` or `.Schemas` dictionary

**Code Patterns Learned**:
- Source generator hint names must disambiguate by path (not just filename) to avoid collisions in multi-project solutions
- MSBuild tasks MUST check `process.ExitCode` and return `false` on failure, otherwise CI/CD silently ships broken builds
- Regex replacement on raw source text is extremely fragile - should operate on NSwag model or Roslyn syntax tree instead
- Enum accessibility patterns must include `(public|internal)` to match NSwag's actual output
- OpenAPI document traversal must always null-check `document.Components` and `document.Components.Schemas` - Swagger 2 uses `definitions` instead
- Multi-document merge must lazily initialize collections on base document to handle "split" API definitions (paths-only + schemas-only)

**File Locations**:
- Source generator: `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs`
- MSBuild task: `src/Refitter.MSBuild/RefitterGenerateTask.cs`
- Type suffix applier: `src/Refitter.Core/ContractTypeSuffixApplier.cs`
- Contract sanitization: `src/Refitter.Core/RefitGenerator.cs`
- Schema preprocessing: `src/Refitter.Core/CSharpClientGeneratorFactory.cs`
- Document factory: `src/Refitter.Core/OpenApiDocumentFactory.cs`
