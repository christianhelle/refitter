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
