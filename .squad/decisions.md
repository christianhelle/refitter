# Squad Decisions

## 2026-04-21

### PR #1067 Linked-Issue Closure Matrix

**Lead:** Ripley  
**Status:** REVIEWED

- Treat **#1017, #1022, #1023, #1024, and #1026** as fully closed on the reviewed branch state.
- Treat **#1025** as **partial/documentation-first only**; do not auto-close it from PR wording.
- Final review guidance requires removing or downgrading `Fixes #1025` in the PR body so GitHub does not overstate closure.

### Documentation and Package Guidance Alignment

**Verified By:** Bishop / Ash  
**Status:** REQUIRED AND VERIFIED

- `Refitter.SourceGenerator` package guidance must describe Roslyn `AddSource()` behavior rather than legacy disk-file output.
- Consumer guidance must explicitly require a direct `Refit` reference (and `Refit.HttpClientFactory` when generated DI helpers are used).
- Disk-output settings (`outputFolder`, `contractsOutputFolder`, `generateMultipleFiles`) should be documented as CLI/MSBuild-oriented, not source-generator disk artifacts.
- Final safety-lane review approved PR #1067 once issue-closure wording was honest and the packaging/docs/test evidence aligned.

### Session Directives Archived

**By:** Christian Helle (via Copilot)

- 2026-04-20: Commit changes as often as possible in small logical groups.
- 2026-04-21: Use Opus for all agents for the rest of that session only.
- 2026-04-21: Commit changes in small logical groups.
- 2026-04-25: Use GPT-5.5 for all agents for the rest of this session only.

## 2026-04-25

### Remaining Audit Matrix Pass (#1057)

**Verified By:** Ripley  
**Status:** VERIFIED

- Treat **#1042** as **validation-only** until a concrete Spectre.Console.Cli parsing regression is reproduced at current HEAD.
- Treat **#1047** as **already fixed / stale issue text** at current HEAD because MSBuild now consumes CLI-emitted `GeneratedFile:` markers instead of regex-parsing `.refitter` JSON for output paths.
- Treat **#1056** as **doc/invariant-only** for now; preserve the current generation ordering/state flow and document the invariant before changing behavior.
- Treat **#1032** as **validation-first**; gather runtime evidence before changing enum-converter behavior.
- Coordination note from the verification pass: **#1045 and #1047 appear already fixed at HEAD**, and the remaining code-backed follow-up stays with Dallas/Parker.

### Remaining Audit Repro Pass (#1057)

**Verified By:** Lambert (Tester)  
**Status:** EVIDENCE NARROWED

- Treat **#1028** as **still reproducible by inspection** on current HEAD; the source-generator incremental pipeline still carries a `List<Diagnostic>` equality hazard.
- Treat **#1029** as **partial** on current HEAD; visible diagnostics improved, but the "no .refitter files found" path is still only `Debug.WriteLine`.
- Treat **#1033** as **still reproducible by inspection**; enum-converter injection still uses a hard-coded LF and needs newline normalization coverage.
- Treat **#1041** as **partial**; runtime resolution improved, but argument escaping and timeout kill semantics still leave repro surface.
- Treat **#1043** as **still reproducible**; legacy `--generate-authentication-header` bool-style CLI usage still fails at current HEAD.
- Treat **#1032, #1042, #1045, and #1047** as **validation-only / fixed-at-HEAD evidence** unless stronger failing repros appear.
- Treat **#1034, #1039, and #1056** as **not reproduced on current HEAD** in Lambert's pass.

### Multi-spec Merge Collision Policy

**Decided By:** Parker (Core Developer)  
**Status:** APPROVED

- `OpenApiDocumentFactory` should clone the first loaded document before merging additional specs so callers do not observe mutation of a previously loaded `OpenApiDocument`.
- Path and schema-key collisions across distinct OpenAPI inputs should fail fast with `InvalidOperationException` instead of silently keeping the first definition.
- Exact duplicate input paths should continue to deduplicate up front so feeding the same spec twice stays harmless.

**Rationale:** Silent first-one-wins merge behavior hides real API-shape conflicts and only surfaces later during generation or runtime use. Failing fast is the safer core-library policy.

### Core Lane Follow-up Gate (#1057)

**Verified By:** Parker (Core Developer)  
**Status:** FIXED / NARROWED

- **#1033**: landed at HEAD with a code change in `src/Refitter.Core/RefitGenerator.cs` plus regression coverage in `src/Refitter.Tests/Examples/InlineJsonConvertersTests.cs`.
- **#1032**: treat as validation-first pending review gate; no current core-lane code change required.
- **#1034** and **#1039**: treat as fixed-at-HEAD / no-repro on the reviewed branch state pending final gate review.
- **#1045**: treat as fixed-at-HEAD on the reviewed branch state pending final gate review.
- **#1056**: treat as doc/invariant-only for now; preserve current ordering behavior unless new failing evidence appears.
### Tooling Compatibility Follow-up

**Verified By:** Dallas (Tooling Developer)  
**Status:** APPROVED

- Preserve CLI compatibility for `--generate-authentication-header` by treating the legacy boolean forms (`true`, `false`) and the bare flag as valid inputs. The bare flag and `true` now map to `AuthenticationHeaderStyle.Method`; `false` maps to `None`, while `Parameter` still requires the explicit enum value.
- Keep MSBuild runtime resolution resilient across both packed and test-project layouts. The task now prefers bundled framework-specific Refitter binaries, falls back to lower compatible TFMs when probing fails, and finally uses a co-located `refitter.dll` when the packaged layout is unavailable.

### PR Prep Closure Guidance

**Verified By:** Ripley  
**Status:** DRAFTED FOR PR ASSEMBLY

- Safe auto-close candidates on the reviewed branch state: **#1028**, **#1029**, **#1033**, and **#1043**.
- Keep **#1032**, **#1034**, **#1039**, **#1041**, **#1042**, **#1045**, **#1047**, and **#1056** out of PR auto-close wording until stronger evidence or final lane approval exists.
- **#1041** specifically remains a Dallas-owned tooling verdict before any PR body claims closure.

### Ash core review of remaining #1057 closures

**Verified By:** Ash  
**Status:** REJECT

- Verified acceptable:
  - **#1032** does not reproduce the claimed custom `JsonNamingPolicy` override regression at current HEAD; runtime repro with a type-level `JsonStringEnumConverter` still serialized via `JsonSerializerOptions.Converters` (`"my_value"`).
  - **#1045** is effectively fixed at current HEAD because `RefitGenerator.GetOpenApiDocument()` uses `OpenApiPaths` directly when populated instead of dereferencing `OpenApiPath`.
  - **#1033** is the only intentional core code change in the working tree (`src/Refitter.Core/RefitGenerator.cs`) and it has matching regression coverage in `src/Refitter.Tests/Examples/InlineJsonConvertersTests.cs`.
- Still open / false closure:
  - **#1034** remains open in `src/Refitter.Core/OpenApiDocumentFactory.cs:55-107`; `Merge()` still mutates `documents[0]` and still silently keeps the first path/schema on key collisions.
  - **#1039** remains open in `src/Refitter.Core/ParameterExtractor.cs:447-487` plus `src/Refitter.Core/RefitInterfaceGenerator.cs:69-82`; `GetParameters()` removes query parameters from `operationModel.Parameters` before XML-doc generation reads the shared model.
- Follow-up requirement: reassign the remaining core revisions to Parker (or another core implementer) for real fixes before closing **#1034**/**#1039** from the `#1057` matrix.

### Dallas core revision on rejected blockers

**Verified By:** Dallas  
**Status:** IMPLEMENTED / PENDING ASH RE-REVIEW

- **#1034:** `OpenApiDocumentFactory.Merge()` now clones the first input before merge so callers no longer observe mutation of a previously loaded `OpenApiDocument`.
- **#1034:** duplicate path/schema collisions now emit warnings while preserving the existing merged entry; this revised pass does **not** follow the earlier fail-fast proposal.
- **#1039:** `ParameterExtractor` no longer mutates the shared `operationModel.Parameters` collection when building grouped query-parameter wrappers, so downstream consumers keep the original operation model intact.
- Regression coverage was refreshed for the revised core pass in `src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs`, `src/Refitter.Tests/ParameterExtractorEdgeCaseTests.cs`, `src/Refitter.Tests/ParameterExtractorPrivateCoverageTests.cs`, and `src/Refitter.Tests/RegressionTests/Issue1039_DynamicQuerystringMutationTests.cs`.
- Dallas reported the revised core validation lane green; Ash is performing re-review and Lambert is reconciling the blocker-test lane against the landed behavior.

### Ash core re-review of Dallas revision

**Verified By:** Ash  
**Status:** PARTIAL / BLOCKED

- **#1039 resolved:** ParameterExtractor.GetParameters() no longer mutates operationModel.Parameters, and ParameterExtractorPrivateCoverageTests now lock that invariant for XML-doc generation and shared-model reuse.
- **#1034 still open:** OpenApiDocumentFactory.Merge() now clones the first input, but it still keeps the first conflicting path/schema/definition/security entry via Trace.TraceWarning(...) instead of failing fast.
- src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs still codifies warning-backed first-wins collision handling; the next narrow revision must flip that coverage to an InvalidOperationException contract for conflicting inputs.
- Do **not** close **#1034** from the #1057 matrix yet. Dallas owns one last narrow revision, and Lambert remains on the blocker-test lane.

### Dallas final #1034 revision / Lambert blocker-test reconciliation

**Verified By:** Dallas / Lambert  
**Status:** IMPLEMENTED / READY FOR ASH FINAL GATE

- `OpenApiDocumentFactory.Merge()` now preserves the clone-first non-mutation guarantee **and** fails fast with `InvalidOperationException` when distinct inputs introduce conflicting duplicate path, schema, definition, or security keys.
- Non-conflicting merges still return a new document without mutating either input document, and exact duplicate input paths remain harmless because they are deduplicated before merge.
- Blocker coverage is now aligned to the fail-fast contract in `src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs`; `Issue1039_DynamicQuerystringMutationTests.cs` still preserves grouped-query XML-doc assertions across single-interface, `MultipleInterfaces.ByTag`, and `MultipleInterfaces.ByEndpoint` generation.
- Dallas's final narrow implementation pass is complete. Ash is performing the final review gate, and Lambert is reconciling the blocker-test lane against the landed fail-fast behavior.

### Ash final core gate rejection

**Verified By:** Ash  
**Status:** REJECTED

- **#1034** is still not proven closed: `src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs` only demonstrates fail-fast behavior for duplicate **paths**, not explicit conflicting **schemas**, **definitions**, and **security schemes**.
- The broader core validation lane is not green because `dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj` still fails `Dynamic_Querystring_Generation_Preserves_Original_Query_Param_Documentation(ByEndpoint)` in `Issue1039_DynamicQuerystringMutationTests`.
- Dallas is now locked out of the next revision cycle for this artifact; Parker remains locked out from the prior rejected cycle.
- Lambert now owns the next/final revision cycle for **#1034** while staying in the blocker-test lane.

### Ash final review of Lambert revision

**Verified By:** Ash  
**Status:** REJECTED

- **#1039 acceptable:** `ParameterExtractor.GetQueryParameters()` still snapshots query parameters locally and preserves the shared `operationModel.Parameters` list; the regression coverage remains aligned with the intended non-mutating behavior.
- **#1034 still not proven closed:** the Swagger 2 definition-collision proof is still not isolated cleanly enough. `OpenApiDocumentFactoryMergeTests.Merge_With_Definition_Collision_Throws_And_Does_Not_Mutate_Inputs` still trips the duplicate **schema** conflict before it conclusively proves the duplicate **definition** lane.
- Remaining blocker for the next cycle: isolate the definition-specific fail-fast proof so the test fails for the intended definition-collision reason instead of the mirrored schema path.
- Lambert now joins Parker and Dallas in lockout for the next revision cycle on this artifact.
- Ripley now owns the next narrow revision cycle for **#1034**.

### Ripley final #1034 proof-gap revision

**Verified By:** Ripley  
**Status:** IMPLEMENTED / PENDING ASH FINAL SIGNOFF

- Preserve the source document schema type during clone/copy so Swagger 2 inputs stay on the intended definitions surface throughout merge handling.
- Isolate the Swagger 2 definition-collision proof at MergeIfMissingOrThrowOnConflict(...) so the definition-specific fail-fast contract is asserted directly instead of being masked by the mirrored schema collision first.
- Reported validation from the revision lane is green for dotnet build -c Release src\Refitter.slnx and dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj.
- Ash now owns the final reviewer signoff before broader validation resumes.
### Ash final signoff on Ripley #1034/#1039 follow-up

**Verified By:** Ash  
**Status:** APPROVED

- **#1034 approved:** OpenApiDocumentFactory.Merge() now clones the first document before merge, fails fast on conflicting duplicate path/schema/definition/security keys, and isolates the remaining Swagger 2 definition proof through the shared MergeIfMissingOrThrowOnConflict(...) path.
- **#1039 approved:** grouped dynamic-query extraction still snapshots query parameters instead of mutating operationModel.Parameters, and XML-doc regression coverage remains locked for single-interface, ByTag, and ByEndpoint generation.
- Evidence reviewed: src/Refitter.Core/OpenApiDocumentFactory.cs, src/Refitter.Tests/OpenApiDocumentFactoryMergeTests.cs, src/Refitter.Tests/RegressionTests/Issue1039_DynamicQuerystringMutationTests.cs, and src/Refitter.Tests/ParameterExtractorPrivateCoverageTests.cs.
- Reviewer signoff was reported against dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj with 1840 passing and 0 failing.

### Final PR package guidance for #1057

**Prepared By:** Ripley  
**Status:** READY FOR PR ASSEMBLY

- Proposed PR title: `[v2.0 audit] Close remaining verified #1057 regressions`.
- Keep PR summary focused on five landed lanes: source-generator diagnostics, newline-safe enum-converter rewriting, non-mutating dynamic querystring generation, fail-fast multi-spec merge handling, and tooling/runtime compatibility hardening.
- Safe auto-close set for the final PR body: **#1028, #1029, #1033, #1034, #1039, #1041, #1043**.
- Keep **#1032, #1042, #1045, #1047, and #1056** out of auto-close wording because they are validation-only, fixed-at-HEAD/stale, or doc/invariant-only.
- Before opening the PR, recreate/publish v2.0.0-prerelease-fixes with `git push -u origin HEAD` because the local branch tracks a gone upstream.
- Latest local full validation reported: dotnet restore src\Refitter.slnx, dotnet build -c Release src\Refitter.slnx --no-restore, dotnet test -c Release src\Refitter.slnx --no-build, and dotnet format --verify-no-changes src\Refitter.slnx --no-restore with 1886 tests passing.

### CLI help output assertions should be semantic

**Verified By:** Lambert (Tester)  
**Status:** APPROVED

- src\Refitter\Program.cs intentionally rewrites a no-argument invocation to --help, exits 0, and emits Spectre.Console.Cli help output.
- The current product behavior is correct; the instability sits in whitespace-sensitive test expectations, not in production code.
- src\Refitter.Tests\GenerateCommandTests.cs should assert semantic help markers (usage pattern, sections, and known option names) instead of exact formatter-driven spacing/default-value layout.
- Validation reported: release run of src\Refitter.Tests\Refitter.Tests.csproj, focused rerun of Program_Main_Should_Show_Help_When_Invoked_Without_Arguments, and dotnet format --verify-no-changes src\Refitter.slnx.
- Cross-agent merge outcome: Dallas proved the Ubuntu failure was ANSI/wrapping noise in raw Spectre.Console help output, and Lambert landed the test-only fix in src\Refitter.Tests\GenerateCommandTests.cs by normalizing redirected console output before asserting semantic help markers.
- Final landed validation for the product commit `normalize help output test across platforms`: dotnet build -c Release src\Refitter.slnx, dotnet test -c Release src\Refitter.slnx, and dotnet format --verify-no-changes src\Refitter.slnx.

### RefitterGenerateTask edge-case coverage stays test-only

**Verified By:** Dallas / Lambert  
**Status:** APPROVED

- Preserve the current `src\Refitter.MSBuild\RefitterGenerateTask.cs` behavior and close the remaining coverage gap with regression tests instead of production changes.
- Lambert isolated the last uncovered branches to `TryExecuteRefitter()` exception handling, missing bundled CLI handling in `StartProcess()`, `ResolveRefitterDll()` fallback edges, sub-second timeout formatting, and the non-throwing `TryLogErrorFromException()` path.
- Dallas landed the test-only closure in `src\Refitter.Tests\RefitterGenerateTaskTests.cs`, covering blank package folders, whitespace runtime entries, co-located and first-bundled fallback resolution, missing bundled CLI failure, process-runner exception handling, millisecond timeout formatting, and successful `LogErrorFromException` forwarding.
- Reported validation: `dotnet test --project src\Refitter.Tests\Refitter.Tests.csproj -c Release --coverage --coverage-output coverage.cobertura.xml --coverage-output-format xml`, `dotnet build -c Release src\Refitter.slnx --no-restore`, and `dotnet format --verify-no-changes src\Refitter.slnx --no-restore`.
- Result: `RefitterGenerateTask.cs` reached 100% line coverage, 100% block coverage, and 0 partial functions in the reported coverage output.
### PR #1070 SonarCloud quality-gate repair

**Verified By:** Dallas / Ash / Parker  
**Status:** APPROVED

- Keep the ParameterExtractor (S1066) and RefitterGenerateTask (S3267, S3358) changes narrow and behavior-preserving; Ash approved those cleanups as safe.
- Keep the source-generator diagnostic cleanup on the stable one-descriptor-per-ID contract: reuse the shared Refitter title/category constant, but assign distinct IDs when the title/message semantics differ.
- Preserve GeneratedDiagnostic as a readonly record struct; retain the explicit ordinal GetHashCode() behavior and handle Sonar S1206 with a targeted suppression plus justification instead of rewriting the type into a manual struct.
- Final validation recorded for the landed artifact: dotnet build -c Release src\Refitter.slnx --no-restore, dotnet test -c Release --solution src\Refitter.slnx --no-build, and dotnet format --verify-no-changes src\Refitter.slnx --no-restore.

## 2026-04-26

### AI-slop cleanup sequencing

**Verified By:** Ripley
**Status:** APPROVED

- Start with **docs/help drift** so stale SourceGenerator and MSBuild guidance is corrected before code refactors.
- Follow with **settings/spec-path normalization** across CLI and source-generator entry points, then centralize the shared **`GeneratedFile:` marker contract**.
- Keep **test-surface cleanup** ahead of deeper generator dedup so regression coverage is stronger before touching contract-sensitive code.
- Leave **core generator dedup** for the last batch and treat `OpenApiDocumentFactory` merge semantics, auth-header CLI compatibility, source-generator diagnostic contracts, and single-file `GeneratedCode` metadata behavior as reviewer-gated cleanup seams.
- Current squad capacity is sufficient for docs, tests, tooling, and settings cleanup; bring in a specialized C# reviewer only for the later core generator / `ParameterExtractor` dedup lane.

### Cleanup-safe baseline and coverage guardrails

**Verified By:** Lambert (Tester)
**Status:** VERIFIED

- Baseline validation is healthy on this machine: restore, release build, solution tests, and format verification all passed.
- Treat the Codecov command in `.github\workflows\codecov.yml` as the authoritative cleanup coverage lane; it targets `src\Refitter.Tests\Refitter.Tests.csproj`.
- Prefer narrow branch-coverage gains before production edits in `src\Refitter.Core\ContractTypeSuffixApplier.cs`, `SchemaCleaner.cs`, `CSharpClientGeneratorFactory.cs`, `XmlDocumentationGenerator.cs`, `IdentifierUtils.cs`, and `src\Refitter\Settings.cs`.
- Do not use the live-network test surface (`OpenApiDocumentFactoryTests`, `SwaggerPetstoreTests`, `SwaggerPetstoreApizrTests`, `Examples\OpenApiUrlTests`) as a stability signal during cleanup because it remains environment-sensitive.
- Coverage reports also include `.test-work` runtime-proof artifacts; note them, but prioritize real repository source files first.

### Generator cleanup phasing

**Verified By:** Parker (Core Developer)
**Status:** APPROVED

- Land generator cleanups in two phases: first trim redundant string/branch duplication already covered by public regressions in `XmlDocumentationGenerator.AppendXmlCommentBlock()` and `ContractTypeSuffixApplier.TypeSuffixRewriter`.
- Before deeper dedupe, add or tighten compile-backed public regressions for `ParameterExtractor`, multipart generation, and interface-emission behavior.
- Route any cleanup that deduplicates `RefitGenerator.Generate()` vs `GenerateMultipleFiles()` or the shared method-emission logic across the three interface generators through **Ash review**.

### AI-slop safety review gate

**Verified By:** Ash
**Status:** REQUIRED

- Treat the safety-sensitive generator and MSBuild cleanup lane as **validation-first** rather than style-first.
- Require source-generator regressions proving same-directory `.refitter` files with the same `outputFilename` do not collide on `AddSource()` hint names and that `AdditionalText.GetText(...) == null` reports a diagnostic instead of throwing.
- Require generator regressions for OpenAPI-title-derived identifiers containing `<` / `>` and for ByEndpoint interface names whose internal `I` characters must remain intact.
- Require MSBuild coverage for `GetInstalledDotnetRuntimes()` timeout/failure handling before claiming the process-launch path is hardened.

### Session directives refresh

**By:** Christian Helle (via Copilot)

- 2026-04-26: Have all agents use GPT-5.5 for the rest of this session only.
- 2026-04-26: Commit changes as frequent as possible in small logical groups for a detailed progress history.

## 2026-04-28

### e-conomic Multi-Spec OpenAPI Merge Failure

**Leads:** Ripley (analysis), Parker (core finding), Dallas (tooling validation)  
**Tester:** Lambert  
**Status:** TRIAGED AND APPROVED FOR FIX

#### Findings

- `test\economic.refitter` triggers merge failure in `OpenApiDocumentFactory.Merge()` while combining `economic-products.json` and `economic-webhooks.json`.
- Exception: `InvalidOperationException: Cannot merge OpenAPI documents because a duplicate schema 'Error' was found.`
- Root cause: `AreEquivalent()` calls `Serializer.Serialize()` on NSwag/NJsonSchema objects; object-cycle behavior returns `false` for semantically identical schemas.
- Both `Error` and `ProblemDetails` are duplicated and textually equivalent in source JSON; merge fails at `Error` first.
- Issue is **not** in `.refitter` parsing, path resolution, or individual spec generation—each spec generates successfully in isolation.
- CLI, source generator, and MSBuild all reach the same merge failure point before code output.

#### Fix Ownership and Scope

**Primary owner:** `Refitter.Core` → `src\Refitter.Core\OpenApiDocumentFactory.cs` → `MergeIfMissingOrThrowOnConflict()` / `AreEquivalent()`

- Replace `AreEquivalent()` with OpenAPI-aware semantic equivalence check (use NSwag `ToJson()` or canonical JSON representation).
- Keep fail-fast policy for **genuinely conflicting** duplicate path/schema/definition/security entries.
- Keep clone-first/non-mutating merge behavior.
- Do **not** edit e-conomic OpenAPI specs; do **not** rename schemas in merge.

#### Approved Test Coverage and Gates

1. Add `OpenApiDocumentFactoryMergeTests`:
   - `Merge_With_Equivalent_Duplicate_Schema_Does_Not_Throw` — OpenAPI 3 docs with identical schema key and body; both paths preserved.
   - Preserve existing `Merge_With_Schema_Collision_Throws_And_Does_Not_Mutate_Inputs` for conflicts.
   - Optional e-conomic-shaped regression for `Error` schema with descriptions, nullable properties, extensions, `additionalProperties: false`.

2. Compile-backed regression: two specs with identical common error schema and distinct paths; assert generation succeeds and generated code builds.

3. Run generated-code validation for `test\economic.refitter` after merge fix; triage any post-merge generation failure separately.

4. Existing merge-collision tests must continue passing.

#### Constraints

- Preserve relative-path behavior across CLI and source generator.
- Keep `openApiPaths` semantics consistent.
- OpenAPI 3 `components.schemas` and Swagger 2 `definitions` equivalence must remain aligned.
- Do not hide genuine schema differences in comparison.
