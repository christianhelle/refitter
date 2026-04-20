# Refitter v2.0 Pre-release Audit Findings

> Code review of all changes between tag `1.7.3` and `main` (~360 commits, ~1,700 LOC changed).
> Reviewed by 4 parallel analyzers partitioning the diff by subsystem.
> GitHub tracking epic: https://github.com/christianhelle/refitter/issues/1057

## Summary

| Priority | Count | GitHub Issues |
|----------|-------|---------------|
| 🔴 P0 Critical | 6 | #1011 – #1016 |
| 🟠 P1 High | 11 | #1017 – #1027 |
| 🟡 P2 Medium | 16 | #1028 – #1043 |
| 🟢 P2 Low | 13 | #1044 – #1056 |
| **Total** | **46** | **#1011 – #1056** |

**Tracking epic:** https://github.com/christianhelle/refitter/issues/1057

**Excluded (intentional breaking changes):**
- Settings rename `generateAuthenticationHeader` → `authenticationHeaderStyle`
- Source generator switching to `context.AddSource()` (architectural; see C1 for the regression it introduced)
- MSBuild output path fix (#998)

---

## 🔴 P0 — Critical (must-fix before v2.0)

### [C1] SourceGenerator: hint-name collisions when two .refitter files share a filename

**GitHub:** https://github.com/christianhelle/refitter/issues/1011

**Severity:** Critical | **Component:** Refitter.SourceGenerator | **v2.0 audit (since v1.7.3)**

The hint name passed to `context.AddSource` is computed from the `.refitter` filename only, so two `.refitter` files with the same filename in different directories collide and crash the analyzer for the whole build/IDE.

### Repro
A solution containing both `src/ApiA/petstore.refitter` and `src/ApiB/petstore.refitter` (common when generating a client per project from the same upstream API). Both yield `hintName = "petstore.g.cs"`. The second `context.AddSource("petstore.g.cs", …)` throws `ArgumentException: hintName 'petstore.g.cs' was already added`.

Under the old `File.WriteAllText` flow (pre-PR #923) this worked because the two outputs went to different physical directories.

### Code
- `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs:148-160`

The local `filename` variable computed from `settings.OutputFilename` is currently dead code — the rewrite lost the intent.

### Fix
Disambiguate using the relative path or a stable hash of `file.Path`:
```csharp
var safeDir = Path.GetDirectoryName(file.Path)!.Replace(Path.DirectorySeparatorChar, '_').Replace(':', '_');
hintName = $"{safeDir}_{Path.GetFileNameWithoutExtension(file.Path)}.g.cs";
```
Also honor `settings.OutputFilename` when present.

Regression source: PR #923. Linked in v2.0 audit epic.

---

### [C2] MSBuild task swallows CLI failures; build always reports success with stale/missing output

**GitHub:** https://github.com/christianhelle/refitter/issues/1012

**Severity:** Critical | **Component:** Refitter.MSBuild | **v2.0 audit (since v1.7.3)**

`RefitterGenerateTask.Execute()` ignores the spawned `dotnet refitter.dll …` exit code and unconditionally returns `true`. CI pipelines silently ship stale generated code without any warning.

### Repro
1. Add a `.refitter` file pointing to an unreachable URL (or invalid spec).
2. Run `dotnet build`.
3. Build succeeds, no error logged. The previous `*.g.cs` (if any) is consumed; `GeneratedFiles` ends up empty because `expectedFiles.Where(File.Exists)` filters everything out.
4. Downstream `<Compile>` items disappear → user sees compile errors several stack frames removed from the actual cause.

### Code
- `src/Refitter.MSBuild/RefitterGenerateTask.cs:22-52, 105-129`

`process.WaitForExit()` is called but `process.ExitCode` is never inspected.

### Fix
1. Capture `process.ExitCode`. If non-zero, `Log.LogError(...)` and return `false`.
2. Return `false` if `TryExecuteRefitter` returned null (exception path).
3. Add `process.WaitForExit(timeout)` to avoid build hangs.

---

### [C3] ContractTypeSuffixApplier corrupts code via raw word-boundary regex (renames members, comments, strings; double-suffix on rerun)

**GitHub:** https://github.com/christianhelle/refitter/issues/1013

**Severity:** Critical | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`ContractTypeSuffixApplier` runs `Regex.Replace` with `\b...\b` over the raw generated source, rewriting *every* textual occurrence of a contract type name.

### Failure modes
- A schema producing both `class Foo` and `Foo Foo()` (member named the same as type) → both renamed to `FooDto`, breaking method/property names.
- `using` clauses, comments, XML docs, string literals containing the bare token are rewritten.
- Re-running with `Suffix=Dto` on already-suffixed code produces `FooDtoDto`.
- When both `Foo` and `FooDto` already exist, output has duplicate `FooDto` declarations → CS0246 / CS0101.
- No collision detection.

### Code
- `src/Refitter.Core/ContractTypeSuffixApplier.cs:33-56`

### Fix
Operate on the NSwag model **before** string emission, or on a Roslyn syntax tree. At minimum:
- Skip names already ending with `suffix`.
- Detect collisions with already-suffixed names and bail/warn.
- Limit the rename regex to type-reference grammar contexts (after `:`, `<`, `,`, `(`, `[`, `new `, `typeof(`, etc.).

Related: JsonSerializerContextGenerator extracts type names *after* this runs (audit epic).

---

### [C4] Forced JsonStringEnumConverter injection breaks Newtonsoft users and silently regresses internal enums

**GitHub:** https://github.com/christianhelle/refitter/issues/1014

**Severity:** Critical | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`SanitizeGeneratedContracts` strips per-property `JsonStringEnumConverter` attributes and unconditionally prepends `[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]` on every `public [partial] enum`.

### Failure modes
1. **Newtonsoft users** (`JsonLibrary == NewtonsoftJson`): generated code now contains a hard reference to `System.Text.Json.Serialization.JsonConverter` even when STJ is not referenced → build fails. If STJ is transitively present, runtime serialization differs.
2. **Internal enums** (`TypeAccessibility = Internal`): regex `^(\s*)(public\s+(?:partial\s+)?enum\s+\w+\b)` only matches `public` enums. NSwag emits `internal partial enum Foo`, which is **not** matched → enums get neither the per-property converter (stripped) nor the type-level one. Enums silently start serializing as integers — runtime regression vs v1.7.3.
3. The strip regex `(?:<[\w.]+>)?` only allows word characters + dot, so nested generic forms like `JsonStringEnumConverter<Outer<Inner>>` are not stripped reliably.

### Code
- `src/Refitter.Core/RefitGenerator.cs:13-21, 264-283`
- `src/Refitter.Core/Settings/CodeGeneratorSettings.cs:266-282`

### Fix
- Gate injection on `JsonLibrary == SystemTextJson`.
- Expand regex to `(public|internal)`.
- Better: have NSwag emit the converter via existing `EnumStyle` settings rather than mutating output via regex.

Related: mixed CRLF/LF in generated source from hard-coded `\n` in same regex (audit epic).

---

### [C5] ConvertOneOfWithDiscriminatorToAllOf NRE on Swagger 2 / OpenAPI 3 docs without components

**GitHub:** https://github.com/christianhelle/refitter/issues/1015

**Severity:** Critical | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`ConvertOneOfWithDiscriminatorToAllOf` does an unguarded `foreach (var kvp in document.Components.Schemas)`, throwing `NullReferenceException` immediately on:
- Every Swagger 2 (OpenAPI 2) document — those use `definitions`, not `components/schemas`.
- Any OpenAPI 3 document with no `components` section.

The sibling `ApplyCustomIntegerType` traversal (via `EnumerateDocumentSchemaRoots`) does null-check, but this method does not.

### Code
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs:97-99`

### Fix
```csharp
if (document.Components?.Schemas == null) return;
```

---

### [C6] Multi-spec merge silently drops all schemas when first spec has no components

**GitHub:** https://github.com/christianhelle/refitter/issues/1016

**Severity:** Critical | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

When `OpenApiPaths` lists multiple specs, `OpenApiDocumentFactory.Merge` short-circuits the schema-merge loop because the inner check is `baseDocument.Components?.Schemas != null`. If the **base** (first) spec lacks a `components/schemas` section — common in "split" API definitions — every schema from later docs is silently discarded. Generated code then references types that are never defined.

### Repro
```json
{ "openApiPaths": ["paths-only.yaml", "schemas-only.yaml"] }
```
Where `paths-only.yaml` has no `components/schemas`. Result: generated client doesn't compile because all DTOs are missing.

### Code
- `src/Refitter.Core/OpenApiDocumentFactory.cs:64-72`

### Fix
Lazily initialize the base document's collections before merging:
```csharp
baseDocument.Components ??= new OpenApiComponents();
baseDocument.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();
```
Apply same to `Definitions` for Swagger 2 inputs, and to `Tags`/`Paths`/`SecuritySchemes`.

Related: `Merge` also mutates `documents[0]` and silently drops conflicting paths/schemas (audit epic).

---

## 🟠 P1 — High

### [H1] JsonSerializerContextGenerator emits non-compiling AOT context (generics, namespaces, polymorphism, nested types)

**GitHub:** https://github.com/christianhelle/refitter/issues/1017

**Severity:** High | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`JsonSerializerContextGenerator` re-parses the emitted C# with regex to discover types, producing several broken outputs.

### Failure modes
1. **Generic types**: regex `(?:class|record)\s+([A-Za-z_][A-Za-z0-9_]*)` captures `PagedResult` from `public partial class PagedResult<T>` → `[JsonSerializable(typeof(PagedResult))]` (CS0246). Open generics `typeof(PagedResult<>)` are not valid `JsonSerializable` targets either; each closed instantiation needs to be registered.
2. **Namespace mismatch**: when `ContractsNamespace` differs from interface namespace, generated context references `typeof(Foo)` without qualification or a `using` → CS0246. The class itself is also emitted without a `namespace` block.
3. **Polymorphism**: discriminator-driven base classes need `[JsonDerivedType(typeof(SubA), "a")]`. None is emitted → polymorphic deserialization silently falls back to base under AOT.
4. **Nested types** (`Outer.Inner`): emits `typeof(Inner)` → CS0246.
5. Context name is `{InterfaceName}SerializerContext` → `IFooSerializerContext` retains leading `I`.

### Code
- `src/Refitter.Core/JsonSerializerContextGenerator.cs:23-76`

### Fix
Drive emission from NSwag's resolved type list (with full type symbols, generic args, discriminator info) rather than re-parsing emitted C#. Emit fully qualified names; register every closed generic instantiation; add `[JsonDerivedType]` for `oneOf`/`discriminator` bases; accept an explicit context-name setting.

Related: ContractTypeSuffixApplier (audit epic) runs first → strings here are already suffixed.

---

### [H2] ParameterExtractor.ConvertToVariableName produces invalid C# identifiers (multipart form-data fields)

**GitHub:** https://github.com/christianhelle/refitter/issues/1018

**Severity:** High | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`ParameterExtractor.ConvertToVariableName` (used at line 106) does not route through the existing `IdentifierUtils.ToCompilableIdentifier`, so multipart form-data property keys can produce invalid identifiers.

### Repro
- `"123File"` → `"123File"` (leading-digit identifier, CS1056).
- `"class"`, `"event"`, `"namespace"` → emitted verbatim (reserved keyword without `@` escape, CS1041).
- `"!"` → `"_"` (collisions across siblings).
- `"Café"` → unchanged (OK), but Unicode digits / combining marks are accepted by `IsLetterOrDigit` though invalid as the first identifier character.

### Code
- `src/Refitter.Core/ParameterExtractor.cs:583-602` (used at line 106)

### Fix
Route through `IdentifierUtils.ToCompilableIdentifier` (already handles digit-prefix + reserved keywords per #991/#997), then `EscapeReservedKeyword`, then de-duplicate within the operation.

Related: H3 (security scheme), H4 (dynamic-querystring self-assign).

---

### [H3] Security-scheme header parameter name not safely sanitized

**GitHub:** https://github.com/christianhelle/refitter/issues/1019

**Severity:** High | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`ReplaceUnsafeCharacters` swaps non-letter-or-digit chars with `_`, but does not handle leading-digit, reserved keywords, or empty names.

### Repro
- `"X-Api-Key"` → `"X_Api_Key"` (OK, but not camelCase like other params — collisions possible if another parameter was named `xApiKey`).
- `"1Token"` → `"1Token"` (illegal leading digit, CS1056).
- `"class"` → `"class"` (reserved keyword, CS1041).
- `""` → `""` (empty parameter name, compile error).

### Code
- `src/Refitter.Core/ParameterExtractor.cs:68`
- `ReplaceUnsafeCharacters` at lines 154-170

### Fix
Use `IdentifierUtils.ToCompilableIdentifier(securityScheme.Name)` (or at minimum prepend `_` when leading char isn't letter/`_` and pass through `EscapeReservedKeyword`).

Related: H2.

---

### [H4] Dynamic-querystring constructor self-assigns when parameter name starts with non-letter

**GitHub:** https://github.com/christianhelle/refitter/issues/1020

**Severity:** High | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`ParameterExtractor` uses `propertyName = operationParameter.VariableName.CapitalizeFirstCharacter()` and `CapitalizeFirstCharacter` (`StringCasingExtensions.cs:39`) does `Substring(0,1).ToUpperInvariant()`. For an NSwag-sanitized variable name like `_foo` (digit-prefixed schema property mangled per PR #997) — `ToUpperInvariant('_') == '_'` — `propertyName == VariableName == "_foo"`.

Emitted code:
```csharp
public Wrapper(string _foo)
{
    _foo = _foo;   // assigns parameter to itself; property never set
}
```
C# resolves `_foo` to the parameter on both sides → property silently stays at default → query string sent without the value the caller passed.

Same hazard for any name starting with a digit (after PR #997 those become `_1abc`).

### Code
- `src/Refitter.Core/ParameterExtractor.cs:432-444`
- `src/Refitter.Core/StringCasingExtensions.cs:39`

### Fix
Use `this.{propertyName} = {variableName};`, or guarantee property name differs (always prefix when capitalize is no-op).

Related: H2.

---

### [H5] CLI: --output / -o no longer overrides settings-file outputFolder (script regression)

**GitHub:** https://github.com/christianhelle/refitter/issues/1021

**Severity:** High | **Component:** Refitter (CLI) | **v2.0 audit (since v1.7.3)**

The `OutputFolder != DefaultOutputFolder` guard was removed from `GetOutputPath`. Combined with `ApplySettingsFileDefaults` setting `OutputFolder="./Generated"`, every CI script using `-o` against a `.refitter` is silently broken (unless `-o` is absolute).

### Repro
```
refitter --settings-file foo.refitter --output Bar.cs
```
With `foo.refitter` having no `outputFolder`. Old behavior wrote to `Bar.cs`. New behavior writes to `./Generated/Bar.cs` because `Path.Combine(root, "./Generated", "Bar.cs")` runs.

### Code
- `src/Refitter/GenerateCommand.cs:665-679, 724-731`
- Regression source: commits 20b6014e, 46e2d5b6 ("Streamline output path resolution in single file mode").

### Fix
Restore the guard, OR detect explicit `-o` (compare to `Settings.DefaultOutputPath`) and prefer it over the settings-file folder. Add a regression test.

Related: H6 (predicted-vs-actual MSBuild divergence), C2 (silent failures).

---

### [H6] MSBuild predicted output paths diverge from CLI actual paths → silent missing compile items

**GitHub:** https://github.com/christianhelle/refitter/issues/1022

**Severity:** High | **Component:** Refitter.MSBuild | **v2.0 audit (since v1.7.3)**

The MSBuild task predicts output paths via regex on `.refitter` file contents. CLI's actual output-path resolution differs (and recently changed — see H5). When predictions miss, `expectedFiles.Where(File.Exists)` returns empty → downstream `<Compile>` items disappear (and combined with C2, no error surfaces).

### Failure modes
- Multi-file mode hard-codes `RefitInterfaces.cs`, `Contracts.cs`, `DependencyInjection.cs`, but Core can produce other names (`MultipleInterfaces=ByEndpoint` / per-tag).
- Any `.refitter` setting tweaks not modeled by the task's regex.

### Code
- `src/Refitter.MSBuild/RefitterGenerateTask.cs:209-218`
- `src/Refitter/GenerateCommand.cs:684-704`

### Fix
Drive predicted file list from the actual generator (or have CLI emit a manifest the task reads) instead of duplicating output-path logic via regex.

Related: H5, C2, L4.

---

### [H7] MSBuild IncludePatterns is substring-matched, over-includes files

**GitHub:** https://github.com/christianhelle/refitter/issues/1023

**Severity:** High | **Component:** Refitter.MSBuild | **v2.0 audit (since v1.7.3)**

`IncludePatterns` filter falls back to `IndexOf(pattern) >= 0`. XML doc claims patterns like `"petstore.refitter;petstore-default.refitter"` (suggesting filename equality), but substring matching silently includes unintended files.

### Repro
- Pattern `petstore.refitter` also matches `internal-petstore.refitter.refitter`, `petstore.refitter.bak.refitter`.
- Pattern `pet` matches `mypet.refitter`.

### Code
- `src/Refitter.MSBuild/RefitterGenerateTask.cs:303-321`

### Fix
Drop substring fallback. Either (a) require exact filename/relative-path equality, or (b) treat patterns as globs using `Microsoft.Extensions.FileSystemGlobbing.Matcher`. Document chosen semantics.

---

### [H8] Refit major bump 9 → 10 silently leaks to Refitter.SourceGenerator consumers

**GitHub:** https://github.com/christianhelle/refitter/issues/1024

**Severity:** High | **Component:** Refitter.SourceGenerator | **v2.0 audit (since v1.7.3)**

`Refitter.SourceGenerator.csproj` references `Refit 10.1.6` and `OasReader 3.5.0.19` without `PrivateAssets="all"`. NSwag refs in same csproj already use that pattern — Refit and OasReader appear inconsistent.

### Repro
A user upgrading the source generator from 1.7.x to 2.0 silently gets Refit 10 pulled into their app via transitive resolution. Refit 10 contains breaking API changes vs 9 (multipart, error handling, `IApiResponse` surface). Apps pinned to Refit 9 see surprise upgrades or, if they pin Refit 9 explicitly, build failures from generated code that may rely on Refit 10 surface.

### Code
- `src/Refitter.SourceGenerator/Refitter.SourceGenerator.csproj:21-22`

### Fix
- Decide intentionally: if generated code requires Refit 10, document it as a breaking change in v2.0 and bump the package's major version (which v2.0 does).
- Set `PrivateAssets="all"` on `OasReader` (it's a generator-time dep only, like NSwag).
- For Refit, consider `PrivateAssets="compile"` and a clear note in the SourceGenerator README.

---

### [H9] Microsoft.OpenApi.Readers 1.x → 3.x silently changes parsing/codegen for users

**GitHub:** https://github.com/christianhelle/refitter/issues/1025

**Severity:** High | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`OasReader` upgraded from 1.6.16.16 → 3.5.0.19 (CHANGELOG entry: `Migrate from Microsoft.OpenApi.Readers 1.x to Microsoft.OpenApi 3.x [#907]`).

Microsoft.OpenApi 3.x has materially different schema interpretation vs 1.x (nullability, discriminator handling, `oneOf`/`anyOf`, `$ref` resolution, examples). Existing user specs that generated cleanly under 1.7.3 may now produce different (or invalid) C# output without any obvious config change.

### Code
- `src/Refitter.Core/Refitter.Core.csproj:17`

### Fix
- Run smoke-test suite over a corpus of real-world OpenAPI documents and diff generated output 1.7.3 vs HEAD.
- Document any meaningful diffs as known migration items.
- Add release-note section: "OpenAPI parser upgraded to Microsoft.OpenApi 3.x — please regenerate and review diff."

---

### [H10] Auto-enabling GenerateOptionalPropertiesAsNullable is a silent breaking shape change

**GitHub:** https://github.com/christianhelle/refitter/issues/1026

**Severity:** High | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

```csharp
if (generator.Settings.CSharpGeneratorSettings.GenerateNullableReferenceTypes)
    generator.Settings.CSharpGeneratorSettings.GenerateOptionalPropertiesAsNullable = true;
```

Any user with `GenerateNullableReferenceTypes=true` and `GenerateOptionalPropertiesAsNullable=false` (the prior default) now gets a different, breaking shape on contract DTOs (every optional property becomes nullable). Their downstream consumers may stop compiling.

There is no "was set" tri-state on `RefitGeneratorSettings` to detect explicit `false`.

### Code
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs:68-72`

### Fix
Either (a) leave the setting alone and document the recommended combination, (b) only force-enable when the user has not explicitly set the value (requires tri-state), or (c) at minimum add a CHANGELOG entry that flags the silent default change.

---

### [H11] RefitInterfaceGenerator NRE when an OpenAPI response has no content

**GitHub:** https://github.com/christianhelle/refitter/issues/1027

**Severity:** High | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

```csharp
foreach (var response in operations.Value.Responses.Values)
{
    foreach (var contentType in response.Content.Keys)   // NRE if response.Content is null
        uniqueContentTypes.Add(contentType);
}
```

In OpenAPI it is perfectly legal for a response (e.g. `204 No Content`, `default`, error responses) to omit the `content` map. NSwag exposes that as `OpenApiResponse.Content == null`. The new flattening throws NRE during `AddAcceptHeaders` for any spec containing such responses.

### Code
- `src/Refitter.Core/RefitInterfaceGenerator.cs:259-266`

### Fix
```csharp
if (response.Content == null) continue;
foreach (var contentType in response.Content.Keys) ...
```

---

## 🟡 P2 — Medium / Low

### [M1] SourceGenerator pipeline output uses List<Diagnostic> (defeats incremental caching)

**GitHub:** https://github.com/christianhelle/refitter/issues/1028

**Severity:** Medium | **Component:** Refitter.SourceGenerator | **v2.0 audit (since v1.7.3)**

`record GeneratedCode(List<Diagnostic>, …)` — `IncrementalGenerator` pipeline outputs require value-equality for caching. `List<T>` uses reference equality, so every keystroke causes the `Select(GenerateCode)` step to be re-evaluated and `RegisterImplementationSourceOutput` to re-run, defeating IDE incremental code generation. With v2.0 calling `RefitGenerator.CreateAsync(...).GetAwaiter().GetResult()` synchronously inside `GenerateCode`, the perf cost is real (multi-second NSwag work on every IntelliSense tick).

### Code
- `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs:207`

### Fix
Use `EquatableArray<Diagnostic>` (already-referenced `H.Generators.Extensions`) or implement `IEquatable<GeneratedCode>` with structural equality on diagnostics + code + hint name. Also short-circuit when `file.GetText()` content hasn't changed.

---

### [M2] SourceGenerator user-visible warnings only emit to Debug.WriteLine (no-op in Release)

**GitHub:** https://github.com/christianhelle/refitter/issues/1029

**Severity:** Medium | **Component:** Refitter.SourceGenerator | **v2.0 audit (since v1.7.3)**

The diff comment promised "warning if no .refitter files were found, instructing the user how to add them". In Release builds (which is how source generators normally execute under `dotnet build`), `Debug.WriteLine` is a no-op — the user sees nothing.

### Code
- `src/Refitter.SourceGenerator/RefitterSourceGenerator.cs:32-46`
- Commit 7eb9fe5b "Add debug logging for source generator …"

### Fix
Emit a Roslyn `Diagnostic` (e.g. `REFITTER003`, severity `Info`/`Warning`) via `spc.ReportDiagnostic`, which is the supported, visible channel. Same for the per-file "Found" log.

---

### [M3] SettingsValidator only validates the first entry of openApiPaths

**GitHub:** https://github.com/christianhelle/refitter/issues/1030

**Severity:** Medium | **Component:** Refitter (CLI) | **v2.0 audit (since v1.7.3)**

`.refitter` file containing `"openApiPaths": ["a.json", "b.json", "c.json"]`. Validator only assigns `settings.OpenApiPath = paths[0]`, used both for downstream validation and for the actual single-spec validation pass. Specs `b.json` and `c.json` are never validated, even though `--skip-validation` was *not* passed.

### Code
- `src/Refitter/SettingsValidator.cs:46-49`
- Commit 2f34d59e "Add support for generating client from multiple OpenAPI specifications"

### Fix
Iterate `OpenApiPaths` in `ValidateOpenApiSpec`, or skip CLI-level validation when paths > 1 and emit a clear info message.

---

### [M4] ValidateOpenApiSpec does not resolve relative spec paths from settings-file directory

**GitHub:** https://github.com/christianhelle/refitter/issues/1031

**Severity:** Medium | **Component:** Refitter (CLI) | **v2.0 audit (since v1.7.3)**

Settings file at `/proj/foo.refitter` containing `"openApiPath": "spec.yaml"` (relative to the .refitter location, since the generator resolves it that way internally). The CLI calls `OpenApiValidator.Validate(refitGeneratorSettings.OpenApiPath)` with `"spec.yaml"`, which is interpreted relative to the *current working directory*, not the `.refitter` directory. Validator fails with file-not-found in any directory other than `/proj`.

### Code
- `src/Refitter/GenerateCommand.cs:107-109`

### Fix
Resolve relative paths relative to the settings-file directory (mirror what `RefitGenerator` and the source generator do); share that resolution helper.

---

### [M5] InlineJsonConverters semantics silently changed: per-property → per-type (custom JsonNamingPolicy regression risk)

**GitHub:** https://github.com/christianhelle/refitter/issues/1032

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

Users keeping the default (`true`) get materially different generated code: `[JsonConverter(typeof(JsonStringEnumConverter))]` now sits on the enum **type** instead of each property.

### Risks
- A user who replaced converters via `JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower))` previously won. Now the type-level attribute may win for `JsonSerializer.Serialize/Deserialize` unless explicitly overridden, which can subtly flip behavior for users with custom enum naming policies (issue #178).
- Diff churn — every regenerated client changes.

### Code
- `src/Refitter.Core/Settings/CodeGeneratorSettings.cs:266-282`
- `src/Refitter.Core/RefitGenerator.cs:264-283`

### Fix
- Verify behavior: with `[JsonConverter]` on the type, does a user-supplied `Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower))` still take precedence? If not, this is a real regression for custom-naming-policy users.
- Document in CHANGELOG/migration guide.
- Consider opt-out value (`InlineJsonConverters = "Property" | "Type" | "None"`) to preserve old behavior.

Related: C4, M6.

---

### [M6] Hard-coded \n in regex replacement → mixed CRLF/LF on Windows

**GitHub:** https://github.com/christianhelle/refitter/issues/1033

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

```csharp
"$1[System.Text.Json.Serialization.JsonConverter(...))]\n$1$2"
```

NSwag emits `\r\n` on Windows. Mixing produces files that show as "modified" in many diff tools, fail consistent-newline checks (some lint configs / CRLF-only commit hooks), and confuse downstream tooling (.editorconfig enforcement, Source Link).

### Code
- `src/Refitter.Core/RefitGenerator.cs:275`

### Fix
Use `Environment.NewLine` (or detect the document's predominant newline once and reuse it).

Related: C4.

---

### [M7] Merge mutates input documents[0] and silently drops conflicting paths/schemas without warning

**GitHub:** https://github.com/christianhelle/refitter/issues/1034

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

Two related concerns in `OpenApiDocumentFactory.Merge`:

1. `baseDocument` (i.e. `documents[0]`) is mutated: callers who hold a reference to the first parsed `OpenApiDocument` now see paths/schemas/tags from later docs grafted in, with no copy.
2. Path-key and schema-key collisions are resolved by "first one wins" with **no logging or diagnostic**. Two specs sharing the same `/users/{id}` path with different shapes silently discard the second — users notice only at runtime.

### Code
- `src/Refitter.Core/OpenApiDocumentFactory.cs:44-95`

### Fix
Either (a) build a fresh `OpenApiDocument` for the merge result, or (b) at minimum emit a warning via the existing diagnostics channel when collisions are dropped.

Related: C6.

---

### [M8] XML doc emission does not escape user-supplied parameter / dynamic-querystring descriptions

**GitHub:** https://github.com/christianhelle/refitter/issues/1035

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`parameter.Description` and similar strings are passed straight to `AppendXmlCommentBlock`, bypassing `EscapeSymbols`. OpenAPI parameter descriptions containing `&`, `<`, `>` (e.g. `"filter by <status>"`) emit malformed XML; `dotnet build` reports CS1570 ("XML comment has badly formed XML"); Roslyn drops the whole comment.

### Code
- `src/Refitter.Core/XmlDocumentationGenerator.cs:127-139`
- `src/Refitter.Core/ParameterExtractor.cs:510-536`

### Fix
Pipe every externally-sourced string through `EscapeSymbols` before appending. Apply the same to `attributes` dictionary values in `AppendXmlCommentBlock` (an attribute whose value contains `"` corrupts the tag).

---

### [M9] ReOrderNullableParameters mis-classifies generic parameters that contain ?

**GitHub:** https://github.com/christianhelle/refitter/issues/1036

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`c.Contains("?")` is used both to sort and to decide whether to append `= default`. A required parameter with a generic argument such as `IDictionary<string, int?> filters` gets reordered to the tail of the parameter list **and** rewritten to `IDictionary<string, int?> filters = default`, silently turning a required argument into an optional one and changing call-site overload resolution.

### Code
- `src/Refitter.Core/ParameterExtractor.cs:172-208`

### Fix
Match the trailing nullability marker only, e.g. `Regex.IsMatch(c, @"\?\s+\w+(\s*=\s*[^,]+)?$")`, or carry the `IsNullable`/`IsOptional` flags through with the parameter rather than re-deriving from the rendered string.

---

### [M10] RefitInterfaceImports.GenerateNamespaceImports throws on empty namespace list

**GitHub:** https://github.com/christianhelle/refitter/issues/1037

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`ExcludeNamespaces` configured with `.*` (or three regexes that cover the three defaults) and no contracts namespace addition → `GetImportedNamespaces` returns empty, `Aggregate((a,b) => …)` throws `InvalidOperationException: Sequence contains no elements`. Crashes the generator with a non-actionable error.

### Code
- `src/Refitter.Core/RefitInterfaceImports.cs:59-62`

### Fix
Use `string.Join(Environment.NewLine, namespaces.Select(ns => $"using {ns};"))` (returns `""` for empty), or guard with `if (namespaces.Length == 0) return string.Empty;`.

---

### [M11] CustomCSharpTypeResolver appends ? to mapped reference-type aliases regardless of nullable-reference-type setting

**GitHub:** https://github.com/christianhelle/refitter/issues/1038

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

A user maps `format=guid → System.Guid` (value type — fine), but also maps `format=uri → System.Uri`. With `GenerateNullableReferenceTypes=false` (the default in many projects) and `isNullable=true`, the resolver emits `System.Uri?`, producing CS8632. Conversely with NRT enabled and a value-type mapping containing `Nullable<T>` written by the user, the `Contains("Nullable<")` check is fragile (matches `MyNullable<T>` substring).

### Code
- `src/Refitter.Core/CustomCSharpTypeResolver.cs:31-35`

### Fix
Consult `Settings.GenerateNullableReferenceTypes` and the CLR category of the mapped type before appending `?`. Replace substring check with a proper prefix/equality check (`StartsWith("System.Nullable<", StringComparison.Ordinal)` or `StartsWith("Nullable<", StringComparison.Ordinal)`).

---

### [M12] GetQueryParameters mutates the shared operationModel.Parameters collection

**GitHub:** https://github.com/christianhelle/refitter/issues/1039

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

`operationModel.Parameters.Remove(operationParameter);` runs while building the dynamic querystring wrapper. `operationModel` is the same NSwag model that `XmlDocumentationGenerator.AppendMethodDocumentation` later iterates over (`method.Parameters`), so any subsequent pass that depends on the original query parameters (XML docs, Apizr generator, multi-interface generator) sees them missing. Hidden global side effect inside what looks like a pure extractor.

### Code
- `src/Refitter.Core/ParameterExtractor.cs:465`

### Fix
Build a local list of "params still to emit" instead of mutating the model; or snapshot/restore the original collection.

---

### [M13] Static HttpClient has no timeout, no cancellation, no User-Agent

**GitHub:** https://github.com/christianhelle/refitter/issues/1040

**Severity:** Medium | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

```csharp
private static readonly HttpClient HttpClient = new(
    new HttpClientHandler { AutomaticDecompression = ... });
```
Uses default 100s timeout and no `User-Agent`. When invoked from MSBuild / source generators / CI pipelines, moving to a static singleton means all callers in the same process share a single timeout & handler. There is also no support for cancellation tokens being plumbed through `CreateAsync`.

### Code
- `src/Refitter.Core/OpenApiDocumentFactory.cs:14-18, 175-176`

### Fix
- Set explicit, configurable `Timeout` (or accept `CancellationToken` overload through `OpenApiDocumentFactory.CreateAsync`).
- Consider `SocketsHttpHandler` with `PooledConnectionLifetime` set so DNS rotation works in long-running hosts.
- Set a `User-Agent: refitter/<version>`.

---

### [M14] MSBuild task: dotnet --list-runtimes invocation is fragile (NRE, unquoted args, no fallback when target TFM dll missing)

**GitHub:** https://github.com/christianhelle/refitter/issues/1041

**Severity:** Medium | **Component:** Refitter.MSBuild | **v2.0 audit (since v1.7.3)**

### Failure modes
1. **Path injection / unquoted args**: A repository at `C:\Users\Foo "Bar"\proj\` (or any `.refitter` file with a `"` in its name on Linux) breaks the unescaped `--settings-file "{file}"` argument.
2. **Missing TFM folder**: Logic prefers `net10.0` if any `Microsoft.NETCore.App 10.*` runtime is installed. The Refitter.MSBuild package only ships TFM folders that exist at pack time. If the package was packed without `net10.0` but the developer has the .NET 10 runtime, the task points at a non-existent `…/net10.0/refitter.dll`. Combined with C2, the build silently produces nothing.
3. **NRE**: `output?.Split(...)` followed by `installedRuntimes.AddRange(<null>)` throws `ArgumentNullException` if `dotnet --list-runtimes` writes nothing (misconfigured `dotnet`, MOTD prompt, antivirus quarantine).

### Code
- `src/Refitter.MSBuild/RefitterGenerateTask.cs:73-101, 135-156`

### Fix
- Verify `File.Exists(refitterDll)` and fall back to lower TFMs.
- Wrap arguments via list-based ProcessStartInfo (or quote-escape).
- Null-check `dotnet --list-runtimes` output.
- Add `process.WaitForExit(15_000)` and `Kill(entireProcessTree: true)` on timeout.

---

### [M15] Spectre.Console.Cli 0.53 → 0.55 pre-1.0 minor bump may shift CLI parsing

**GitHub:** https://github.com/christianhelle/refitter/issues/1042

**Severity:** Medium | **Component:** Refitter (CLI) | **v2.0 audit (since v1.7.3)**

Spectre.Console.Cli pre-1.0 has historically changed enum/flag binding semantics between minor releases. Combined with M16 (bool → enum on `--generate-authentication-header`), behavior of `--flag`-style options could shift in subtle ways.

### Code
- `src/Refitter/Refitter.csproj:24`

### Fix
Smoke-test all CLI examples in `src/Refitter/README.md` and `samples/` against the new build.

---

### [M16] CLI --generate-authentication-header changed from bool flag to enum value (silent script breakage)

**GitHub:** https://github.com/christianhelle/refitter/issues/1043

**Severity:** Medium | **Component:** Refitter (CLI) | **v2.0 audit (since v1.7.3)**

Existing CI scripts invoking `refitter ... --generate-authentication-header` (Spectre treated this as a bool flag) or `--generate-authentication-header true` will now fail with a Spectre parse error, since the option binds to `AuthenticationHeaderStyle` (`None | Method | Parameter`). The flag *name* is preserved, which makes the breakage non-obvious.

### Code
- `src/Refitter/Settings.cs:288-291`
- `src/Refitter/GenerateCommand.cs:335`

### Fix
- Add a custom `TypeConverter` mapping `true → Parameter`, `false → None` for backward compatibility, OR
- Introduce a new option name (e.g. `--authentication-header-style`) and keep `--generate-authentication-header [true|false]` working with a deprecation message.
- Document in CHANGELOG / migration guide.

Related to known intentional rename of the `.refitter` JSON key (audit epic).

---

### [L1] OpenApiPath + OpenApiPaths precedence is silent; no validation

**GitHub:** https://github.com/christianhelle/refitter/issues/1044

**Severity:** Low | **Component:** Refitter.Core / CLI | **v2.0 audit (since v1.7.3)**

A user setting both `openApiPath` and `openApiPaths` (common when copy-pasting from samples) silently uses `openApiPaths` and ignores `openApiPath`. SettingsValidator additionally mutates `OpenApiPath = OpenApiPaths[0]`, which is surprising.

### Code
- `src/Refitter.Core/Settings/RefitGeneratorSettings.cs:30-35`
- `src/Refitter.Core/RefitGenerator.cs:45-46`
- `src/Refitter/SettingsValidator.cs:47-49`

### Fix
Validate "only one of `OpenApiPath` / `OpenApiPaths` may be set" in `SettingsValidator`, OR emit a warning when both are present. Document precedence in the JSON schema.

---

### [L2] OpenApiPath remains null! when OpenApiPaths is set → NRE for library consumers

**GitHub:** https://github.com/christianhelle/refitter/issues/1045

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

When `OpenApiPaths` is provided and `OpenApiPath` is omitted, code paths in `Refitter.Core` (e.g. when consumers call `RefitGenerator.CreateAsync(settings)` directly without going through `SettingsValidator`) may dereference `OpenApiPath`. SettingsValidator mitigates this for the CLI, but library consumers (per `Refitter.Core` README example showing direct `OpenApiPaths` use) will hit NRE.

### Code
- `src/Refitter.Core/Settings/RefitGeneratorSettings.cs:28`

### Fix
In `RefitGenerator.CreateAsync`, normalize `OpenApiPath = OpenApiPaths[0]` when `OpenApiPaths` is non-empty, so library consumers get the same guarantee as the CLI.

---

### [L3] Default OpenApiPaths = Array.Empty<string>() round-trips into saved settings as "openApiPaths": []

**GitHub:** https://github.com/christianhelle/refitter/issues/1046

**Severity:** Low (cosmetic) | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

Any tool calling `Serializer.Serialize(settings)` (e.g. `refitter --output-settings-file`) emits `"openApiPaths": []` for every user. Newcomers will assume the field is required.

### Code
- `src/Refitter.Core/Settings/RefitGeneratorSettings.cs:35`

### Fix
Change default to `null` and add `[JsonIgnore(Condition = WhenWritingNull)]`, or make the property `string[]?`.

---

### [L4] MSBuild task does regex-based JSON parsing of .refitter files

**GitHub:** https://github.com/christianhelle/refitter/issues/1047

**Severity:** Low | **Component:** Refitter.MSBuild | **v2.0 audit (since v1.7.3)**

A `.refitter` file with `"outputFolder": "C:\\folder\\\"with quote"` or a string containing the substring `"outputFolder"` inside another value: regex extraction fails or returns wrong data, leading to predicted-paths drift (H6).

### Code
- `src/Refitter.MSBuild/RefitterGenerateTask.cs:267-295`

### Fix
Use the same JSON deserializer the generator uses (`Refitter.Core.Serializer`); the package already references `System.Text.Json` transitively.

Related: H6.

---

### [L5] CLI reads .refitter twice (validator + execute), risks drift

**GitHub:** https://github.com/christianhelle/refitter/issues/1048

**Severity:** Low | **Component:** Refitter (CLI) | **v2.0 audit (since v1.7.3)**

Two separate `File.ReadAllText` + `Serializer.Deserialize` calls per invocation. Wasteful and makes interpretations easier to drift (already happened: validator uses `OpenApiPaths[0]`; ExecuteAsync only uses `OpenApiPath`).

### Code
- `src/Refitter/SettingsValidator.cs:44`
- `src/Refitter/GenerateCommand.cs:47`

### Fix
Read once and pass the deserialized object through (e.g., stash on `Settings` or via DI).

---

### [L6] Library code missing ConfigureAwait(false) — sync-over-async deadlock risk for hosted callers

**GitHub:** https://github.com/christianhelle/refitter/issues/1049

**Severity:** Low (pre-existing, expanded surface) | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

New awaits (`SerializeAsJsonAsync`, `SerializeAsYamlAsync`, the new `CreateAsync(IEnumerable<string>)` loop) all run without `ConfigureAwait(false)`. Refitter is consumed not just as a CLI but also as a library and a source generator; a WPF/WinForms host calling `.Result`/`.Wait()` on `RefitGenerator.Generate()` (which currently happens in some MSBuild hosts) can deadlock.

### Code
- `src/Refitter.Core/OpenApiDocumentFactory.cs` (multiple await points)
- `src/Refitter.Core/RefitGenerator.cs:42-47`

### Fix
Add `.ConfigureAwait(false)` to every await in `Refitter.Core` (this is library code).

---

### [L7] Enum-deserialization errors in .refitter give unhelpful JsonException

**GitHub:** https://github.com/christianhelle/refitter/issues/1050

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

A typo such as `"propertyNamingPolicy": "preserve-original"` or `"authenticationHeaderStyle": "header"` throws a generic `System.Text.Json.JsonException`. Users won't know which property failed or what the valid values are.

### Code
- `src/Refitter.Core/Serializer.cs`
- `src/Refitter.Core/Settings/PropertyNamingPolicy.cs`
- `src/Refitter.Core/Settings/AuthenticationHeaderStyle.cs`

### Fix
Catch `JsonException` in `Serializer.Deserialize` and re-throw with the property path and a list of valid enum values; OR implement validation in `SettingsValidator` that lints enum properties producing actionable errors. Document accepted casings.

---

### [L8] XmlDocumentationGenerator.DecodeJsonEscapedText mishandles malformed \u sequences

**GitHub:** https://github.com/christianhelle/refitter/issues/1051

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

When `escapedCharacter == 'u'` but `int.TryParse` fails (non-hex chars after `\u`, e.g. `\uZZZZ`), the code falls through to the simple-char `switch`, treats `'u'` as unknown, emits `\u`, but does **not** advance past the four following characters. The next loop iteration re-reads the malformed hex and possibly mis-decodes (`\uZZ\n`).

### Code
- `src/Refitter.Core/XmlDocumentationGenerator.cs:400-413`

### Fix
When the hex parse fails, append the raw `\uXXXX` text *and* advance index by 4 (or fall back to writing the original substring). Use `index + 5 <= input.Length` for clearer bounds.

---

### [L9] OperationNameGenerator.CheckForDuplicateOperationIds runs full pipeline twice in constructor

**GitHub:** https://github.com/christianhelle/refitter/issues/1052

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

When the user does not specify a strategy, the constructor instantiates `MultipleClientsFromOperationIdOperationNameGenerator`, runs `GetOperationName` over every operation in the document just to detect duplicates, then **replaces** `defaultGenerator`. Doubles the cost on large specs, hides any exception thrown by NSwag's generator inside object construction, and `Distinct().Count() != operationNames.Count` allocates twice.

### Code
- `src/Refitter.Core/OperationNameGenerator.cs:44-95`

### Fix
Compare HashSet membership in a single pass and short-circuit on first duplicate; consider deferring detection until first call.

---

### [L10] IdentifierUtils.ReservedKeywords incomplete; Sanitize() does not escape keywords

**GitHub:** https://github.com/christianhelle/refitter/issues/1053

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

- Missing real reserved keywords: `__arglist`, `__makeref`, `__reftype`, `__refvalue` (rare but real CS1041 sources).
- Contextual keywords aren't included — `record`, `nameof`, `value`, `var`, `dynamic`, `init`, `required`, `file`, `scoped`, `with`, `notnull`, `unmanaged`. `record` as a type name is allowed only in some positions; `file` as a class modifier is reserved at type position in C# 11+.
- `Sanitize()` (used by `OperationNameGenerator` and `SanitizeControllerTag`) does **not** call `EscapeReservedKeyword`. Today this is masked because callers always `CapitalizeFirstCharacter()`, but anything calling `Sanitize()` for a lowercase identifier can still emit a keyword.

### Code
- `src/Refitter.Core/IdentifierUtils.cs:8-87, 124-142`

### Fix
Add the underscore keywords; have `Sanitize()` route through `EscapeReservedKeyword` for safety; consider a small `ReservedTypeKeywords` set (`record`, `file`).

Related: H2, H3.

---

### [L11] OpenApiDocumentFactory.CreateAsync(IEnumerable<string>) throws wrong exception type on null

**GitHub:** https://github.com/christianhelle/refitter/issues/1054

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

```csharp
var paths = openApiPaths?.ToArray()
    ?? throw new ArgumentException("At least one OpenAPI path must be specified.", nameof(openApiPaths));
```

When `openApiPaths` is `null` the type expected by the public API is `ArgumentNullException`, not `ArgumentException`. Callers trying to catch null arguments specifically will miss it.

### Fix
Throw `ArgumentNullException` for `null`, keep `ArgumentException` for empty.

---

### [L12] Reordering of interface-generator construction in Generate() is correct but fragile

**GitHub:** https://github.com/christianhelle/refitter/issues/1055

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

The diff comment correctly explains that `interfaceGenerator` must be instantiated *before* `generator.GenerateFile()` so that `CheckForDuplicateOperationIds` sees the original operation IDs (NSwag mutates them during `GenerateFile`). This is now done in two places (`Generate` and `GenerateMultipleFiles`). Anybody adding a third entry point will easily reintroduce the bug.

### Code
- `src/Refitter.Core/RefitGenerator.cs:136-149, 196-215`

### Fix
Encapsulate the "create interface generator + then call GenerateFile" sequence in a single private helper, or capture the original operation IDs in the interface generator's constructor so generation order no longer matters.

---

### [L13] IdentifierUtils.Counted uses fresh HashSet per GenerateCode() call

**GitHub:** https://github.com/christianhelle/refitter/issues/1056

**Severity:** Low | **Component:** Refitter.Core | **v2.0 audit (since v1.7.3)**

The `HashSet` for de-dup is local to one call to `GenerateCode()`. If `RefitGenerator` invokes the interface generator more than once, the set is fresh each time. Currently fine for single-interface case, but duplicate-name detection no longer cooperates with `OperationNameGenerator.CheckForDuplicateOperationIds` if call ordering changes.

### Code
- `src/Refitter.Core/RefitInterfaceGenerator.cs:50, 64-65`

### Fix
Document the invariant or move the set onto the generator instance.

---
