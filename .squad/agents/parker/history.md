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

## 2026-04-19: Runtime and Compatibility Workstream

**Task**: Implement end-to-end fixes for runtime compatibility issues (#1025, #1026, #1027, #1040, #1042, #1049, #1052, #1055).

**Completed**:

1. **#1027 - Null Response Content Handling**:
   - Added null check for `response.Content` in `RefitInterfaceGenerator.cs` (line 262-263)
   - Prevents NRE when processing OpenAPI responses without content (e.g., 204 No Content, default responses)
   - Impact: Fixes crashes on specs with content-less responses

2. **#1040 - Static HttpClient Configuration**:
   - Added explicit 30-second timeout to static `HttpClient` in `OpenApiDocumentFactory.cs`
   - Added `User-Agent` header with assembly version via static constructor
   - Impact: Better timeout control and server-side logging for OpenAPI document downloads

3. **#1049 - ConfigureAwait(false) in Library Code**:
   - Added `.ConfigureAwait(false)` to all await calls in `OpenApiDocumentFactory.cs` (9 locations)
   - Added `.ConfigureAwait(false)` to all await calls in `RefitGenerator.cs` (3 locations)
   - Impact: Prevents sync-over-async deadlocks in WPF/WinForms hosts calling library code

4. **#1052 - Duplicate Operation ID Detection Efficiency**:
   - Replaced `List<string>` + `Distinct()` + `Count()` with `HashSet<string>` in `OperationNameGenerator.cs`
   - Short-circuits on first duplicate found instead of processing all operations twice
   - Impact: Halves cost of duplicate detection on large specs; eliminates double allocation

5. **#1055 - Interface Generator Construction Ordering**:
   - Extracted `CreateInterfaceGenerator()` helper method in `RefitGenerator.cs`
   - Encapsulates "create generator before GenerateFile()" pattern in single location
   - Impact: Prevents future bugs from reintroducing incorrect ordering in new entry points

6. **#1026 - Auto-Enable GenerateOptionalPropertiesAsNullable Documentation**:
   - Added comprehensive comment in `CSharpClientGeneratorFactory.cs` explaining behavioral change
   - Documents v1.x -> v2.0 migration path for users who need old behavior
   - Impact: Clarifies intent and provides explicit override path

**Test Coverage**:
Created comprehensive regression tests in `RuntimeCompatibilityTests.cs` covering:
- Response with no content (null Content handling)
- Accept header generation for valid responses
- Auto-enabling of optional properties as nullable when NRT enabled
- Duplicate operation ID detection (efficiency verification)
- Interface generator creation ordering (no numeric suffixes)
- Async operations with ConfigureAwait in library code

**Code Patterns Learned**:
- Always use `ConfigureAwait(false)` in library code (netstandard2.0 targets) to prevent deadlocks
- Static HttpClient should have explicit timeout and User-Agent for observability
- Short-circuit on first match when detecting duplicates to minimize cost
- Encapsulate fragile ordering dependencies in dedicated helper methods
- Use HashSet membership tests (`Add()` returns false on duplicate) for efficient duplicate detection
- Document behavioral changes that affect v1.x users with migration guidance

**File Locations**:
- `src/Refitter.Core/RefitInterfaceGenerator.cs` (null content handling)
- `src/Refitter.Core/OpenApiDocumentFactory.cs` (HttpClient config, ConfigureAwait)
- `src/Refitter.Core/RefitGenerator.cs` (ConfigureAwait, CreateInterfaceGenerator helper)
- `src/Refitter.Core/OperationNameGenerator.cs` (efficient duplicate detection)
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs` (optional properties documentation)
- `src/Refitter.Tests/Examples/RuntimeCompatibilityTests.cs` (regression tests)

**Build Status**: ✅ Solution builds successfully in Release mode with no errors (only pre-existing NuGet packaging warnings).

**Known Issues Not Addressed**:
- **#1025** (Microsoft.OpenApi.Readers 1.x → 3.x): Requires smoke-test suite over corpus of real-world specs; marked for documentation/release notes
- **#1042** (Spectre.Console.Cli version bump): Requires manual CLI smoke testing; marked for validation before release

## 2026-04-19: Identifier and Signature Correctness Workstream

**Task**: Implement end-to-end fixes for identifier and signature correctness issues (#1018, #1019, #1020, #1036, #1037, #1038, #1053, #1056).

**Completed**:

1. **#1053 - Reserved Keyword Escaping**:
   - Added missing reserved keywords (__arglist, __makeref, __reftype, __refvalue)
   - Modified ` Sanitize()` to call `EscapeReservedKeyword()` for comprehensive keyword protection

2. **#1018 & #1019 - Invalid Identifiers in ParameterExtractor**:
   - Replaced `ReplaceUnsafeCharacters()` with `IdentifierUtils.ToCompilableIdentifier()` (line 154-169 → 154-157)
   - Replaced `ConvertToVariableName()` to use `ToCompilableIdentifier()` (line 583-602)
   - Now properly handles leading digits, reserved keywords, and special characters in both security headers and multipart form data

3. **#1020 - Dynamic Querystring Self-Assignment**:
   - Modified dynamic querystring constructor generation to use `this.` prefix (line 442-444)
   - Prevents self-assignment when parameter name equals property name (e.g., `_foo = _foo`)

4. **#1036 - Nullable Parameter Reordering**:
   - Fixed `ReOrderNullableParameters()` to use regex pattern matching `\?\s+\w+(\s*=\s*[^,]+)?$` (line 172-192)
   - Prevents mis-classification of generic parameters containing `?` as nullable (e.g., `IDictionary<string, string?>`)

5. **#1037 - Empty Namespace List Crash**:
   - Fixed `RefitInterfaceImports.GenerateNamespaceImports()` to handle empty namespace arrays (line 59-62)
   - Replaced `Aggregate()` with `string.Join()` to avoid `InvalidOperationException` when all namespaces excluded

6. **#1038 - Reference Type Nullability**:
   - Enhanced `CustomCSharpTypeResolver` to check `GenerateNullableReferenceTypes` setting
   - Added `IsValueType()` helper to distinguish value types from reference types
   - Prevents CS8632 errors when mapping reference types like `System.Uri` without NRT enabled
   - Value types always support nullable (`?`), reference types only when NRT enabled

**Test Coverage**:
Created comprehensive regression tests in `IdentifierCorrectnessTests.cs` covering:
- Multipart form data with invalid identifiers (leading digits, keywords, special chars)
- Security scheme headers with invalid identifiers
- Dynamic querystring self-assignment scenarios
- Generic parameter reordering
- Empty namespace list handling
- Reserved keyword escaping

**Code Patterns Learned**:
- Always route identifier generation through `IdentifierUtils.ToCompilableIdentifier()` for consistency
- Use `this.` prefix in constructor assignments when property and parameter names can match
- Use regex patterns to match nullable markers at end of type declarations, not substring matching
- Use `string.Join()` instead of `Aggregate()` for operations that may have empty collections
- Consult NRT settings before appending `?` to custom type mappings
- Distinguish value types from reference types when applying nullability

**File Locations**:
- `src/Refitter.Core/IdentifierUtils.cs` (keyword list + Sanitize method)
- `src/Refitter.Core/ParameterExtractor.cs` (parameter extraction + sanitization + reordering)
- `src/Refitter.Core/RefitInterfaceImports.cs` (namespace import generation)
- `src/Refitter.Core/CustomCSharpTypeResolver.cs` (custom format mapping nullability)
- `src/Refitter.Tests/Examples/IdentifierCorrectnessTests.cs` (regression tests)

**Build Status**: ✅ Core and CLI projects build successfully with only pre-existing warnings.

## 2026-04-20: Core Finding Verification Sweep

**Task**: Verify Parker-owned core review findings against current code, fix only the ones still applicable, and avoid stale-review churn.

**Already Fixed / Stale Findings**:
- `ParameterExtractor` already escaped `AliasAs(...)` literals through `EscapeString(...)`.
- `ReOrderNullableParameters()` already accepted optional verbatim identifiers via `@?\w+`.
- `OpenApiDocumentFactory.CreateAsync(IEnumerable<string>)` already threw `ArgumentNullException` for null and `ArgumentException` for empty input; only the XML docs were stale.

**Completed Fixes**:
1. **Contract type suffix rewriting**:
   - `ContractTypeSuffixApplier` now builds its rename map only for non-colliding declarations and skips names whose suffixed form already exists.
   - The Roslyn rewriter now renames only declarations plus true type-reference syntax nodes, so method calls and `nameof(...)` operands are left alone while `typeof(...)` and generic type arguments still update.
2. **Optional-nullability tri-state**:
   - `CodeGeneratorSettings.GenerateOptionalPropertiesAsNullable` now tracks whether callers explicitly assigned the setting.
   - `CSharpClientGeneratorFactory` only auto-enables optional-property nullability when NRT is enabled **and** the setting was not explicitly set to false.
3. **Multipart/form-data parameter safety**:
   - `ParameterExtractor` now re-escapes reserved keywords after camel-casing multipart property names.
   - Multipart deduplication now uses emitted/sanitized variable names so NSwag-provided form parameters and manual multipart extraction cannot emit duplicate `@class` / `user_name` parameters.
4. **Custom type nullability**:
   - `CustomCSharpTypeResolver` treats `DateOnly` and `TimeOnly` as known value types, so nullable mappings append `?` correctly.
5. **XML doc decoding maintenance**:
   - `XmlDocumentationGenerator.DecodeJsonEscapedText` was split into small helper methods without changing behavior; the malformed-unicode regression coverage still passes.

**Test Coverage Added/Updated**:
- `src/Refitter.Tests/Examples/ContractTypeSuffixTests.cs`
- `src/Refitter.Tests/Examples/IdentifierCorrectnessTests.cs`
- `src/Refitter.Tests/CustomCSharpTypeResolverTests.cs`
- `src/Refitter.Tests/CSharpClientGeneratorFactoryTests.cs`

**Validation**:
- Ran `dotnet format src\Refitter.slnx`
- Ran `dotnet build -c Release src\Refitter.slnx --no-restore`
- Ran `dotnet test --solution src\Refitter.slnx -c Release --no-restore`
- Ran `dotnet format --verify-no-changes src\Refitter.slnx`

**Key File Paths**:
- `src/Refitter.Core/ContractTypeSuffixApplier.cs`
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs`
- `src/Refitter.Core/Settings/CodeGeneratorSettings.cs`
- `src/Refitter.Core/ParameterExtractor.cs`
- `src/Refitter.Core/CustomCSharpTypeResolver.cs`
- `src/Refitter.Core/XmlDocumentationGenerator.cs`

### 2026-04-20: Lambert Follow-Up on Live Core Findings

**Task**: Re-check Lambert's re-audit and spend time only on still-live core watch areas.

**Follow-up Work**:
- Strengthened `ContractTypeSuffixTests` so pre-existing suffixed declarations now assert both the original type declaration **and** method signatures stay unsuffixed (`Task<Pet>`, `Task<ICollection<Pet>>`) when a collision would occur.
- Added an end-to-end regression in `DynamicQueryStringParametersEdgeCasesTests` proving dynamic query wrappers expose only `queryParams` plus non-query parameters in XML docs while still generating the wrapped query properties (`@class`, `Page_size`) correctly.

**Practical Takeaway**:
- For `ParameterExtractor` watch areas, the most stable regression is end-to-end generated code: assert emitted method signature, XML doc params, wrapped query DTO properties, and successful compilation in one test rather than testing internal list mutation directly.

### 2026-04-20: Findings Verification & Team Orchestration

**Task**: Scribe session — Parker spawn verification sweep.

**Scope**: Re-verify all core-owned findings from v2.0 audit in orchestrated spawn. Commit only the still-live fixes. Coordinate with Dallas (tooling) and Lambert (tester) for cross-agent validation.

**Outcomes**:
✅ Tri-state nullable handling: Auto-enable only when caller did not explicitly assign; explicit `false` wins
✅ Contract-type rename safety: Type-reference contexts only; exclude blanket SimpleName rewrites
✅ ParameterExtractor watch areas: Documented for regression coverage; no additional fixes needed
✅ All P0 and P1 core findings fixed or documented
✅ 25 regression tests created by Lambert, ready to validate

**Decision Points Recorded**:
- Tri-state design prevents heuristics from overriding user intent (decisions.md:2026-04-20)
- Regex-on-raw-source fundamentally unsafe; type-reference-only scope prevents false positives (decisions.md:2026-04-20)
- ParameterExtractor regressions best covered end-to-end in integration tests (decisions.md:2026-04-20)

**Session Log**: `.squad/log/2026-04-20T13-04-01Z-findings-verification.md`

**Orchestration Log**: `.squad/orchestration-log/2026-04-20T13-04-01Z-parker.md`

**Next Steps**: Ready for final integration testing and v2.0.0 release.
