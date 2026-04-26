# Parker History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- 2026-04-20: JsonSerializerContextGenerator must use Roslyn syntax analysis instead of regex, emit attributes inside the contracts namespace, register nested types plus closed generic usages, and strip a conventional leading I from serializer-context names.
- 2026-04-20: GenerateJsonSerializerContext is wired through both RefitGenerator.Generate() and GenerateMultipleFiles(); multi-file generation emits a dedicated {ContextName}.cs file alongside contracts.
- 2026-04-20: GenerateNullableReferenceTypes must not silently flip GenerateOptionalPropertiesAsNullable; optional-property nullability stays an explicit user choice in CodeGeneratorSettings.
- 2026-04-26: Lowest-risk generator cleanup candidates are XmlDocumentationGenerator.AppendXmlCommentBlock() redundant multi-line splitting and ContractTypeSuffixApplier.TypeSuffixRewriter's repeated Visit*Declaration renaming branches; both already sit behind strong regression coverage.
- 2026-04-26: Do not refactor ParameterExtractor or interface-emission flow until compile-backed public regressions cover multipart generation and dynamic-query behavior; ParameterExtractorPrivateCoverageTests currently leans on reflection plus RuntimeHelpers.GetUninitializedObject and is too implementation-coupled to be the only safety net.
- 2026-04-26: The generator pipeline is duplicated between src\Refitter.Core\RefitGenerator.cs Generate()/GenerateMultipleFiles() and among src\Refitter.Core\RefitInterfaceGenerator.cs, RefitMultipleInterfaceGenerator.cs, and RefitMultipleInterfaceByTagGenerator.cs; treat those as Ash-review cleanups because ordering and emitted signature shape are behavior-sensitive.

## Core Context

- **Breaking-change audit:** the durable v2 break was the settings-model change from generateAuthenticationHeader: bool to authenticationHeaderStyle: AuthenticationHeaderStyle; treat that as a hard compatibility break that justified the 2.0 line.
- **P0/P1 audit patterns:** source-generator hint names must disambiguate by path, MSBuild tasks must fail the build on CLI/process errors, regex replacement on raw generated source is fragile, Swagger/OpenAPI traversals must null-check Components/Schemas, and multi-spec merge logic must not silently drop later document state.
- **Runtime / compatibility patterns:** library awaits should use ConfigureAwait(false), static HttpClient setup needs explicit timeout plus User-Agent, null response content must be handled, duplicate operation IDs should short-circuit through HashSet.Add, and fragile generator ordering belongs in a helper rather than being duplicated across entry points.
- **Identifier / signature patterns:** route emitted identifiers through IdentifierUtils.ToCompilableIdentifier(), sanitize after final string composition, use this. for dynamic-query constructor assignments, detect nullable parameters at the tail of the type declaration, prefer string.Join() for potentially empty namespace lists, and only append ? to custom reference types when NRT is enabled.
- **PR #1064 lessons:** suffix collision checks must consider the final target name, multipart/query dedup should happen on the emitted C# identifier, reserved-keyword escaping is a final-step concern, and source-generator/package review needs the packed .nuspec plus analyzer payload instead of only the project file.
- **#1057 core-lane state:** keep #1032 validation-first, #1056 doc/invariant-only, #1045 fixed at HEAD, and #1033 closed via the newline-safe enum-converter hardening. Only treat #1034 / #1039 as closed when merge handling is clone-first and fail-fast on conflicting duplicate path/schema/definition/security keys while dynamic-query extraction stays non-mutating for downstream XML-doc generation.
- **Lockout chain:** Parker's initial no-code closure set for the late #1057 core artifact was rejected, which moved the follow-up through Dallas, Lambert, and Ripley before Ash gave final approval on the isolated #1034 / #1039 proof.

## 2026-04-25: PR #1070 Sonar source-generator revision

**Task:** Rework the RefitterSourceGenerator.cs Sonar fix so the source-generator lane keeps the GeneratedDiagnostic record-struct shape while satisfying the quality gate and Ash's review constraint.  
**Status:** COMPLETE — source-generator-only revision landed and reviewer-approved.

**Implementation Summary:**
- Kept the S1192 cleanup in src\Refitter.SourceGenerator\RefitterSourceGenerator.cs by reusing the shared Refitter diagnostic-title constant and assigning distinct diagnostic IDs for the found-file and file-contents info diagnostics.
- Restored GeneratedDiagnostic to a readonly record struct, preserved the explicit ordinal GetHashCode() implementation, and suppressed Sonar S1206 on the type because record structs already synthesize the paired Equals overloads.
- Left Dallas's src\Refitter.Core\ParameterExtractor.cs and src\Refitter.MSBuild\RefitterGenerateTask.cs changes untouched.

**Validation Notes:**
- dotnet build -c Release src\Refitter.slnx --no-restore
- dotnet test -c Release --solution src\Refitter.slnx --no-build
- dotnet format --verify-no-changes src\Refitter.slnx --no-restore

## 2026-04-25: Scribe consolidation of PR #1070

- Dallas's ParameterExtractor / RefitterGenerateTask cleanups remain the approved behavior-preserving response for S1066, S3267, and S3358.
- Ash explicitly rejected only the first manual-struct S1206 direction; Parker's revision replaced that one artifact and became the final approved source-generator state.
- The merged squad decision now records the stable diagnostic-ID contract, the preserved readonly record struct shape, and the shared build/test/format validation for PR #1070.

## 2026-04-26: Shared cleanup context

- Ripley's AI-slop sequencing kept Parker's low-risk-first generator guidance intact: docs/help drift first, then settings/marker cleanup, and only later Ash-reviewed generator dedup.
- Lambert's baseline scan confirmed the repo is currently green on restore/build/test/format, which keeps compile-backed regression work as the gate before any deeper generator cleanup.
