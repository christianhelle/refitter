### 2026-05-01T14:04:19.681+02:00: Issue #1083 response framing
**By:** Bishop
**What:** Treat issue #1083 as a likely product bug rather than a documentation dispute; current docs scope identifier sanitization to contract properties under propertyNamingPolicy, not schema or contract type names.
**Why:** The maintainer reply should acknowledge the broken generated output without inaccurately claiming the docs already promised schema-type sanitization.

### 2026-05-01T14:04:19.681+02:00: User directive
**By:** Christian Helle (via Copilot)
**What:** Use GPT-5.5 for all agents for the rest of this session only.
**Why:** User request — captured for team memory

### 2026-05-01T14:34:56.630+02:00: Issue #1083 adjacent tooling verdict

**By:** Dallas

**Decision:** Do not add CLI, MSBuild, or README follow-up for #1083. The bug and its fix live in shared core type-name generation, so those surfaces inherit the corrected behavior without new wiring or user-facing settings. Add only source-generator regression coverage to prove the compile-time surface emits the sanitized DTO type and method return type for dotted schema names.

**Why:** #1083 does not introduce a new option, command contract, settings shape, or consumer workflow. The adjacent risk was validation drift in the source-generator lane, so a focused generated-code test is the smallest correctness guard that keeps scope tight.

# Lambert issue #1083 repro decision

- **Date:** 2026-05-01T14:04:19.681+02:00
- **Requester:** Christian Helle
- **Decision:** Treat GitHub issue #1083 as a valid current-HEAD bug and anchor the regression on a minimal inline Swagger 2 fixture that uses the exact dotted schema key `LookUpErnResponse.`. Keep any real Revenue-spec check as optional evidence only, not as the primary automated regression, to avoid live-network coupling.

## Evidence

- Minimal fixture generation produced `Task<> LookUpERN(...)` and `public partial class` with no identifier.
- Real Revenue spec generation produced the same failure shape in `IPAYEEnhancedReportingNotificationRESTAPIApi.cs` and `Contracts.cs`.
- Isolated compile checks failed with `CS1001 Identifier expected` for both generated outputs.

## Required regression coverage

1. Add a focused scenario test file in `src\Refitter.Tests\Scenarios` for invalid schema/type names.
2. Use an inline fixture with one endpoint whose 200-response references `#/definitions/LookUpErnResponse.`.
3. Assert generated code does **not** contain `Task<>` or `partial class` followed by a blank name.
4. Assert generated code **does** contain a concrete sanitized DTO identifier and that the response method returns that identifier.
5. Add a `BuildHelper.BuildCSharp(generatedCode).Should().BeTrue()` assertion so the regression proves compilable output, not just string replacement.
6. Only add/update `IdentifierUtilsTests` if the implementation explicitly routes schema/type-name sanitization through shared identifier utilities; otherwise keep coverage at the scenario level.

## 2026-05-01T14:34:56.630+02:00

- Decision: keep issue #1083 coverage in a dedicated scenario test file instead of widening `PR1064BlockerRegressions`, `PropertyNamingPolicyTests`, or `IdentifierUtilsTests`.
- Why: the failure is schema/type-name generation, not property/parameter sanitization; isolating it behind a minimal inline fixture keeps the regression signal focused and avoids coupling unrelated suites to dotted-schema behavior.
- Required assertions: no blank `partial class`, no `Task<>`, `LookUpErnResponse` used consistently in generated contracts and interface signatures, plus a compile gate.

---
timestamp: 2026-05-01T14:34:56.630+02:00
agent: parker
issue: 1083
---

# Decision

Implement issue #1083 at the schema type-name generation hook in `src\Refitter.Core\CSharpClientGeneratorFactory.cs`, not in downstream interface or contract post-processing.

# Why

- The failure starts in NSwag/NJsonSchema type resolution when a schema hint ends with an empty `.` segment.
- Repairing the hint before `DefaultTypeNameGenerator` runs fixes both DTO declarations and response signatures with one narrow change.
- To preserve normal behavior, malformed keys only normalize empty segments; if that normalized name collides with an existing clean schema key, the malformed schema is forced onto the counted suffix so the clean schema keeps the unsuffixed base name.

# Validation

- `dotnet format src\Refitter.slnx`
- `dotnet build -c Release src\Refitter.slnx`
- `dotnet test -c Release src\Refitter.slnx --no-build` (remaining failures were the known external-URL timeout lane in `PathParametersWithUrlTests`)
- `dotnet format --verify-no-changes src\Refitter.slnx`
- Manual CLI generation + scratch-project compilation for single-schema and collision fixtures

# Parker Plan: Issue #1083

**Date:** 2026-05-01T14:04:19.681+02:00  
**Owner:** Parker  
**Status:** Planning only

## Decision

Issue #1083 is a valid current bug in Refitter's generator pipeline.


- Reproduced at HEAD against `https://revenue-ie.github.io/paye-employers-documentation/PIT3/rest/paye-employers-rest-api-pit3.json`.
- Generated output contains both:
  - `internal partial class` with no type name
  - `Task<> LookUpERN(...)`
- The offending Swagger 2 definition key is `LookUpErnResponse.` and the `GET /ern/{employerRegistrationNumber}/{taxYear}` response references `#/definitions/LookUpErnResponse.`

## Root Cause

- Refitter currently relies on NSwag/NJsonSchema default type naming for schema names.
- NJsonSchema's `DefaultTypeNameGenerator` treats `.` as a segment separator and uses the last segment.
- For a trailing-dot name like `LookUpErnResponse.`, the last segment is empty, and the fallback path also returns an empty string instead of a usable anonymous name.
- That empty type name flows into both DTO emission and `generator.GetTypeName(...)`, producing the blank class declaration and empty generic return type.

## Safest Fix Shape

1. Add a Refitter-owned custom type-name generator that preserves current NSwag naming behavior for normal inputs.
2. Only special-case malformed hints whose final segment is empty or whose generated type name is blank.
3. Normalize those cases to the last non-empty segment (for example `LookUpErnResponse.` -> `LookUpErnResponse`) and still route the final identifier through Refitter-safe identifier sanitization.
4. Inject that generator in `CSharpClientGeneratorFactory` so the fix applies once at the DTO/type-resolution layer.

## Main Files Likely Needed

- `src\Refitter.Core\CSharpClientGeneratorFactory.cs`
- new core generator file such as `src\Refitter.Core\RefitterTypeNameGenerator.cs`
- possibly `src\Refitter.Core\IdentifierUtils.cs` if a shared helper is introduced for final identifier normalization
- regression coverage in `src\Refitter.Tests\Scenarios\PR1064BlockerRegressions.cs` or a dedicated scenario test file
- possibly `src\Refitter.Tests\IdentifierUtilsTests.cs` if helper behavior moves into `IdentifierUtils`

## Risks / Tradeoffs

- Any type-name generator change can rename emitted contracts for malformed schema names, so tests should pin current behavior for ordinary names.
- Avoid broad dot replacement: dotted names may already rely on NSwag's "use last segment" behavior, so the fix should target only empty-tail cases.
- Collisions remain possible if multiple malformed names normalize to the same identifier; rely on NSwag's reserved-name flow or add focused collision coverage.
- Rewriting schema keys and refs in the OpenAPI document would be much riskier because it can disturb references, exclusions, and other preprocessing logic.

### 2026-05-04T08:59:00.400+02:00: User directive
**By:** Christian Helle (via Copilot)
**What:** Use GPT-5.5 for all agent work for the rest of this session while investigating issue #1088 and planning the fix.
**Why:** User request — captured for team memory

# Ripley issue #1088 plan

- **Date:** 2026-05-04T08:59:00.400+02:00
- **Issue:** #1088 — "Casing in generated code is wrong"
- **Requested by:** Christian Helle

## Decision

**Recommendation:** treat #1088 as a contract-sensitive integration regression and fix it only after adding compile-backed repro coverage. Do **not** start by broadly changing `StringCasingExtensions` or sprinkling one-off casing rewrites through the generators.

## Why this is the safest path

- `src\Refitter.Core\OperationNameGenerator.cs` and `src\Refitter.Core\StringCasingExtensions.cs` do not show a direct 1.7.3 → 2.0.0 behavior change on their own, so a blind helper rewrite risks destabilizing unrelated naming surfaces without proving the reported break.
- A minimal standalone `Refit` 9.x/10.x interface named `FormGet(...)` compiles cleanly, so the report is more likely at the Refitter-generated-interface boundary than a generic Refit-only failure.
- Current focused naming tests pass, but they do not cover acronym/HTTP-verb cases and the source-generator test suite mostly checks interface presence/attributes rather than compile-time compatibility with Refit's implementation generator.

## Implementation-plan outline

1. **Add a failing repro first**
   - Create a minimized OpenAPI repro in `src\Refitter.Tests\Scenarios\` covering acronym/HTTP-verb operation names (`GET`, `POST`, etc.) and assert both generated method names and successful compilation.
   - Add a matching source-generator integration test in `src\Refitter.SourceGenerator.Tests\` that proves generated interfaces compile cleanly in a consuming project with `Refit`.

2. **Localize the defect before changing behavior**
   - Compare the generated method names across single-interface, `MultipleInterfaces.ByEndpoint`, and `MultipleInterfaces.ByTag`.
   - Verify whether the mismatch originates in operation-name normalization (`OperationNameGenerator` / `RefitInterfaceGenerator`) or only in the Refitter source-generator + Refit source-generator interaction.

3. **Patch only the shared naming seam**
   - If Refitter is emitting unstable casing, fix it in the centralized operation-name path (`OperationNameGenerator` and its callers), not in downstream one-off generators.
   - Preserve existing generated-name behavior for already-covered cases; only normalize the acronym/HTTP-verb edge case proven by the new regression tests.

4. **Validate the full blast radius**
   - Re-run the focused naming scenario classes plus any new source-generator integration coverage.
   - Before merge, run the normal repo gates: restore, release build, full test suite, and format verification.

## Blast radius to watch

- Single-interface generation in `src\Refitter.Core\RefitInterfaceGenerator.cs`
- Multi-interface generation in `src\Refitter.Core\RefitMultipleInterfaceGenerator.cs` and `src\Refitter.Core\RefitMultipleInterfaceByTagGenerator.cs`
- CLI and `.refitter` mapping in `src\Refitter\GenerateCommand.cs` and `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs`
- Consumer builds that rely on Refit's generated implementations, where casing drift becomes a compile break rather than a cosmetic difference

## Parker issue #1088 analysis [2026-05-04T08:59:00.400+02:00]

- Interface-side method naming is centralized in `src\Refitter.Core\RefitInterfaceGenerator.cs` (`GenerateOperationName`) and `src\Refitter.Core\OperationNameGenerator.cs` (`GetOperationName`), so any fix should stay in that shared core path instead of surface-specific CLI/source-generator wiring.
- The naming pipeline itself predates the 2.0.0 tag; the strongest 2.0.0 regression candidate is the packaging/runtime shift around the source-generator lane, especially because `src\Refitter.Tests\Build\ProjectFileContents.cs` still validates generated code against `Refit.HttpClientFactory` 8.0.0 while `src\Refitter.SourceGenerator\Refitter.SourceGenerator.csproj` ships newer Refit bits.
- Safest planned fix shape: add a very narrow operation-name normalization step for all-caps HTTP verb suffixes (for example `...GET` -> `...Get`) inside the shared operation-name pipeline, then cover it with both core scenario assertions and a source-generator consumer build that exercises the active Refit implementation generator.

## Dallas issue #1088 tooling note [2026-05-04T08:59:00.400+02:00]

- Issue #1088 presents as a Refit-generated implementation mismatch (`FormGet` interface vs `FormGET` implementation), but Refitter's three tooling surfaces all converge on the same `Refitter.Core.RefitGenerator` naming pipeline.
- Planning assumption: treat the fix as a shared core naming / downstream Refit compatibility investigation, not as separate CLI, source-generator, or MSBuild wiring bugs.
- Regression plan should include parity coverage for:
  - core scenario output,
  - CLI generation smoke output,
  - source-generator compile-time generation,
  - MSBuild-driven generation,
  - and at least one consumer build against the current `Refit.HttpClientFactory` source-generator path.

# Lambert issue #1088 test decision

- Date: 2026-05-04T08:59:00.400+02:00
- Decision: Treat issue #1088 as an exact method-casing regression and cover it with compile-backed generation tests plus explicit generated-member assertions.
- Why:
  - The reported failure is an interface/implementation mismatch, so string-only generation checks are insufficient.
  - Existing adjacent coverage in `src\Refitter.Tests\Scenarios\MultipleInterfacesByTagsWithSamePathSegmentTests.cs` allows both `InfoGet` and `InfoGET`, which can hide casing drift.
  - `src\Refitter.Tests\StringCasingExtensionTests.cs` currently exercises only happy-path casing examples and does not define expected behavior for all-caps segments like `GET`.
- Recommended test shape:
  1. Add one minimal inline OpenAPI scenario reproducing the exact casing contract once the reporter snippet or a repo-local failing shape is known.
  2. Assert the generated interface contains the expected method name and does not contain the alternate casing.
  3. Compile the generated code so Refit source-generation mismatches are caught.
4. If the fix changes helper semantics, add narrow helper-level tests for acronym inputs such as `form-GET`, `form GET`, and `formGET`.

# Lambert issue #1088 repro outcome

- **Date:** 2026-05-04T15:23:00.022+02:00
- **Requester:** Christian Helle
- **Decision:** Treat the current local evidence as a confirmed casing-shape regression between Refitter versions, but **not yet** as a reproduced consumer compile failure. Keep the next step focused on compile-backed coverage that proves whether any remaining failure requires source-generator-specific wiring or a user-package/version skew.

## Confirmed facts

1. The reporter's GET-only comment snippet does **not** reproduce the FormGet/FormGET split by itself; all tested versions generate Form(...) when only one verb exists on the path.
2. A minimized shape with both GET and POST on /api/process/element/{elementType}/{elementInstance}/form reproduces the casing boundary:
   - 1.7.3 emits FormGET / FormPOST
   - 2.0.0 emits FormGet / FormPost
   - current HEAD emits FormGet / FormPost
3. Local consumer compilation of the generated interface succeeded for every tested pairing with Refit.HttpClientFactory 8.0.0, 9.0.2, and 10.1.6, so the exact interface/implementation mismatch remains unconfirmed here.
4. Relevant package lanes observed in repo:
   - src\Refitter.SourceGenerator\Refitter.SourceGenerator.csproj at 2.0.0 references Refit 9.0.2.
   - current HEAD references Refit 10.1.6.
   - generated-code build fixtures still pin Refit.HttpClientFactory 8.0.0.

## Implication

The safest follow-up is to add regression coverage around the confirmed 1.7.3 -> 2.0.0 casing change and then probe any remaining mismatch through a source-generator/consumer-version lane, rather than assuming the reported compile break is reproducible from Refitter output alone.

---
date: 2026-05-04T15:23:00.022+02:00
agent: ripley
issue: 1088
---

# Decision

Do not start production or regression-test changes for issue #1088 until the team has an exact compile-backed repro that matches the reporter's failure shape, or a minimized equivalent proven to fail across the same dependency lane.

# Why

- The reporter-provided artifact is still partial: one path snippet and one schema, but no full spec, `.refitter` settings, or package matrix.
- Reconstructing the obvious minimized case required **both** GET and POST on `/api/process/element/{elementType}/{elementInstance}/form`; that produced `FormGet`/`FormPost` as expected, but it compiled successfully on:
  - Refitter `2.0.0` + Refit `10.1.6`
  - current HEAD + Refit `10.1.6`
  - current HEAD + Refit `8.0.0`
- A hand-written Refit interface with `FormGet`/`FormPost` also compiled cleanly on Refit `8.0.0` and `10.1.6`, so the mismatch is not proven to be a generic Refit source-generator bug either.
- The remaining current evidence is therefore insufficient to justify changing `OperationNameGenerator`, `StringCasingExtensions`, or adjacent tests.

# Gate for Parker and Dallas

Parker and Dallas may begin code/test changes only after **one** of these is true:

1. The exact user repro is available (full spec or sanitized equivalent, `.refitter` settings, and package versions) and fails in a compile-backed harness; or
2. A minimized repo-local fixture reproduces the same interface/implementation mismatch and is shown to fail under the same Refitter/Refit version lane that users actually consume.

If neither condition is met, the correct next step is to request more issue detail rather than invent a failing case and patch blind.

---
date: 2026-05-04T15:23:00.022+02:00
agent: lambert
issue: 1088
---

# Decision

Treat issue #1088 as a confirmed same-path multi-verb naming-shape change, but not yet as a reproduced consumer compile mismatch.

# Why

- The reporter-provided GET snippet and `Form` schema only reproduce the verb suffix boundary once a same-path POST operation is added.
- In that minimized shape, Refitter 1.7.3 emits `FormGET` / `FormPOST`, while Refitter 2.0.0 and current HEAD emit `FormGet` / `FormPost`.
- Tag changes did not change the method names in the reconstructed repro.
- Compile-backed consumer probes showed Refit-generated implementation stubs matching the interface names on Refit.HttpClientFactory 8.0.0, 9.0.2, and 10.1.6.

# Evidence

- `.test-work\issue-1088-repro\outputs\1.7.3.cs`
- `.test-work\issue-1088-repro\outputs\2.0.0.cs`
- `.test-work\issue-1088-repro\outputs\head-default-get.cs`
- `.test-work\issue-1088-repro\outputs\head-default-post.cs`
- `.test-work\issue-1088-repro\outputs\head-default-getpost.cs`
- `.test-work\issue-1088-repro\compile-fixed\`

# Consequence

Do not patch `src\Refitter.Core\OperationNameGenerator.cs` or adjacent tests yet unless a failing consumer lane is found, or the reporter supplies the missing full spec/settings/package matrix.

---
date: 2026-05-04T15:23:00.022+02:00
agent: bishop
issue: 1088
---

# Decision

Draft the issue follow-up as a compatibility update, not as a fully reproduced compile-failure confirmation.

# Why

- Local validation confirmed a narrow behavior regression: for path-derived same-path multi-verb operation names, Refitter 1.7.3 emitted `FormGET`/`FormPOST`, while 2.0.0 and current HEAD emitted `FormGet`/`FormPost`.
- The exact interface/implementation compile break reported by the user did not reproduce locally, so the maintainer reply should avoid overstating certainty on that point.
- The approved fix is intentionally narrow in `src\Refitter.Core\OperationNameGenerator.cs`: restore legacy all-caps verb suffixes only for the path-derived same-path multi-verb case, while leaving explicit `operationId` names such as `FormGet` unchanged.
- Current docs do not promise specific HTTP-verb casing for generated method names, so no immediate README correction is required unless new evidence shows a documentation misunderstanding.

---
date: 2026-05-04T15:23:00.022+02:00
agent: dallas
issue: 1088
---

# Decision

Keep the shared generated-code build fixture on the legacy Refit 8.x lane, and add a separate current-tooling fixture for issue #1088 compatibility coverage.

# Why

- `src\Refitter.Tests\Build\ProjectFileContents.cs` is used broadly by compile-gate tests, including Apizr scenarios that still depend on older Refit-compatible package combinations.
- Globally bumping that fixture to `Refit.HttpClientFactory` 10.1.6 broke many unrelated compile tests even though the new issue #1088 parity check only needed one focused current-lane build.
- A dedicated `Net80AppCurrentTooling` fixture plus `BuildHelper.BuildCSharpWithProject(...)` lets Dallas validate source-generator/current-Refit compatibility without destabilizing the repo-wide legacy compile harness.

# Applied files

- `src\Refitter.Tests\Build\ProjectFileContents.cs`
- `src\Refitter.Tests\Build\BuildHelper.cs`
- `src\Refitter.SourceGenerator.Tests\Build\ProjectFileContents.cs`
- `src\Refitter.SourceGenerator.Tests\Build\BuildHelper.cs`
- `src\Refitter.SourceGenerator.Tests\SourceGeneratorCompatibilityTests.cs`
- `src\Refitter.SourceGenerator.Tests\SourceGeneratorPackageReferenceTests.cs`

# Lambert issue #1088 test scope

- **Date:** 2026-05-04T15:23:00.022+02:00
- **Requester:** Christian Helle
- **Decision:** Keep the issue #1088 regression tightening in test-only scope by adding one dedicated same-path multi-verb scenario plus exact by-tag assertions, instead of widening the broad generator-matrix tests right now.

## Why

- The confirmed boundary is specific: same-path GET/POST operations changed from `FormGET`/`FormPOST` in 1.7.3 to `FormGet`/`FormPost` in 2.0.0 and HEAD.
- `MultipleInterfacesByTagsWithSamePathSegmentTests.cs` already sat adjacent to the bug but hid it by permitting both `InfoGet` and `InfoGET`; that file needed a narrow correction.
- A dedicated scenario file reduces merge overlap with Parker while still pinning the consumer-facing contract with a compile-backed build check.

---
date: 2026-05-04T15:23:00.022+02:00
agent: parker
issue: 1088
---

# Decision

Restore the legacy all-caps HTTP verb suffix only in the shared `OperationNameGenerator` path, and only for same-path multi-verb operations whose names are path-derived.

# Why

- The confirmed compatibility drift is limited to same-path multi-verb names such as `FormGET` / `FormPOST` becoming `FormGet` / `FormPost`.
- A broad `StringCasingExtensions` rewrite would risk changing unrelated identifiers, including explicit operation IDs that intentionally end with `Get` / `Post`.
- Guarding the normalization behind same-path multi-verb detection plus path-derived naming keeps explicit operation-id casing untouched while restoring the 1.7.3-compatible suffix shape where the regression was observed.

# Validation

- `dotnet build -c Release src\Refitter.Tests\Refitter.Tests.csproj --no-restore`
- Built and ran a temporary `.test-work\issue-1088-validation` harness that invoked the new regression methods directly, then removed the scratch files after the run.
- `dotnet format --verify-no-changes src\Refitter.slnx --no-restore`

---
date: 2026-05-04T15:23:00.022+02:00
agent: ripley
issue: 1088
---

# Decision

Approve the narrow compatibility fix for the observed 1.7.3 -> 2.0.0 same-path multi-verb casing change.

# Why

- The only production change is localized to `src\Refitter.Core\OperationNameGenerator.cs`, which is the shared naming seam for generated interface methods.
- The patch restores legacy `GET`/`POST` suffix casing only when the generated name is path-derived on a route that has multiple verbs, which matches the confirmed drift without broadening the casing rules.
- Explicit operation-id-driven names remain on the current PascalCase contract (`FormGet`/`FormPost`), so the fix does not rewrite user-authored operation IDs.
- The adjacent test changes tighten previously permissive coverage, add a dedicated issue repro, and add compile-backed source-generator coverage against the current tooling lane without changing CLI or source-generator production wiring.

# Validation

- `dotnet build -c Release src\Refitter.slnx`
- `dotnet test -c Release src\Refitter.SourceGenerator.Tests\Refitter.SourceGenerator.Tests.csproj`
- `dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj` *(only the known live-URL timeout lane failed: `OpenApiDocumentFactoryTests.Create_From_Http_Url_Returns_NotNull` and `OpenApiDocumentFactoryTests.IsHttp_Detects_Http_Protocol`)*
- `dotnet format --verify-no-changes src\Refitter.slnx`

# Residual risk

- The fix intentionally reintroduces the legacy all-caps suffix only for path-derived same-path multi-verb names; if a downstream consumer depends on the 2.0.0+ PascalCase form for that narrow shape, this is a compatibility trade back toward 1.7.3 behavior.
- The consumer compile mismatch originally reported still was not reproduced locally; this batch safely restores the old method names and hardens the regression net, but it does not prove a separate external package/version interaction cannot still exist in the reporter's environment.

# Ash review decision: Issue 1088 source-generator angle

Primary source-generator investigation should focus on build-time duplication and generator ordering, not core operation-name casing.

## Why
- `Refitter.SourceGenerator.props` already includes `**/*.refitter` as `AdditionalFiles`, so a consuming project that also declares `<AdditionalFiles Include=".refitter" />` can feed the same config into the generator twice.
- `RefitterSourceGenerator` does not deduplicate `AdditionalText` inputs before calling `AddSource`, and its hint name is path-based, so duplicate inclusion can create same-source/hint conflicts or duplicate surfaces.
- The source-generator README explicitly recommends `Refitter.MSBuild` when Refit source generation is involved because pre-compile generation avoids rebuild/order issues.
- Legacy/sample build integration under `test\SourceGenerator` still compiles checked-in `Generated\*.cs`, which is the exact stale-artifact pattern to rule out in consumers migrating from older generator behavior.

## Revised plan targets
1. Reproduce with and without manual `<AdditionalFiles Include=".refitter" />`.
2. Compare first build vs second build when Refit's generator is present.
3. Remove any checked-in/generated `.g.cs` compile items and compare results.
4. Keep core naming output fixed while varying only source-generator/build inputs.

---
date: 2026-05-05T14:18:05.572+02:00
agent: dallas
issue: 1088
---

# Decision

Handle the source-generator-side issue #1088 follow-up as a build-integration fix in the package targets, not as a shared naming change.

# Why

- The strongest proven reporter-style repro is now rebuild-sensitive: first build emits `FormGet`/`FormPost` successfully, then rebuild fails because stale `Generated\Refitter.SourceGenerator\...\*.g.cs` files are compiled again alongside fresh source-generator output.
- The same repro shape also carries duplicate `.refitter` inputs when consumers rely on the packaged auto-include and keep a manual `<AdditionalFiles Include=".refitter" />`.
- Local source-generator consumer rebuilds still matched `FormGet`/`FormPost`, so the originally reported interface/implementation casing mismatch remained unproven in the tooling lane.
- A narrow `Refitter.SourceGenerator.targets` fix can solve both proven build-integration hazards—deduplicating `.refitter` `AdditionalFiles` and removing `$(CompilerGeneratedFilesOutputPath)\**\*.cs` from `Compile` before `CoreCompile`—without touching shared core naming behavior.

# Applied files

- `src\Refitter.SourceGenerator\Refitter.SourceGenerator.targets`
- `src\Refitter.SourceGenerator\Refitter.SourceGenerator.csproj`
- `src\Refitter.SourceGenerator.Tests\SourceGeneratorPackageReferenceTests.cs`
- `src\Refitter.SourceGenerator.Tests\Issue1088SourceGeneratorConsumerRebuildTests.cs`

# Dallas decision: Issue 1088 source-generator reinvestigation

- Treat FormGET/FormPOST -> FormGet/FormPost as intentional shared-core behavior, not the bug target.
- Focus the follow-up plan on source-generator-only divergence:
  1. package props auto-including `**/*.refitter` while consumers may also add `<AdditionalFiles Include=".refitter" />`, risking duplicate processing;
  2. source generator bypassing CLI settings normalization/validation (`ApplySettingsFileDefaults`, `SettingsValidator`);
  3. source generator always calling `Generate()` instead of `GenerateMultipleFiles()`, so source-generator behavior diverges from CLI for `generateMultipleFiles`/output settings;
  4. no end-to-end source-generator test currently verifies the downstream Refit implementation still matches the generated interface when `useCancellationTokens`, DI, and package-consumer semantics are involved.
- Best follow-up test target: a package-consumer integration test that builds a small project using the packed source-generator package, `EmitCompilerGeneratedFiles`, the issue 1088-style `.refitter`, and asserts no duplicate/stale generated sources plus a successful build of the Refit-generated implementation.

# Dallas decision: issue #1088 validation matrix

- Date: 2026-05-05T14:18:05.572+02:00
- Requested by: Christian Helle

## Decision

Treat issue #1088 as **shared-core naming with source-generator consumer validation**, not as three separate tooling bugs.

## Why

- CLI settings-file mode deserializes `.refitter` JSON into `RefitGeneratorSettings`, applies settings-file defaults, then calls `RefitGenerator.CreateAsync(...)` and `Generate()`/`GenerateMultipleFiles()`.
- The source generator also deserializes `.refitter` JSON into `RefitGeneratorSettings`, resolves relative spec paths, then calls the same `RefitGenerator.CreateAsync(...)` and `Generate()`.
- MSBuild does not implement naming itself; it shells the bundled CLI with `--settings-file`.
- The reported flags (`returnIApiResponse`, `useCancellationTokens`, `usePolymorphicSerialization`, `generateDisposableClients`) affect signature shape or emitted attributes, but not the operation-name casing pipeline.
- The only meaningful surface-specific difference in this area is that the source generator always emits a single `AddSource()` payload and does not honor CLI multi-file/output-folder behavior, so `generateMultipleFiles` should be treated as an adjacent tooling parity concern, not the likely root cause of a casing-only interface/implementation mismatch.

## Required validation matrix

1. **Core regression proof**
   - Add exact naming assertions for the simplified `/form` GET+POST repro in `Refitter.Core` coverage.
   - Add a compile-backed check for the reporter-style settings shape (`ProcessEngineClient`, `returnIApiResponse`, `useCancellationTokens`, `usePolymorphicSerialization`, `generateDisposableClients`).

2. **Source-generator consumer proof**
   - Add an end-to-end source-generator test that feeds a real `.refitter` file through Roslyn/consumer compilation and verifies the generated interface compiles on the **current Refit lane** (the lane that exercises Refit source generation, not only the legacy Refit 8.x helper template).
   - Include the reporter-style settings in that test, because those flags change the interface signature the downstream Refit tooling must implement.

3. **CLI parity proof**
   - Add or update a settings-file-driven CLI test proving the same `.refitter` input produces the same interface/member names as the core/source-generator path.
   - If the eventual fix touches multi-file generation, include a `GenerateMultipleFiles()` assertion too.

4. **MSBuild smoke proof**
   - Keep MSBuild coverage lightweight: one task-level smoke/integration test showing a `.refitter` file flows through the CLI path and reports generated files successfully.
   - Do not duplicate naming assertions here unless the fix changes CLI process orchestration.

## Consequence

Implementation should stay focused on shared naming behavior unless a new failing repro proves the source generator diverges beyond its known single-file `AddSource()` behavior.

# Lambert issue #1088 source-generator tests

- **Date:** 2026-05-05T14:18:05.572+02:00
- **Decision:** Anchor Lambert's source-generator coverage for issue #1088 in a dedicated reporter-style consumer rebuild repro instead of trying to lock in the unproven `FormGet`/`FormGET` mismatch.
- **Why:** Current HEAD still does not reproduce the reported interface/implementation casing split, but it does reproducibly fail on the analyzer path when a consumer emits compiler-generated files into a project-local `Generated\` folder and rebuilds. The first build succeeds and emits `FormGet`/`FormPost`; the second build fails because the stale emitted `.g.cs` file is compiled alongside the in-memory source-generator output.
- **Test file:** `src\Refitter.SourceGenerator.Tests\Issue1088SourceGeneratorConsumerRebuildTests.cs`
- **Scope guardrails:** Treat the `FormGET` -> `FormGet` naming drift as intentional/out of scope here; use this repro as the closest verified gap until the exact reporter setup can be proven locally.

# Lambert issue #1088 source-generator validation

- **Date:** 2026-05-05T14:18:05.572+02:00
- **Decision:** Treat `issue-1088-add-sourcegen-regressions` as complete on the current source-generator lane.
- **Why:** The current package now ships `Refitter.SourceGenerator.targets`, package tests prove the targets are present, the build-targets test proves duplicate `.refitter` AdditionalFiles are deduped, and the reporter-style consumer rebuild test proves `$(CompilerGeneratedFilesOutputPath)\**\*.cs` is excluded from `Compile` so first build and rebuild both succeed.
- **Remaining blocker assessment:** No remaining verified source-generator regression gap should block `issue-1088-patch-sourcegen-seam`. The only unresolved mismatch is the historical `FormGET` versus `FormGet` report itself, which remains unproven on current HEAD and is intentionally out of scope for this source-generator seam fix.

# Lambert decision: issue #1088 validation status

- Date: 2026-05-05T14:18:05.572+02:00
- Requested by: Christian Helle

## Decision

Treat issue #1088 as **partly proven**, not fully proven, on the current repo state.

## Why

- The user and maintainer comments, plus current repo-local reproduction, consistently prove one narrow behavior change: when a path has multiple verbs and no explicit `operationId`, Refitter now emits `FormGet` / `FormPost` instead of the legacy `FormGET` / `FormPOST`.
- The repo does **not** currently contain a dedicated regression test that pins that boundary in either `src\Refitter.Tests` or `src\Refitter.SourceGenerator.Tests`; adjacent by-tag coverage still only guards against numeric suffixes.
- A fresh CLI-generated consumer build against `Refit.HttpClientFactory` `10.1.6` compiled successfully with `FormGet` / `FormPost`.
- A fresh analyzer-backed source-generator consumer build using the reporter-style `.refitter` settings (`returnIApiResponse`, `useCancellationTokens`, `usePolymorphicSerialization`, `generateDisposableClients`, `generateMultipleFiles`) also compiled successfully and emitted `FormGet` / `FormPost`.
- That means the exact reported failure shape — interface expects `FormGet(...)` while generated implementation emits `FormGET(...)` — is still unproven here even though the naming-contract drift is real.

## Smallest missing tests

1. **One core regression scenario**
   - Inline OpenAPI spec with the same `/api/process/element/{elementType}/{elementInstance}/form` GET+POST shape and no `operationId`.
   - Assert the exact generated member names and reject the alternate casing.
   - Add a compile-backed check so downstream Refit implementation mismatches surface.

2. **One source-generator end-to-end consumer test**
   - Feed a real `.refitter` file through the analyzer path with the reporter-style settings and current Refit lane.
   - Build a minimal consumer project with `EmitCompilerGeneratedFiles=true`.
   - Assert the emitted member names and successful consumer compilation.

No broader matrix is needed until one of those two tests fails.

## Ambiguities the implementation plan must resolve

- The first user snippet only showed `get`; the naming split only appears once a same-path sibling verb is present, so investigation must explicitly decide whether the missing `post` shape is part of the real repro.
- The issue body cites an interface/implementation mismatch, but the current local source-generator lane does not reproduce that mismatch; the plan should ask whether the user’s failing package matrix or generated artifacts differ from current HEAD.
- The report does not confirm whether `operationId` is absent on both operations; that matters because explicit operation IDs follow a different naming path.
- The report mentions source generator plus `generateMultipleFiles`, but Refitter’s source generator emits a single `AddSource()` payload; the plan should treat multi-file intent as adjacent parity context unless evidence shows it changes the failing artifact.

## Parker issue #1088 validation — 2026-05-05T14:18:05.572+02:00

- Shared naming path confirmed: source-generator output flows through `Refitter.SourceGenerator\RefitterSourceGenerator.cs` into `Refitter.Core\RefitGenerator.Generate()`, then `RefitInterfaceGenerator.GenerateOperationName()` and `OperationNameGenerator.GetOperationName()`.
- Current HEAD behavior for missing-operationId same-path multi-verb operations is still `FormGet` / `FormPost`, not legacy `FormGET` / `FormPOST`.
- I reproduced the reporter-shaped `.refitter` settings and inspected emitted source-generator output; interface methods were `FormGet` / `FormPost`.
- I also compiled minimal consumer projects against Refit.HttpClientFactory 8.0.0, 9.0.2, and 10.1.6 with the generated interface, and those builds succeeded, so the interface/implementation mismatch is not yet reproduced locally.
- Planning conclusion: if the team chooses to pursue compatibility restoration anyway, the narrowest likely fix surface remains `src\Refitter.Core\OperationNameGenerator.cs`, with minimum regression coverage split across existing core naming scenarios plus a dedicated `Refitter.SourceGenerator.Tests` compile-backed repro in the reporter's dependency lane.
