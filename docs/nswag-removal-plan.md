# Plan: Removing the NSwag Dependency

Status: **All phases done. NSwag, NJsonSchema and Newtonsoft.Json are no longer dependencies** (branch `feature/nswag-removal-feasibility-874777`). See "Outcome" below.

## Goal

Remove `NSwag.CodeGeneration.CSharp` and `NSwag.Core.Yaml` (and with them the transitive
`NJsonSchema*` packages) from `Refitter.Core` without dropping a user-facing feature.

"Without dropping a feature" is defined mechanically: **every golden snapshot produced by
the Phase 0 harness must be reproduced by the NSwag-free pipeline**, apart from differences
that are explicitly accepted and listed in the "Accepted differences" section of this
document.

## Outcome

The plan was carried out differently from the phases below in two ways:

- **No `Microsoft.OpenApi` model and no feature flag.** Byte-identical output is far easier to reach by
  reproducing NSwag's own semantics than by mapping a different model onto them. So Refitter.Core now has
  its own OpenAPI model and reader (`src/Refitter.Core/OpenApi`) that behave like NSwag and NJsonSchema. That
  covers keyword handling per spec version, reference resolution including external files, and the JSON
  round trip used to copy documents. The contract generator (`src/Refitter.Core/CodeGeneration/Contracts`)
  ports NJsonSchema's and NSwag's C# generation and renders their Liquid templates with Fluid. Code and
  templates adapted from NJsonSchema and NSwag (MIT) are attributed in `THIRD-PARTY-NOTICES.md`. Because the
  output is identical, the pipeline was switched without a flag.
- **Differential tests instead of a debug comparer.** While both implementations existed, temporary tests
  compared the native document model, document copies and contracts with NSwag's for every parity spec and
  setting. All matched, and the tests were removed together with NSwag.

Verification:

- Every golden snapshot (19,000+ cases: every spec in `test/OpenAPI` plus every scenario-test spec, times
  about 70 settings variants) is reproduced byte for byte.
- The CLI built from the last NSwag-based commit and the NSwag-free CLI produce identical output for every
  spec in `test/OpenAPI` and for the remote specs used by the tests, with 31 flag combinations (1,271 runs).
- The unit, integration and source generator tests pass.

Behavior changes:

- The obsolete NSwag-typed APIs were removed: `OpenApiDocumentFactory`, `RefitDocumentFilter`,
  `SchemaCleaner`, the `RefitGenerator(settings, OpenApiDocument)` constructor, `RefitGenerator.OpenApiDocument`,
  `CodeGeneratorSettings.PropertyNameGenerator` and `RefitGeneratorSettings.ParameterNameGenerator`.
  `PublicApiNSwagIndependenceTests` now guards the whole public API.
- OpenAPI 3 documents that reference responses outside `components` (e.g. `#/responses/errors`, as in
  https://developers.intellihr.io/docs/v1/swagger.json) could be loaded but not generated with NSwag,
  because the references inside those responses were never resolved. They are now resolved.

Still open:

- `CustomTemplateDirectory` still works, because the ported templates are rendered with template models
  equivalent to NJsonSchema's. It is still marked deprecated; keep it and remove the deprecation, or remove it.
- The generated code still says `GeneratedCode("NJsonSchema"/"NSwag", "14.7.1.0 ...")` and "Generated using the
  NSwag toolchain" (`ContractTemplateRenderer.ToolchainVersion` and the templates), to keep the output identical.
  Changing it is a one-line, explicitly accepted snapshot difference.

## Where NSwag was used

About 40 files in `Refitter.Core` import NSwag or NJsonSchema. The usage falls into four layers.

| Layer | What NSwag does | Main files | Replacement |
|---|---|---|---|
| 1. Document model | Parses Swagger 2.0 / OpenAPI 3.x into `NSwag.OpenApiDocument` + `NJsonSchema.JsonSchema`, resolves `$ref` (incl. external files), upgrades Swagger 2 to one model | `Document/*`, `Schema/*`, `Mutators/*`, `Partitioning/*`, `RefitGenerator` | `Microsoft.OpenApi` (already referenced through OasReader) behind a Refitter-owned model |
| 2. Operation model | `CSharpOperationModel`: parameter/response type resolution, nullability, parameter naming | `Generation/*`, `ParameterExtraction/*`, `CustomCSharpClientGenerator.CreateOperationModel` | Refitter `OperationModel` built from the Refitter schema model |
| 3. Contract generation | NJsonSchema C# generator + Liquid templates emit DTOs, enums, inheritance, converters | `Pipeline/CSharpClientGeneratorFactory`, `GeneratorPipeline` (`generator.GenerateFile()`), `CodeGeneration/*` | Refitter contract emitter (Roslyn `SyntaxFactory` or string builder) |
| 4. Public API | NSwag types exposed on Refitter's public surface | see below | Refitter-owned abstractions, NSwag ones obsoleted |

Public API that exposes NSwag/NJsonSchema types:

- `CodeGeneratorSettings.PropertyNameGenerator` (`NJsonSchema.CodeGeneration.IPropertyNameGenerator`)
- `RefitGeneratorSettings.ParameterNameGenerator` (`NSwag.CodeGeneration.IParameterNameGenerator`)
- `RefitGenerator.OpenApiDocument` and `OpenApiDocumentFactory.CreateAsync` (`NSwag.OpenApiDocument`)
- `CustomTemplateDirectory` (users' Liquid templates bind to NJsonSchema template models)
- `OperationNameGeneratorTypes` is Refitter's own enum, but its values select NSwag
  `IOperationNameGenerator` implementations, so their behavior must be reimplemented.

Workarounds that exist only because of NSwag, and which should disappear or shrink:
`NumericBoundsSanitizer`, `PathItemReferenceInliner`, the OasReader→NSwag round-trip in
`OpenApiReaderDocumentStrategy`, `SchemaCleaner`, the five document mutators,
`Swagger2OptionalReferenceNullabilityNormalizer`, `EnumStringConverterInjector`,
the regex-based `ContractTypeSuffixApplier`, `ObsoleteContractAttributeRemover`.

## Phases

Each phase ends in a releasable state. NSwag stays the default until Phase 5.

### Phase 0 — Golden-output parity harness (no production changes)

Freeze today's behavior so later phases can prove they preserve it.

1. Add a `Parity` test suite in `Refitter.Tests` that, for every spec in `test/OpenAPI`
   (v2.0, v3.0, v3.1; JSON and YAML), and for a matrix of settings variants that mirrors
   `test/smoke-tests.ps1`, generates code and compares it to a checked-in snapshot.
2. Snapshots live under `src/Refitter.Tests/Parity/Snapshots/<spec>/<variant>.cs`.
   Volatile content (Refitter and NJsonSchema version numbers) is normalized before compare.
3. Setting `REFITTER_UPDATE_SNAPSHOTS=1` rewrites the snapshots instead of asserting.
4. Snapshot tests are tagged `[Category("Parity")]` so they can be run on their own.
5. Extend the matrix to cover `CodeGeneratorSettings` options (date/array/dictionary types,
   nullable reference types, data annotations, `ExcludedTypeNames`, …) on a feature-rich spec.

Exit: harness green on `main`, snapshots committed.

**Result (done):** `src/Refitter.Tests/Parity` — 1,412 cases (34 specs × 44 variants; specs over
500 KB only run `Default` and `NullCodeGeneratorSettings`), ~15 s. A variant only gets a snapshot
file when its output differs from that spec's `Default`, which keeps the corpus at 808 files
(19 MB on disk, under 1 MB compressed). Generation is deterministic across repeated runs.

```bash
# verify
dotnet test --project src/Refitter.Tests/Refitter.Tests.csproj -c Release --no-build --treenode-filter "/*/*/*/*[Category=Parity]"
# accept new output (review the git diff afterwards)
REFITTER_UPDATE_SNAPSHOTS=1 dotnet test --project src/Refitter.Tests/Refitter.Tests.csproj -c Release --no-build --treenode-filter "/*/*/*/*[Category=Parity]"
```

Findings:

- `v3.1/non-oauth-scopes.*` cannot be loaded today (an operation has no `responses`); it is excluded,
  as in `test/smoke-tests.ps1`. The native loader may accept it — that would be a feature gain.
- JSON and YAML variants of the same spec are *not* interchangeable in the corpus (e.g. the v2.0
  petstore YAML has an extra `uploadImage` operation), so both are snapshotted independently.
- Variants that never change output for small specs (e.g. `InlineNamedTypes`,
  `NullCodeGeneratorSettings` in most specs) still run; they guard against the native generator
  starting to react to settings it previously ignored.

### Phase 1 — Refitter-owned public extension points (minor release)

1. Introduce `Refitter.Core.IPropertyNameGenerator` / `IParameterNameGenerator` with
   Refitter-owned inputs (`PropertyNameContext` with name, schema info, parent type name).
2. Adapt them to NSwag internally; mark the NSwag-typed settings `[Obsolete]`.
3. Add `RefitGenerator.Document` returning a Refitter read-only view; obsolete
   `RefitGenerator.OpenApiDocument`.
4. Document the `CustomTemplateDirectory` deprecation path (see Accepted differences).

Exit: no NSwag type is needed to use any non-obsolete API.

**Result (done):**

| Old (obsolete, removed in the next major) | Replacement |
|---|---|
| `CodeGeneratorSettings.PropertyNameGenerator` (`NJsonSchema…IPropertyNameGenerator`) | `CodeGeneratorSettings.PropertyNameProvider` (`IPropertyNameProvider`, `PropertyNameContext`) |
| `RefitGeneratorSettings.ParameterNameGenerator` (`NSwag…IParameterNameGenerator`) | `RefitGeneratorSettings.ParameterNameProvider` (`IParameterNameProvider`, `ParameterNameContext`, `ParameterSource`) |
| `RefitGenerator.OpenApiDocument` | `RefitGenerator.DocumentInfo` (`ApiDocumentInfo`: title, version, paths, schema names) |
| `new RefitGenerator(settings, NSwag.OpenApiDocument)` | `RefitGenerator.CreateAsync` |
| `OpenApiDocumentFactory`, `RefitDocumentFilter`, `SchemaCleaner` | `RefitGenerator.CreateAsync` with `IncludeTags`, `IncludePathMatches`, `TrimUnusedSchema` |
| `CustomTemplateDirectory` (settings, `.refitter`, `--custom-template-directory`) | None; see Accepted differences |

- New providers take precedence over the obsolete generators when both are set. Provided property
  names still go through the collision de-duplication added for #1268.
- Implementations moved to internal `NSwagDocumentFactory`, `NSwagDocumentFilter` and
  `NSwagSchemaCleaner`; the public names are thin `[Obsolete]` facades.
- `PublicApiNSwagIndependenceTests` fails if any non-obsolete public member exposes an NSwag or
  NJsonSchema type. It caught the public `RefitGenerator` constructor.
- Using `customTemplateDirectory` now produces a "Deprecated Setting" warning (CLI, MSBuild), like `usePolly`.

Findings:

- `codeGeneratorSettings.customTemplateDirectory` was never read: it silently had no effect.
  It now warns that it has no effect.
- `XmlDocumentationGenerator` was public but had only an internal constructor and no public way to
  obtain an instance, so it was made internal instead of getting a facade.
- Adding `ParameterNameProvider` to the public `ICodeGenerationConfiguration` interface breaks
  anyone who implements it outside Refitter (netstandard2.0 has no default interface members).
  This is unlikely but belongs in the release notes.
- The source generator only surfaces the "Date Format Override" warning, so it does not show the
  `usePolly` or `customTemplateDirectory` deprecation warnings either.

### Phase 2 — Refitter schema/document model

1. Define an internal immutable model: `ApiDocument`, `ApiOperation`, `ApiParameter`,
   `ApiResponse`, `ApiSchema` (type, format, nullability, enum values, properties,
   required, allOf/oneOf/anyOf, discriminator, additionalProperties, x-* extensions).
2. Write a loader from `Microsoft.OpenApi` (v2 reader handles 2.0/3.0/3.1 and external refs).
3. Port filtering (`RefitDocumentFilter`), cleaning (`SchemaCleaner`), merging
   (`DocumentMerger`), and the mutators to the new model. Most mutators become loader rules.
4. Temporarily build the NSwag document **from** the new model is *not* planned; instead the
   two loaders run side by side and a debug-only comparer checks that both models agree on
   schema names, operation ids and parameter lists for every spec in `test/OpenAPI`.

Exit: new model loads every spec in `test/OpenAPI` and every scenario test spec.

### Phase 3 — Interface generation on the new model

1. Port `ParameterExtraction/*`, `ReturnTypeGenerator`, `MethodAttributeGenerator`,
   `XmlDocumentationGenerator`, partitioning and operation naming off `CSharpOperationModel`.
2. Reimplement the three `OperationNameGeneratorTypes` strategies.
3. Type-name resolution: port `SafeSchemaTypeNameGenerator`/`UniqueEnumNameGenerator` rules
   so interface parameter types match the names contracts get.

Exit: interface portion of every golden snapshot is byte-identical.

### Phase 4 — Contract emitter (behind `RefitGeneratorSettings.UseNativeContractGenerator`)

Order of work, each step gated by snapshots:

1. Plain classes: properties, `JsonPropertyName`, XML docs, required/nullable, defaults.
2. Enums: string/integer enums, `x-enumNames`, `JsonStringEnumConverter`, flag enums.
3. Collections and dictionaries: `ArrayType`, `DictionaryType`, instance/base types, immutability.
4. `AdditionalProperties` dictionary.
5. Inheritance: allOf, discriminators, `JsonInheritanceConverter` and STJ polymorphism.
6. Records / immutable records, `PropertySetterAccessModifier`, `TypeAccessibility`.
7. Data annotations, date/time converters, `JsonConverters`, `GenerateJsonMethods`.
8. Inline named any/tuple/array/dictionary, `ExcludedTypeNames`, `ContractTypeSuffix` as a
   naming rule instead of a regex rewrite.
9. The JSON serializer context generator reads the new type table instead of NJsonSchema's resolver.

Exit: full parity suite green with the flag on; all scenario, source-generator and smoke tests green.

### Phase 5 — Flip the default (major release)

1. Native generator becomes default; NSwag path remains behind a flag for one major version.
2. Remove obsolete NSwag-typed APIs; remove `CustomTemplateDirectory` or replace it.

### Phase 6 — Delete NSwag

Remove the packages, the flag, the NSwag code path and the workarounds listed above.

## Accepted differences

Confirmed by the maintainer:

- `CustomTemplateDirectory` is dropped in the major release: NJsonSchema Liquid templates cannot be
  honored without re-implementing NJsonSchema's template models. Deprecated in Phase 1.
- NSwag-typed APIs are kept as `[Obsolete]` until the next major version, which removes them.

Still to confirm:

- `[GeneratedCode("NJsonSchema", "x.y.z")]` on contracts becomes `[GeneratedCode("Refitter", …)]`.

## Risks

| Risk | Mitigation |
|---|---|
| Hidden NJsonSchema edge cases not covered by specs in `test/OpenAPI` | Add regression specs from past issues to the parity corpus; run the harness against scenario-test specs too |
| Snapshots are large and noisy in diffs | Keep matrix focused; only snapshot variants that change output |
| Two code paths during Phases 2–5 double maintenance | Keep the flag window short; bug fixes land in the native path first |
| Swagger 2.0 semantics differ between NSwag and Microsoft.OpenApi (e.g. optional `$ref` nullability) | Parity harness covers both v2.0 JSON and YAML |
