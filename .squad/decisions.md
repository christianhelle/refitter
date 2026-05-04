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
