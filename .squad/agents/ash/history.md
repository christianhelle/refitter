# Ash History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Added to the squad on 2026-04-20 as a specialist reviewer for PR #1064 / #1057 safety gates.
- For suffix rewrites, block both source-name corruption and suffix-target collisions (Pet + PetDto cannot both land on PetDto).
- For multipart/query parameter extraction, deduplicate by the emitted C# identifier, not the original OpenAPI key.
- For source-generator packaging reviews, inspect the packed .nuspec and analyzer payload, not just the .csproj.
- The minimal safe Swagger 2 nullable-shape fix for #1026 lives in src/Refitter.Core/RefitGenerator.cs as targeted post-processing when NRT is enabled but optional-nullable generation stays opt-in.
- `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs` still has a same-directory hint-name collision risk because `CreateUniqueHintName(...)` hashes only the parent directory, not the full `.refitter` path.
- In the source generator, treat `AdditionalText.GetText(...)` as nullable and convert read/encoding failures into diagnostics; null-forgiving it weakens failure reporting.
- Review gate for the next safety pass: require targeted tests for source-generator hint-name collisions, OpenAPI-title sanitization including `<`/`>`, and MSBuild runtime-discovery timeout behavior before accepting cleanup claims.
- 2026-04-28: Approved Parker/Lambert e-conomic multi-spec merge fix after verifying canonical JSON-token equivalence accepts duplicate recursive/shared schemas, conflicting duplicate path/schema/definition/security entries still fail fast, and focused net10.0 TUnit coverage passes.
- 2026-04-29T10:41:29.997+02:00: `src\Refitter.Core\OpenApiDocumentFactory.cs` should clone merge baselines with `OpenApiDocument.ToJson()` plus `OpenApiDocument.FromJsonAsync(...)`; the parameterless serializer preserves `SchemaType` for both Swagger 2 and OpenAPI 3 while avoiding obsolete `ToJson(SchemaType)` usage.
- 2026-04-29T10:41:29.997+02:00: The safety proof for this obsolete-API lane lives in `src\Refitter.Tests\OpenApiDocumentFactoryMergeTests.cs`: round-trip embedded Swagger Petstore V2/V3 fixtures, then assert mixed-version merges keep the base document schema type and serialized top-level marker (`swagger` vs `openapi`).
- 2026-04-29T10:41:29.997+02:00: For focused core-lane validation in this repo, `src\Refitter.Tests\bin\Release\net10.0\Refitter.Tests.exe --treenode-filter '/*/Refitter.Tests/OpenApiDocumentFactoryMergeTests/*' --disable-logo --no-progress --no-ansi` reliably runs the merge test class when `dotnet test --filter ...` is unavailable under TUnit.

## Core Context

- **PR #1064 safety review:** Rejected the initial blocker set until #1013 collision detection and #1018 sanitized-identifier deduplication were fully closed; treated #1053 as safe once keyword escaping routed through EscapeReservedKeyword().
- **Blocker-fix verification:** Confirmed the eventual #1013, #1018, and #1053 fixes once collision handling, naming consistency, and test expectations aligned.
- **#1024 packaging closure:** Verified that PrivateAssets="all" is required to keep Refit out of the packed source-generator nuspec, then aligned README/docs guidance around explicit consumer references.

## 2026-04-25: Core Review Chain

- Rejected Parker's closure set and kept **#1034** and **#1039** open as real blockers.
- Cleared **#1039** once grouped query-parameter extraction stopped mutating operationModel.Parameters and the private coverage locked that invariant.
- Rejected Dallas's first merge revision because OpenApiDocumentFactory.Merge() still warned and kept the first conflicting entry instead of failing fast.
- Rejected Lambert's later proof pass because the Swagger 2 definition-collision evidence still tripped the mirrored schema conflict before isolating the intended definition lane.
- Queued Ripley for the final narrow proof-gap revision once Parker, Dallas, and Lambert were all locked out.

## 2026-04-25: Final Signoff on Ripley Follow-up

- Approved the final #1034 / #1039 follow-up after Ripley preserved the source schema type during clone and isolated the Swagger 2 definition-collision proof through MergeIfMissingOrThrowOnConflict(...).
- Signed off that merge handling now stays clone-first, fails fast on conflicting duplicate path/schema/definition/security keys, and keeps grouped dynamic-query extraction non-mutating across single-interface, ByTag, and ByEndpoint generation.
- Evidence reviewed included src\Refitter.Core\OpenApiDocumentFactory.cs, src\Refitter.Tests\OpenApiDocumentFactoryMergeTests.cs, src\Refitter.Tests\RegressionTests\Issue1039_DynamicQuerystringMutationTests.cs, and src\Refitter.Tests\ParameterExtractorPrivateCoverageTests.cs.
- Final reviewer gate was reported green on dotnet test -c Release src\Refitter.Tests\Refitter.Tests.csproj with 1840 passing and 0 failing.

## 2026-04-25: SonarCloud PR #1070 gate review

- Reviewed SonarCloud PR #1070 quality-gate output: 5 findings total, with the gate failing only because Sonar classified `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs` `GeneratedDiagnostic` as a BUG (`S1206`).
- Treat the `S1206` finding as analyzer noise, not a real product bug: `GeneratedDiagnostic` is a `record struct`, so changing equality members just to satisfy Sonar would risk destabilizing incremental-generator equality semantics without fixing an observed defect.
- Treat the remaining findings in `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs`, `src\Refitter.Core\ParameterExtractor.cs`, and `src\Refitter.MSBuild\RefitterGenerateTask.cs` as maintainability-only/style noise rather than product or test failures.
- Risk guidance: avoid style-only rewrites in the dynamic querystring extraction and MSBuild runtime-resolution/timeout-formatting paths, because those areas were recently hardened and a cleanup-only patch could regress real behavior while merely silencing Sonar.

## 2026-04-25: Review of landed Sonar cleanup

- Re-reviewed the landed cleanup on the three implicated files. The `S1066`, `S3267`, `S3358`, and `S1192` changes are acceptable as behavior-preserving cleanup only.
- Rejected the `S1206` fix direction: converting `GeneratedDiagnostic` from `readonly record struct` to a manual `readonly struct` is unnecessary churn for a likely Sonar false positive and weakens the safety story by making equality/hash maintenance manual.
- Gate guidance for Dallas: request changes only on the source-generator `GeneratedDiagnostic` rewrite; the other four Sonar items do not justify blocking once they stay narrow and behavior-preserving.

## 2026-04-25: Parker follow-up on source-generator Sonar fix

- Approved Parker's revised `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs` artifact.
- The safe correction was restoring `GeneratedDiagnostic` to `readonly record struct` and suppressing `S1206` with an explicit justification, which preserves synthesized equality while keeping the custom ordinal `GetHashCode()` implementation.
- The `S1192` cleanup remains cosmetic and safe, and no new generator-safety regression is evident in the revised source-generator artifact.
## 2026-04-25: Scribe consolidation of PR #1070

- Final squad memory keeps Dallas's ParameterExtractor / RefitterGenerateTask cleanups and the source-generator diagnostic-ID cleanup as the approved behavior-preserving Sonar response.
- The only rejected direction was the first manual-struct S1206 rewrite; Parker's follow-up restored the readonly record struct shape and was the approved final source-generator artifact.
- Shared validation to cite for the consolidated PR #1070 outcome: dotnet build -c Release src\Refitter.slnx --no-restore, dotnet test -c Release --solution src\Refitter.slnx --no-build, and dotnet format --verify-no-changes src\Refitter.slnx --no-restore.

## 2026-04-26: AI-slop safety triage

- High-confidence source-generator issues from the read-only review: same-directory `.refitter` files can still collide on `AddSource()` hint names, and `file.GetText(cancellationToken)!` can convert input-read failures into opaque crashes instead of diagnostics.
- High-confidence core-generation issues from the same pass: `RefitMultipleInterfaceGenerator` still derives `QueryParams` types via `interfaceName.Replace("I", string.Empty)`, and `IdentifierUtils.Sanitize()` still misses angle brackets for OpenAPI-title-derived identifiers used by `RefitInterfaceGenerator` and `JsonSerializerContextGenerator`.
- High-confidence MSBuild issue from the same pass: `RefitterGenerateTask.GetInstalledDotnetRuntimes()` still blocks on `dotnet --list-runtimes` without any timeout, so build hangs remain possible before the guarded process-runner path starts.

## 2026-04-26: Shared cleanup gate context

- Ripley's sequencing and Lambert's green baseline keep docs/help drift as the safe first cleanup lane, but Ash's gate still holds the deeper generator and MSBuild cleanup as validation-first work.
- Dallas's docs clarification commit `f6374210` landed cleanly, so the next review concern stays on hint-name uniqueness, null-safe source-generator input handling, identifier sanitization, and MSBuild runtime-discovery timeout coverage.

## 2026-04-29: OpenApiDocument Clone Path Obsolete API Approval

- **2026-04-29T08:41:29Z Final Safety Approval:** Confirmed `src\Refitter.Core\OpenApiDocumentFactory.cs` clone path is safe to migrate from obsolete `ToJson(SchemaType)` to parameterless `ToJson()` + `FromJsonAsync()` round-trip.
- **Evidence Verified:** 
  - Schema-type preservation across round-trip serialization for Swagger 2 and OpenAPI 3
  - Merge contract integrity (clone-first, fail-fast on genuine conflicts)
  - Focused `OpenApiDocumentFactoryMergeTests` covering embedded Swagger Petstore V2/V3 fixtures
  - Mixed-version merge assertions confirming correct schema type and top-level markers
- **Validation Results:** Release build ✓, full Refitter.Tests ✓, Refitter.SourceGenerator.Tests ✓, format verification ✓
- **Outcome:** APPROVED for production fix. Obsolete API successfully removed from merge-critical clone path while maintaining all semantic guarantees.
