# Changelog

## [Unreleased](https://github.com/christianhelle/refitter/tree/HEAD)

[Full Changelog](https://github.com/christianhelle/refitter/compare/2.0.0-preview.106...HEAD)

**Implemented enhancements:**

- Handle equivalent duplicate schemas in multi-spec merge [\#1076](https://github.com/christianhelle/refitter/pull/1076) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Multi-spec merge fails on equivalent duplicate schemas [\#1075](https://github.com/christianhelle/refitter/issues/1075)
- \[v2.0 audit\]\[L13\] IdentifierUtils.Counted uses fresh HashSet per GenerateCode\(\) call [\#1056](https://github.com/christianhelle/refitter/issues/1056)
- \[v2.0 audit\]\[L4\] MSBuild task does regex-based JSON parsing of .refitter files [\#1047](https://github.com/christianhelle/refitter/issues/1047)
- \[v2.0 audit\]\[L2\] OpenApiPath remains null! when OpenApiPaths is set → NRE for library consumers [\#1045](https://github.com/christianhelle/refitter/issues/1045)
- \[v2.0 audit\]\[M15\] Spectre.Console.Cli 0.53 → 0.55 pre-1.0 minor bump may shift CLI parsing [\#1042](https://github.com/christianhelle/refitter/issues/1042)
- \[v2.0 audit\]\[M5\] InlineJsonConverters semantics silently changed: per-property → per-type \(custom JsonNamingPolicy regression risk\) [\#1032](https://github.com/christianhelle/refitter/issues/1032)

**Merged pull requests:**

- docs: add AntonTeyken as a contributor for bug [\#1077](https://github.com/christianhelle/refitter/pull/1077) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update actions/github-script action to v9 [\#1073](https://github.com/christianhelle/refitter/pull/1073) ([renovate[bot]](https://github.com/apps/renovate))
- Update actions/checkout action to v6 [\#1072](https://github.com/christianhelle/refitter/pull/1072) ([renovate[bot]](https://github.com/apps/renovate))
- Harden generation flows [\#1071](https://github.com/christianhelle/refitter/pull/1071) ([christianhelle](https://github.com/christianhelle))
- Breaking changes and Migration Guide for v2.0.0 [\#1009](https://github.com/christianhelle/refitter/pull/1009) ([christianhelle](https://github.com/christianhelle))

## [2.0.0-preview.106](https://github.com/christianhelle/refitter/tree/2.0.0-preview.106) (2026-04-25)

[Full Changelog](https://github.com/christianhelle/refitter/compare/2.0.0-preview.105...2.0.0-preview.106)

**Fixed bugs:**

- \[v2.0 audit\]\[M16\] CLI --generate-authentication-header changed from bool flag to enum value \(silent script breakage\) [\#1043](https://github.com/christianhelle/refitter/issues/1043)
- \[v2.0 audit\]\[M14\] MSBuild task: dotnet --list-runtimes invocation is fragile \(NRE, unquoted args, no fallback when target TFM dll missing\) [\#1041](https://github.com/christianhelle/refitter/issues/1041)
- \[v2.0 audit\]\[M12\] GetQueryParameters mutates the shared operationModel.Parameters collection [\#1039](https://github.com/christianhelle/refitter/issues/1039)
- \[v2.0 audit\]\[M7\] Merge mutates input documents\[0\] and silently drops conflicting paths/schemas without warning [\#1034](https://github.com/christianhelle/refitter/issues/1034)
- \[v2.0 audit\]\[M6\] Hard-coded \n in regex replacement → mixed CRLF/LF on Windows [\#1033](https://github.com/christianhelle/refitter/issues/1033)
- \[v2.0 audit\]\[M2\] SourceGenerator user-visible warnings only emit to Debug.WriteLine \(no-op in Release\) [\#1029](https://github.com/christianhelle/refitter/issues/1029)
- \[v2.0 audit\]\[M1\] SourceGenerator pipeline output uses List\<Diagnostic\> \(defeats incremental caching\) [\#1028](https://github.com/christianhelle/refitter/issues/1028)
- \[v2.0 audit\]\[H10\] Auto-enabling GenerateOptionalPropertiesAsNullable is a silent breaking shape change [\#1026](https://github.com/christianhelle/refitter/issues/1026)
- \[v2.0 audit\]\[H8\] Refit major bump 9 → 10 silently leaks to Refitter.SourceGenerator consumers [\#1024](https://github.com/christianhelle/refitter/issues/1024)
- \[v2.0 audit\]\[H7\] MSBuild IncludePatterns is substring-matched, over-includes files [\#1023](https://github.com/christianhelle/refitter/issues/1023)
- \[v2.0 audit\]\[H6\] MSBuild predicted output paths diverge from CLI actual paths → silent missing compile items [\#1022](https://github.com/christianhelle/refitter/issues/1022)
- \[v2.0 audit\]\[H1\] JsonSerializerContextGenerator emits non-compiling AOT context \(generics, namespaces, polymorphism, nested types\) [\#1017](https://github.com/christianhelle/refitter/issues/1017)

**Merged pull requests:**

- \[v2.0 audit\] Close remaining verified \#1057 regressions [\#1070](https://github.com/christianhelle/refitter/pull/1070) ([christianhelle](https://github.com/christianhelle))
- Resolve high-severity audit findings from \#1057 [\#1067](https://github.com/christianhelle/refitter/pull/1067) ([christianhelle](https://github.com/christianhelle))
- Update nswag monorepo to 14.7.1 [\#1065](https://github.com/christianhelle/refitter/pull/1065) ([renovate[bot]](https://github.com/apps/renovate))

## [2.0.0-preview.105](https://github.com/christianhelle/refitter/tree/2.0.0-preview.105) (2026-04-21)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.8.0-preview.103...2.0.0-preview.105)

**Fixed bugs:**

- \[v2.0 audit\]\[L12\] Reordering of interface-generator construction in Generate\(\) is correct but fragile [\#1055](https://github.com/christianhelle/refitter/issues/1055)
- \[v2.0 audit\]\[L11\] OpenApiDocumentFactory.CreateAsync\(IEnumerable\<string\>\) throws wrong exception type on null [\#1054](https://github.com/christianhelle/refitter/issues/1054)
- \[v2.0 audit\]\[L10\] IdentifierUtils.ReservedKeywords incomplete; Sanitize\(\) does not escape keywords [\#1053](https://github.com/christianhelle/refitter/issues/1053)
- \[v2.0 audit\]\[L9\] OperationNameGenerator.CheckForDuplicateOperationIds runs full pipeline twice in constructor [\#1052](https://github.com/christianhelle/refitter/issues/1052)
- \[v2.0 audit\]\[L8\] XmlDocumentationGenerator.DecodeJsonEscapedText mishandles malformed \u sequences [\#1051](https://github.com/christianhelle/refitter/issues/1051)
- \[v2.0 audit\]\[L7\] Enum-deserialization errors in .refitter give unhelpful JsonException [\#1050](https://github.com/christianhelle/refitter/issues/1050)
- \[v2.0 audit\]\[L6\] Library code missing ConfigureAwait\(false\) — sync-over-async deadlock risk for hosted callers [\#1049](https://github.com/christianhelle/refitter/issues/1049)
- \[v2.0 audit\]\[L5\] CLI reads .refitter twice \(validator + execute\), risks drift [\#1048](https://github.com/christianhelle/refitter/issues/1048)
- \[v2.0 audit\]\[L3\] Default OpenApiPaths = Array.Empty\<string\>\(\) round-trips into saved settings as "openApiPaths": \[\] [\#1046](https://github.com/christianhelle/refitter/issues/1046)
- \[v2.0 audit\]\[L1\] OpenApiPath + OpenApiPaths precedence is silent; no validation [\#1044](https://github.com/christianhelle/refitter/issues/1044)
- \[v2.0 audit\]\[M13\] Static HttpClient has no timeout, no cancellation, no User-Agent [\#1040](https://github.com/christianhelle/refitter/issues/1040)
- \[v2.0 audit\]\[M11\] CustomCSharpTypeResolver appends ? to mapped reference-type aliases regardless of nullable-reference-type setting [\#1038](https://github.com/christianhelle/refitter/issues/1038)
- \[v2.0 audit\]\[M10\] RefitInterfaceImports.GenerateNamespaceImports throws on empty namespace list [\#1037](https://github.com/christianhelle/refitter/issues/1037)
- \[v2.0 audit\]\[M9\] ReOrderNullableParameters mis-classifies generic parameters that contain ? [\#1036](https://github.com/christianhelle/refitter/issues/1036)
- \[v2.0 audit\]\[M8\] XML doc emission does not escape user-supplied parameter / dynamic-querystring descriptions [\#1035](https://github.com/christianhelle/refitter/issues/1035)
- \[v2.0 audit\]\[M4\] ValidateOpenApiSpec does not resolve relative spec paths from settings-file directory [\#1031](https://github.com/christianhelle/refitter/issues/1031)
- \[v2.0 audit\]\[M3\] SettingsValidator only validates the first entry of openApiPaths [\#1030](https://github.com/christianhelle/refitter/issues/1030)
- \[v2.0 audit\]\[H11\] RefitInterfaceGenerator NRE when an OpenAPI response has no content [\#1027](https://github.com/christianhelle/refitter/issues/1027)
- \[v2.0 audit\]\[H9\] Microsoft.OpenApi.Readers 1.x → 3.x silently changes parsing/codegen for users [\#1025](https://github.com/christianhelle/refitter/issues/1025)
- \[v2.0 audit\]\[H5\] CLI: --output / -o no longer overrides settings-file outputFolder \(script regression\) [\#1021](https://github.com/christianhelle/refitter/issues/1021)
- \[v2.0 audit\]\[H4\] Dynamic-querystring constructor self-assigns when parameter name starts with non-letter [\#1020](https://github.com/christianhelle/refitter/issues/1020)
- \[v2.0 audit\]\[H3\] Security-scheme header parameter name not safely sanitized [\#1019](https://github.com/christianhelle/refitter/issues/1019)
- \[v2.0 audit\]\[H2\] ParameterExtractor.ConvertToVariableName produces invalid C\# identifiers \(multipart form-data fields\) [\#1018](https://github.com/christianhelle/refitter/issues/1018)
- \[v2.0 audit\]\[C6\] Multi-spec merge silently drops all schemas when first spec has no components [\#1016](https://github.com/christianhelle/refitter/issues/1016)
- \[v2.0 audit\]\[C5\] ConvertOneOfWithDiscriminatorToAllOf NRE on Swagger 2 / OpenAPI 3 docs without components [\#1015](https://github.com/christianhelle/refitter/issues/1015)
- \[v2.0 audit\]\[C4\] Forced JsonStringEnumConverter injection breaks Newtonsoft users and silently regresses internal enums [\#1014](https://github.com/christianhelle/refitter/issues/1014)
- \[v2.0 audit\]\[C3\] ContractTypeSuffixApplier corrupts code via raw word-boundary regex \(renames members, comments, strings; double-suffix on rerun\) [\#1013](https://github.com/christianhelle/refitter/issues/1013)
- \[v2.0 audit\]\[C2\] MSBuild task swallows CLI failures; build always reports success with stale/missing output [\#1012](https://github.com/christianhelle/refitter/issues/1012)
- \[v2.0 audit\]\[C1\] SourceGenerator: hint-name collisions when two .refitter files share a filename [\#1011](https://github.com/christianhelle/refitter/issues/1011)

**Merged pull requests:**

- \[v2.0 audit\] Fix pre-release regressions from \#1057 [\#1064](https://github.com/christianhelle/refitter/pull/1064) ([christianhelle](https://github.com/christianhelle))
- Update dependency coverlet.collector to v10 [\#1007](https://github.com/christianhelle/refitter/pull/1007) ([renovate[bot]](https://github.com/apps/renovate))
- Update actions/github-script action to v9 [\#1006](https://github.com/christianhelle/refitter/pull/1006) ([renovate[bot]](https://github.com/apps/renovate))
- Update actions/checkout action to v6 [\#1005](https://github.com/christianhelle/refitter/pull/1005) ([renovate[bot]](https://github.com/apps/renovate))
- Upgrade Squad to v0.9.1 [\#1004](https://github.com/christianhelle/refitter/pull/1004) ([christianhelle](https://github.com/christianhelle))

## [1.8.0-preview.103](https://github.com/christianhelle/refitter/tree/1.8.0-preview.103) (2026-04-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.8.0-preview.102...1.8.0-preview.103)

**Implemented enhancements:**

- Harden .refitter settings deserialization and output path handling [\#1000](https://github.com/christianhelle/refitter/pull/1000) ([christianhelle](https://github.com/christianhelle))
- Verify alias handling and sanitize PascalCase properties [\#997](https://github.com/christianhelle/refitter/pull/997) ([christianhelle](https://github.com/christianhelle))
- Enhance schema alias handling and property name generation [\#996](https://github.com/christianhelle/refitter/pull/996) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Refitter.MSBuild 1.7.3 on .NET 10 ignores output file path [\#998](https://github.com/christianhelle/refitter/issues/998)
- OpenApi client generates invalid schema property name if it starts with a digit [\#992](https://github.com/christianhelle/refitter/issues/992)
- OpenApi client generate fails with System.ArgumentException: An item with the same key has already been added [\#991](https://github.com/christianhelle/refitter/issues/991)

**Merged pull requests:**

- Update dependency TUnit to 1.35.2 [\#1003](https://github.com/christianhelle/refitter/pull/1003) ([renovate[bot]](https://github.com/apps/renovate))
- Special-case the default .refitter filename before deriving OutputFilename [\#1002](https://github.com/christianhelle/refitter/pull/1002) ([coderabbitai[bot]](https://github.com/apps/coderabbitai))
- CodeRabbit auto-fixes for PR \#1000 [\#1001](https://github.com/christianhelle/refitter/pull/1001) ([coderabbitai[bot]](https://github.com/apps/coderabbitai))
- docs: add Timovzl as a contributor for bug [\#999](https://github.com/christianhelle/refitter/pull/999) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update dependency TUnit to 1.34.5 [\#995](https://github.com/christianhelle/refitter/pull/995) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add dimyle as a contributor for bug [\#994](https://github.com/christianhelle/refitter/pull/994) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update dependency TUnit to 1.31.0 [\#989](https://github.com/christianhelle/refitter/pull/989) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency AutoMapper to v16 [\#988](https://github.com/christianhelle/refitter/pull/988) ([renovate[bot]](https://github.com/apps/renovate))
- Bump the nuget group with 1 update [\#987](https://github.com/christianhelle/refitter/pull/987) ([dependabot[bot]](https://github.com/apps/dependabot))
- Update Spectre.Console.Cli to v0.55.0 [\#985](https://github.com/christianhelle/refitter/pull/985) ([christianhelle](https://github.com/christianhelle))
- Update actions/github-script action to v9 [\#984](https://github.com/christianhelle/refitter/pull/984) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency TUnit to 1.30.8 [\#983](https://github.com/christianhelle/refitter/pull/983) ([renovate[bot]](https://github.com/apps/renovate))
- Update NSwag to v14.7.0 [\#980](https://github.com/christianhelle/refitter/pull/980) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.29.0 [\#976](https://github.com/christianhelle/refitter/pull/976) ([renovate[bot]](https://github.com/apps/renovate))

## [1.8.0-preview.102](https://github.com/christianhelle/refitter/tree/1.8.0-preview.102) (2026-04-01)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.8.0-preview.101...1.8.0-preview.102)

**Implemented enhancements:**

- Made Header Parameters for Security Schemes safe to use as C\# variable name [\#977](https://github.com/christianhelle/refitter/pull/977) ([smoerijf](https://github.com/smoerijf))

**Merged pull requests:**

- chore\(deps\): update dependency tunit to 1.22.6 [\#975](https://github.com/christianhelle/refitter/pull/975) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.22.3 [\#974](https://github.com/christianhelle/refitter/pull/974) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dotnet monorepo [\#960](https://github.com/christianhelle/refitter/pull/960) ([renovate[bot]](https://github.com/apps/renovate))

## [1.8.0-preview.101](https://github.com/christianhelle/refitter/tree/1.8.0-preview.101) (2026-03-27)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.8.0-preview.100...1.8.0-preview.101)

**Implemented enhancements:**

- Fix recursive schema stack overflows [\#971](https://github.com/christianhelle/refitter/pull/971) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- StackOverflowException in recursive schema traversal [\#973](https://github.com/christianhelle/refitter/issues/973)
- how to keep contract Property Name as original name without Serialization ? [\#967](https://github.com/christianhelle/refitter/issues/967)

**Merged pull requests:**

- Bump codecov/codecov-action from 5 to 6 [\#972](https://github.com/christianhelle/refitter/pull/972) ([dependabot[bot]](https://github.com/apps/dependabot))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 10.1.7 [\#966](https://github.com/christianhelle/refitter/pull/966) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.21.30 [\#965](https://github.com/christianhelle/refitter/pull/965) ([renovate[bot]](https://github.com/apps/renovate))

## [1.8.0-preview.100](https://github.com/christianhelle/refitter/tree/1.8.0-preview.100) (2026-03-25)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.8.0-preview.99...1.8.0-preview.100)

**Implemented enhancements:**

- Add PropertyNamingPolicy support for JSON property naming [\#969](https://github.com/christianhelle/refitter/pull/969) ([christianhelle](https://github.com/christianhelle))
- chore\(deps\): update dependency tunit to 1.20.0 [\#962](https://github.com/christianhelle/refitter/pull/962) ([renovate[bot]](https://github.com/apps/renovate))

**Merged pull requests:**

- docs: add naji-makhoul as a contributor for ideas [\#968](https://github.com/christianhelle/refitter/pull/968) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency tunit to 1.21.0 [\#964](https://github.com/christianhelle/refitter/pull/964) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency oasreader to 3.5.0.19 [\#963](https://github.com/christianhelle/refitter/pull/963) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update refit monorepo to 10.1.6 [\#961](https://github.com/christianhelle/refitter/pull/961) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency coverlet.collector to 8.0.1 [\#959](https://github.com/christianhelle/refitter/pull/959) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency ruby to v4.0.2 [\#958](https://github.com/christianhelle/refitter/pull/958) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 10.1.5 [\#952](https://github.com/christianhelle/refitter/pull/952) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.19.74 [\#951](https://github.com/christianhelle/refitter/pull/951) ([renovate[bot]](https://github.com/apps/renovate))

## [1.8.0-preview.99](https://github.com/christianhelle/refitter/tree/1.8.0-preview.99) (2026-03-08)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.7.3...1.8.0-preview.99)

**Implemented enhancements:**

- Support custom format-mappings via Key-Value configuration [\#438](https://github.com/christianhelle/refitter/issues/438)
- Generate the client from multiple versions [\#350](https://github.com/christianhelle/refitter/issues/350)
- Config parameter to add suffix to contract types [\#193](https://github.com/christianhelle/refitter/issues/193)
- Add SourceGenerator support to the standalone tool [\#179](https://github.com/christianhelle/refitter/issues/179)
- Add Unicode support for XML doc comment generation [\#948](https://github.com/christianhelle/refitter/pull/948) ([christianhelle](https://github.com/christianhelle))
- Fix broken CLI tool help text [\#940](https://github.com/christianhelle/refitter/pull/940) ([christianhelle](https://github.com/christianhelle))
- Move \[JsonConverter\] from enum properties to enum types [\#938](https://github.com/christianhelle/refitter/pull/938) ([christianhelle](https://github.com/christianhelle))
- Fix PR \#897 review feedback and add comprehensive bearer auth tests [\#936](https://github.com/christianhelle/refitter/pull/936) ([christianhelle](https://github.com/christianhelle))
- Fix SonarCloud Code Quality Issues [\#932](https://github.com/christianhelle/refitter/pull/932) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Fix multipart form-data parameter extraction [\#928](https://github.com/christianhelle/refitter/pull/928) ([christianhelle](https://github.com/christianhelle))
- Add custom format mappings configuration [\#927](https://github.com/christianhelle/refitter/pull/927) ([christianhelle](https://github.com/christianhelle))
- Fix \#635: Refactor source generator to use context.AddSource\(\) [\#923](https://github.com/christianhelle/refitter/pull/923) ([christianhelle](https://github.com/christianhelle))
- Fix \#672: MultipleInterfaces ByTag method naming scoped per-interface [\#922](https://github.com/christianhelle/refitter/pull/922) ([christianhelle](https://github.com/christianhelle))
- Fix \#580: Nullable strings marked correctly [\#921](https://github.com/christianhelle/refitter/pull/921) ([christianhelle](https://github.com/christianhelle))
- Fix numeric suffix added to interface method names in ByTag mode [\#914](https://github.com/christianhelle/refitter/pull/914) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Migrate from Microsoft.OpenApi.Readers 1.x to Microsoft.OpenApi 3.x [\#907](https://github.com/christianhelle/refitter/pull/907) ([vgmello](https://github.com/vgmello))
- Fix: Base type not generated for types using oneOf with discriminator [\#906](https://github.com/christianhelle/refitter/pull/906) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Add support for generating a single client from multiple OpenAPI specifications [\#904](https://github.com/christianhelle/refitter/pull/904) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Add option for Method Level Authorization header attribute [\#897](https://github.com/christianhelle/refitter/pull/897) ([Roflincopter](https://github.com/Roflincopter))
- Update refit monorepo to v10 \(major\) [\#893](https://github.com/christianhelle/refitter/pull/893) ([renovate[bot]](https://github.com/apps/renovate))
- Fix null reference and XML escaping in XmlDocumentationGenerator [\#890](https://github.com/christianhelle/refitter/pull/890) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Read group documentation from document tags. [\#887](https://github.com/christianhelle/refitter/pull/887) ([DJ4ddi](https://github.com/DJ4ddi))
- Fix numeric format with pattern quirk - infer type from format for all numeric types [\#869](https://github.com/christianhelle/refitter/pull/869) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Add debug logging for source generator when searching for .refitter files [\#743](https://github.com/christianhelle/refitter/pull/743) ([codymullins](https://github.com/codymullins))

**Fixed bugs:**

- Non-ASCII response descriptions become \uXXXX in XML comments [\#944](https://github.com/christianhelle/refitter/issues/944)
- Resolve SonarCloud Code Quality Issues [\#931](https://github.com/christianhelle/refitter/issues/931)
- format: int32 quirk =\> Serializes to "object" when open api spec contains pattern [\#867](https://github.com/christianhelle/refitter/issues/867)
- Failing CLI example with confusing error message [\#847](https://github.com/christianhelle/refitter/issues/847)
- .refitter - "mutipleInterfaces": "ByTag" increments number at the end of Method Name globally instead of being related to its interface [\#672](https://github.com/christianhelle/refitter/issues/672)
- Build errors when combined with `Microsoft.Extensions.ApiDescription.Server` [\#635](https://github.com/christianhelle/refitter/issues/635)
- Nullable Strings not being marked correctly [\#580](https://github.com/christianhelle/refitter/issues/580)
- Refitter failed to write generated code: System.IO.IOException: The process cannot access the file 'Generated.cs' because it is being used by another process. [\#520](https://github.com/christianhelle/refitter/issues/520)
- Code Generator adds numeric suffix to Interface method name when not needed [\#361](https://github.com/christianhelle/refitter/issues/361)
- Source generator does not work in .Net 8 [\#310](https://github.com/christianhelle/refitter/issues/310)
- Hyphens with JsonStringEnumConverter results in `JSON value could not be converted` [\#300](https://github.com/christianhelle/refitter/issues/300)
- The client parameter type's names occur wrong when multipart is include. [\#231](https://github.com/christianhelle/refitter/issues/231)
- Multipart endpoint \[FromForm\] decorated argument is missing from signature [\#222](https://github.com/christianhelle/refitter/issues/222)
- The using of StringEnumConverter end up generating unserializable data when different NamingPolicy is needed [\#178](https://github.com/christianhelle/refitter/issues/178)
- Base type not generated for types specified by oneOf in the schema [\#175](https://github.com/christianhelle/refitter/issues/175)

**Merged pull requests:**

- docs: add send0xx as a contributor for bug [\#946](https://github.com/christianhelle/refitter/pull/946) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Microsoft.OpenApi v3.4 [\#945](https://github.com/christianhelle/refitter/pull/945) ([christianhelle](https://github.com/christianhelle))
- chore\(deps\): update dependency tunit to 1.19.0 [\#942](https://github.com/christianhelle/refitter/pull/942) ([renovate[bot]](https://github.com/apps/renovate))
- Improve code coverage to \>90% [\#941](https://github.com/christianhelle/refitter/pull/941) ([christianhelle](https://github.com/christianhelle))
- Improve OpenAPI parse + codegen throughput by removing avoidable allocations and repeated regex work [\#937](https://github.com/christianhelle/refitter/pull/937) ([Copilot](https://github.com/apps/copilot-swe-agent))
- chore\(deps\): update dependency polly to 8.6.6 [\#935](https://github.com/christianhelle/refitter/pull/935) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency TUnit to 1.18.21 [\#934](https://github.com/christianhelle/refitter/pull/934) ([renovate[bot]](https://github.com/apps/renovate))
- Fix smoke tests: --interface-only variant missing using directive for contract types [\#933](https://github.com/christianhelle/refitter/pull/933) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Bump actions/github-script from 7 to 8 [\#920](https://github.com/christianhelle/refitter/pull/920) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump actions/checkout from 4 to 6 [\#919](https://github.com/christianhelle/refitter/pull/919) ([dependabot[bot]](https://github.com/apps/dependabot))
- Improve Smoke Tests execution time [\#915](https://github.com/christianhelle/refitter/pull/915) ([christianhelle](https://github.com/christianhelle))
- Bump actions/upload-artifact from 6 to 7 [\#912](https://github.com/christianhelle/refitter/pull/912) ([dependabot[bot]](https://github.com/apps/dependabot))
- chore\(deps\): update actions/upload-artifact action to v7 - autoclosed [\#911](https://github.com/christianhelle/refitter/pull/911) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add vgmello as a contributor for code [\#909](https://github.com/christianhelle/refitter/pull/909) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency tunit to 1.18.9 [\#903](https://github.com/christianhelle/refitter/pull/903) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 10.1.4 [\#902](https://github.com/christianhelle/refitter/pull/902) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency coverlet.collector to v8 [\#901](https://github.com/christianhelle/refitter/pull/901) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency TUnit to 1.15.11 [\#899](https://github.com/christianhelle/refitter/pull/899) ([renovate[bot]](https://github.com/apps/renovate))
- Fix MSBuild workflow [\#898](https://github.com/christianhelle/refitter/pull/898) ([christianhelle](https://github.com/christianhelle))
- Update ghcr.io/devcontainers/features/powershell Docker tag to v2 [\#895](https://github.com/christianhelle/refitter/pull/895) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency FluentAssertions to 7.2.1 [\#894](https://github.com/christianhelle/refitter/pull/894) ([renovate[bot]](https://github.com/apps/renovate))
- Fix build workflow: add dotnet restore before dotnet msbuild in Prepare step [\#892](https://github.com/christianhelle/refitter/pull/892) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Update dotnet monorepo [\#891](https://github.com/christianhelle/refitter/pull/891) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Swashbuckle.AspNetCore to 10.1.3 [\#889](https://github.com/christianhelle/refitter/pull/889) ([renovate[bot]](https://github.com/apps/renovate))
- Update Dependencies [\#886](https://github.com/christianhelle/refitter/pull/886) ([christianhelle](https://github.com/christianhelle))
- Update dependency ruby to v4.0.1 [\#866](https://github.com/christianhelle/refitter/pull/866) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency TUnit to 1.13.60 [\#865](https://github.com/christianhelle/refitter/pull/865) ([renovate[bot]](https://github.com/apps/renovate))
- Update dotnet monorepo [\#835](https://github.com/christianhelle/refitter/pull/835) ([renovate[bot]](https://github.com/apps/renovate))

## [1.7.3](https://github.com/christianhelle/refitter/tree/1.7.3) (2026-01-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.7.2...1.7.3)

**Implemented enhancements:**

- Refitter.MSBuild Support .NET 10 [\#881](https://github.com/christianhelle/refitter/issues/881)
- Add support for systems running only .NET 10.0 \(without .NET 8.0 or 9.0\) in Refitter.MSBuild [\#882](https://github.com/christianhelle/refitter/pull/882) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Add comprehensive Docker CLI documentation [\#884](https://github.com/christianhelle/refitter/pull/884) ([Copilot](https://github.com/apps/copilot-swe-agent))

## [1.7.2](https://github.com/christianhelle/refitter/tree/1.7.2) (2026-01-21)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.7.1...1.7.2)

**Implemented enhancements:**

- Improve Immutable Records ergonomics [\#844](https://github.com/christianhelle/refitter/issues/844)
- Omit certain operation headers and include all others [\#840](https://github.com/christianhelle/refitter/issues/840)
- Create .refitter settings file as part of output [\#859](https://github.com/christianhelle/refitter/pull/859) ([christianhelle](https://github.com/christianhelle))
- Fix integerType enum deserialization issue [\#855](https://github.com/christianhelle/refitter/pull/855) ([christianhelle](https://github.com/christianhelle))
- support custom nswag template directory \#844 [\#854](https://github.com/christianhelle/refitter/pull/854) ([kmc059000](https://github.com/kmc059000))
- Fix missing method parameter XML code-documentation [\#850](https://github.com/christianhelle/refitter/pull/850) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- integerType parsing in settings file fails [\#851](https://github.com/christianhelle/refitter/issues/851)
- CS1573 : Method parameter has no matching XML comment [\#846](https://github.com/christianhelle/refitter/issues/846)

**Merged pull requests:**

- docs: add frogcrush as a contributor for code [\#878](https://github.com/christianhelle/refitter/pull/878) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Migrate solution files from .sln to .slnx format [\#876](https://github.com/christianhelle/refitter/pull/876) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Fix issue with randomly failing tests to due parallel execution [\#872](https://github.com/christianhelle/refitter/pull/872) ([christianhelle](https://github.com/christianhelle))
- chore\(deps\): update dependency tunit to 1.9.2 [\#863](https://github.com/christianhelle/refitter/pull/863) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.7.7 [\#862](https://github.com/christianhelle/refitter/pull/862) ([renovate[bot]](https://github.com/apps/renovate))
- Add unit tests for WriteRefitterSettingsFile functionality [\#860](https://github.com/christianhelle/refitter/pull/860) ([Copilot](https://github.com/apps/copilot-swe-agent))
- chore\(deps\): update dependency ruby to v4 [\#858](https://github.com/christianhelle/refitter/pull/858) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.6.28 [\#857](https://github.com/christianhelle/refitter/pull/857) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add 0x2badc0de as a contributor for bug [\#856](https://github.com/christianhelle/refitter/pull/856) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 10.1.0 [\#853](https://github.com/christianhelle/refitter/pull/853) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.6.0 [\#852](https://github.com/christianhelle/refitter/pull/852) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add lilinus as a contributor for code [\#849](https://github.com/christianhelle/refitter/pull/849) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency ruby to v3.4.8 [\#845](https://github.com/christianhelle/refitter/pull/845) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency tunit to 1.5.70 [\#837](https://github.com/christianhelle/refitter/pull/837) ([renovate[bot]](https://github.com/apps/renovate))

## [1.7.1](https://github.com/christianhelle/refitter/tree/1.7.1) (2025-12-16)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.7.0...1.7.1)

**Implemented enhancements:**

- Improved handling of optional parameters [\#448](https://github.com/christianhelle/refitter/issues/448)
- Asana API specs strange naming [\#364](https://github.com/christianhelle/refitter/issues/364)
- Refit v9.0.2 [\#829](https://github.com/christianhelle/refitter/pull/829) ([renovate[bot]](https://github.com/apps/renovate))
- Add .NET 10 support [\#822](https://github.com/christianhelle/refitter/pull/822) ([christianhelle](https://github.com/christianhelle))
- Fix missing XML doc for CancellationToken [\#819](https://github.com/christianhelle/refitter/pull/819) ([christianhelle](https://github.com/christianhelle))
- Fix incorrect casing on multi-part form data parameters [\#806](https://github.com/christianhelle/refitter/pull/806) ([christianhelle](https://github.com/christianhelle))
- Optional parameters with default values [\#803](https://github.com/christianhelle/refitter/pull/803) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Using cancellation tokens with xml doc comments plus TreatWarningsAsErrors and documentation file [\#817](https://github.com/christianhelle/refitter/issues/817)
- Multipart form data parameters wrong casing [\#805](https://github.com/christianhelle/refitter/issues/805)
- Use of non generic `JsonStringEnumConverter` prohibits usage of Json-SourceGenerationContext [\#778](https://github.com/christianhelle/refitter/issues/778)
- SourceGenerator 1.5 and newer causes build error with Visual Studio 2022 [\#627](https://github.com/christianhelle/refitter/issues/627)

**Merged pull requests:**

- Update dependency TUnit to 1.5.37 [\#836](https://github.com/christianhelle/refitter/pull/836) ([renovate[bot]](https://github.com/apps/renovate))
- Update dotnet monorepo [\#834](https://github.com/christianhelle/refitter/pull/834) ([renovate[bot]](https://github.com/apps/renovate))
- Fix typo in class name and Spectre.Console markup escaping issue [\#828](https://github.com/christianhelle/refitter/pull/828) ([Copilot](https://github.com/apps/copilot-swe-agent))
- chore\(deps\): update dependency spectre.console.cli to 0.53.1 [\#827](https://github.com/christianhelle/refitter/pull/827) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency polly to 8.6.5 [\#826](https://github.com/christianhelle/refitter/pull/826) ([renovate[bot]](https://github.com/apps/renovate))
- Bump actions/checkout from 5 to 6 [\#825](https://github.com/christianhelle/refitter/pull/825) ([dependabot[bot]](https://github.com/apps/dependabot))
- chore\(deps\): update nswag monorepo to 14.6.3 [\#824](https://github.com/christianhelle/refitter/pull/824) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency microsoft.build.utilities.core to v18 [\#821](https://github.com/christianhelle/refitter/pull/821) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 10.0.1 [\#820](https://github.com/christianhelle/refitter/pull/820) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add karoberts as a contributor for bug [\#818](https://github.com/christianhelle/refitter/pull/818) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency swashbuckle.aspnetcore to v10 [\#816](https://github.com/christianhelle/refitter/pull/816) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency microsoft.extensions.http.resilience to v10 [\#815](https://github.com/christianhelle/refitter/pull/815) ([renovate[bot]](https://github.com/apps/renovate))
- Update Spectre.Console.Cli to 0.53.0 [\#814](https://github.com/christianhelle/refitter/pull/814) ([Copilot](https://github.com/apps/copilot-swe-agent))
- chore\(deps\): update dotnet monorepo to v10 \(major\) [\#813](https://github.com/christianhelle/refitter/pull/813) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency microsoft.net.test.sdk to 18.0.1 [\#808](https://github.com/christianhelle/refitter/pull/808) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add mhartmair-cubido as a contributor for bug [\#807](https://github.com/christianhelle/refitter/pull/807) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Fix string escaping, null safety, and numeric literals in optional parameter default values [\#804](https://github.com/christianhelle/refitter/pull/804) ([Copilot](https://github.com/apps/copilot-swe-agent))

## [1.7.0](https://github.com/christianhelle/refitter/tree/1.7.0) (2025-11-06)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.6.5...1.7.0)

**Implemented enhancements:**

- Option to Disable JsonStringEnumConverter Attributes in Refitter Code Generation [\#652](https://github.com/christianhelle/refitter/issues/652)
- Improve OpenAPI Description handling [\#787](https://github.com/christianhelle/refitter/pull/787) ([christianhelle](https://github.com/christianhelle))
- Add option to remove \[JsonConverter\(typeof\(JsonStringEnumConverter\)\)\] from generated contracts [\#786](https://github.com/christianhelle/refitter/pull/786) ([christianhelle](https://github.com/christianhelle))
- Fix Multipart file array support [\#784](https://github.com/christianhelle/refitter/pull/784) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Multipart file array generates IEnumerable\<FileParameter\> instead of IEnumerable\<StreamPart\> [\#783](https://github.com/christianhelle/refitter/issues/783)
- Refitter.MSBuild 1.6.4 - Can't generate from .refitter file [\#763](https://github.com/christianhelle/refitter/issues/763)
- If description has /n the Generator dont add /// to comment the line for QueryParameter classes [\#613](https://github.com/christianhelle/refitter/issues/613)
- When openapi not contains format field for integer type, it is generated as Int32 C\# equivalent type [\#167](https://github.com/christianhelle/refitter/issues/167)

**Merged pull requests:**

- Update actions/upload-artifact action to v5 [\#798](https://github.com/christianhelle/refitter/pull/798) ([renovate[bot]](https://github.com/apps/renovate))
- Re-format code [\#788](https://github.com/christianhelle/refitter/pull/788) ([christianhelle](https://github.com/christianhelle))
- docs: add christophdebaene as a contributor for bug [\#785](https://github.com/christianhelle/refitter/pull/785) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency ruby to v3.4.7 [\#777](https://github.com/christianhelle/refitter/pull/777) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency refitter.sourcegenerator to 1.6.5 [\#776](https://github.com/christianhelle/refitter/pull/776) ([renovate[bot]](https://github.com/apps/renovate))

## [1.6.5](https://github.com/christianhelle/refitter/tree/1.6.5) (2025-10-06)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.6.4...1.6.5)

**Implemented enhancements:**

- chore: add ability to skip-validation + simplify unicode logging [\#767](https://github.com/christianhelle/refitter/pull/767) ([david-pw](https://github.com/david-pw))
- Do not remove colon from url paths, verify they're not present in operation names [\#765](https://github.com/christianhelle/refitter/pull/765) ([eoma-knowit](https://github.com/eoma-knowit))

**Fixed bugs:**

- Explicitely setting multipleInterfaces = "ByEndpoint" causes Refitter to render names literally "{operationName}Async" [\#757](https://github.com/christianhelle/refitter/issues/757)

**Merged pull requests:**

- chore\(deps\): update dependency system.reactive to 6.1.0 [\#775](https://github.com/christianhelle/refitter/pull/775) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency microsoft.net.test.sdk to v18 [\#774](https://github.com/christianhelle/refitter/pull/774) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add 0xced as a contributor for code [\#773](https://github.com/christianhelle/refitter/pull/773) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency polly to 8.6.4 [\#769](https://github.com/christianhelle/refitter/pull/769) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add david-pw as a contributor for code [\#768](https://github.com/christianhelle/refitter/pull/768) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add eoma-knowit as a contributor for code [\#766](https://github.com/christianhelle/refitter/pull/766) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add david-pw as a contributor for bug [\#764](https://github.com/christianhelle/refitter/pull/764) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency newtonsoft.json to 13.0.4 [\#748](https://github.com/christianhelle/refitter/pull/748) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dotnet monorepo [\#742](https://github.com/christianhelle/refitter/pull/742) ([renovate[bot]](https://github.com/apps/renovate))

## [1.6.4](https://github.com/christianhelle/refitter/tree/1.6.4) (2025-09-20)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.6.3...1.6.4)

**Implemented enhancements:**

- Update --operation-name-template implementation to replace all {operationName} instances with Execute [\#759](https://github.com/christianhelle/refitter/pull/759) ([christianhelle](https://github.com/christianhelle))

**Closed issues:**

- Fix SonarCloud  issues [\#752](https://github.com/christianhelle/refitter/issues/752)

**Merged pull requests:**

- docs: add marcohern as a contributor for ideas [\#760](https://github.com/christianhelle/refitter/pull/760) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update dependency Refitter.SourceGenerator to 1.6.3 [\#755](https://github.com/christianhelle/refitter/pull/755) ([renovate[bot]](https://github.com/apps/renovate))
- Fix SonarCloud maintainability issues - eliminate code duplication and improve code quality [\#753](https://github.com/christianhelle/refitter/pull/753) ([Copilot](https://github.com/apps/copilot-swe-agent))

## [1.6.3](https://github.com/christianhelle/refitter/tree/1.6.3) (2025-09-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.6.2...1.6.3)

**Implemented enhancements:**

- Introduce --simple-output CLI argument [\#751](https://github.com/christianhelle/refitter/pull/751) ([christianhelle](https://github.com/christianhelle))
- Add support for systems running only .NET 9.0 \(without .NET 8.0\) in Refitter.MSBuild [\#746](https://github.com/christianhelle/refitter/pull/746) ([christianhelle](https://github.com/christianhelle))
- Fix MSBuild task so that the generated code is included in the compilation [\#745](https://github.com/christianhelle/refitter/pull/745) ([christianhelle](https://github.com/christianhelle))
- Revert NSwag back to v14.4.0 [\#734](https://github.com/christianhelle/refitter/pull/734) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Refitter Output is mangled and unreadable [\#750](https://github.com/christianhelle/refitter/issues/750)

**Merged pull requests:**

- chore\(deps\): update dependency spectre.console.cli to 0.51.1 [\#741](https://github.com/christianhelle/refitter/pull/741) ([renovate[bot]](https://github.com/apps/renovate))
- Bump actions/github-script from 7 to 8 [\#740](https://github.com/christianhelle/refitter/pull/740) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump actions/setup-dotnet from 4 to 5 [\#738](https://github.com/christianhelle/refitter/pull/738) ([dependabot[bot]](https://github.com/apps/dependabot))
- chore\(deps\): update dependency system.reactive to 6.0.2 [\#736](https://github.com/christianhelle/refitter/pull/736) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 9.0.4 [\#733](https://github.com/christianhelle/refitter/pull/733) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency polly to 8.6.3 [\#732](https://github.com/christianhelle/refitter/pull/732) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency refitter.sourcegenerator to 1.6.2 [\#731](https://github.com/christianhelle/refitter/pull/731) ([renovate[bot]](https://github.com/apps/renovate))

## [1.6.2](https://github.com/christianhelle/refitter/tree/1.6.2) (2025-08-18)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.6.1...1.6.2)

**Implemented enhancements:**

- ASCII Art Title [\#729](https://github.com/christianhelle/refitter/pull/729) ([christianhelle](https://github.com/christianhelle))
- NSwag v14.5.0 [\#722](https://github.com/christianhelle/refitter/pull/722) ([renovate[bot]](https://github.com/apps/renovate))
- Fix table alignments in CLI Output [\#720](https://github.com/christianhelle/refitter/pull/720) ([christianhelle](https://github.com/christianhelle))
- Fix match path on cmd prompt [\#719](https://github.com/christianhelle/refitter/pull/719) ([christianhelle](https://github.com/christianhelle))
- Fix missing namespace import [\#718](https://github.com/christianhelle/refitter/pull/718) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Missing usings for separate contract/interface namespaces in same file [\#715](https://github.com/christianhelle/refitter/issues/715)
- match-path doesn't work from cmd prompt [\#713](https://github.com/christianhelle/refitter/issues/713)

**Closed issues:**

- Setup CoPilot Instructions [\#724](https://github.com/christianhelle/refitter/issues/724)

**Merged pull requests:**

- chore\(deps\): update dependency xunit.runner.visualstudio to 3.1.4 [\#730](https://github.com/christianhelle/refitter/pull/730) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency microsoft.extensions.http.resilience to 9.8.0 [\#728](https://github.com/christianhelle/refitter/pull/728) ([renovate[bot]](https://github.com/apps/renovate))
- Bump actions/checkout from 4 to 5 [\#727](https://github.com/christianhelle/refitter/pull/727) ([dependabot[bot]](https://github.com/apps/dependabot))
- Add comprehensive GitHub Copilot instructions for Refitter repository [\#725](https://github.com/christianhelle/refitter/pull/725) ([Copilot](https://github.com/apps/copilot-swe-agent))
- chore\(deps\): update dotnet monorepo to 9.0.8 [\#723](https://github.com/christianhelle/refitter/pull/723) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency xunit.runner.visualstudio to 3.1.3 [\#721](https://github.com/christianhelle/refitter/pull/721) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add lilinus as a contributor for bug [\#717](https://github.com/christianhelle/refitter/pull/717) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency polly to 8.6.2 [\#716](https://github.com/christianhelle/refitter/pull/716) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add SWarnberg as a contributor for bug [\#714](https://github.com/christianhelle/refitter/pull/714) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dotnet monorepo [\#712](https://github.com/christianhelle/refitter/pull/712) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency refitter.sourcegenerator to 1.6.1 [\#711](https://github.com/christianhelle/refitter/pull/711) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 9.0.3 [\#710](https://github.com/christianhelle/refitter/pull/710) ([renovate[bot]](https://github.com/apps/renovate))

## [1.6.1](https://github.com/christianhelle/refitter/tree/1.6.1) (2025-07-08)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.6.0...1.6.1)

**Implemented enhancements:**

- Generated Refit Code Pragmas Start Above Interface [\#706](https://github.com/christianhelle/refitter/issues/706)
- Ensure that Refit interfaces have a \<summary\> [\#709](https://github.com/christianhelle/refitter/pull/709) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- chore\(deps\): update dependency xunit.runner.visualstudio to 3.1.1 [\#708](https://github.com/christianhelle/refitter/pull/708) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add sb-chericks as a contributor for ideas, and bug [\#707](https://github.com/christianhelle/refitter/pull/707) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Add console output screenshots to documentation [\#705](https://github.com/christianhelle/refitter/pull/705) ([christianhelle](https://github.com/christianhelle))
- Update dependency Refitter.SourceGenerator to 1.6.0 [\#704](https://github.com/christianhelle/refitter/pull/704) ([renovate[bot]](https://github.com/apps/renovate))

## [1.6.0](https://github.com/christianhelle/refitter/tree/1.6.0) (2025-06-16)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.5.6...1.6.0)

**Implemented enhancements:**

- fix missing schema for dictionary keys [\#697](https://github.com/christianhelle/refitter/pull/697) ([kirides](https://github.com/kirides))
- Fancy CLI output using  Spectre Console [\#695](https://github.com/christianhelle/refitter/pull/695) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- \[Bug\] Refitter generates invalid \[Range\] attribute for decimal properties starting from v1.5.2 [\#668](https://github.com/christianhelle/refitter/issues/668)
- Generates Content-Type: multipart/form-data Header which breaks Multipart uploads [\#654](https://github.com/christianhelle/refitter/issues/654)

**Closed issues:**

- Improve documentation [\#700](https://github.com/christianhelle/refitter/issues/700)

**Merged pull requests:**

- Update dependency Polly to 8.6.1 [\#703](https://github.com/christianhelle/refitter/pull/703) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Swashbuckle.AspNetCore to v9 [\#702](https://github.com/christianhelle/refitter/pull/702) ([renovate[bot]](https://github.com/apps/renovate))
- Fix typos and grammar issues in documentation [\#701](https://github.com/christianhelle/refitter/pull/701) ([Copilot](https://github.com/apps/copilot-swe-agent))
- Update dotnet monorepo [\#699](https://github.com/christianhelle/refitter/pull/699) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Polly to 8.6.0 [\#698](https://github.com/christianhelle/refitter/pull/698) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Refitter.SourceGenerator to 1.5.6 [\#696](https://github.com/christianhelle/refitter/pull/696) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Microsoft.NET.Test.Sdk to 17.14.1 [\#692](https://github.com/christianhelle/refitter/pull/692) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Swashbuckle.AspNetCore to 8.1.4 [\#691](https://github.com/christianhelle/refitter/pull/691) ([renovate[bot]](https://github.com/apps/renovate))

## [1.5.6](https://github.com/christianhelle/refitter/tree/1.5.6) (2025-06-03)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.5.5...1.5.6)

**Implemented enhancements:**

- Support for .NET 9 [\#684](https://github.com/christianhelle/refitter/issues/684)
- Do not add both \[Multipart\] and "Content-Type: multipart/form-data" [\#693](https://github.com/christianhelle/refitter/pull/693) ([jaroslaw-dutka](https://github.com/jaroslaw-dutka))
- Add .NET 9.0 to Target Frameworks [\#690](https://github.com/christianhelle/refitter/pull/690) ([christianhelle](https://github.com/christianhelle))
- Use fully qualified type name in Class template [\#686](https://github.com/christianhelle/refitter/pull/686) ([velvolue](https://github.com/velvolue))

**Fixed bugs:**

- Missing references when generating contracts only using polymorphic serialization [\#683](https://github.com/christianhelle/refitter/issues/683)

**Closed issues:**

- Resolve build warnings [\#688](https://github.com/christianhelle/refitter/issues/688)
- Contribution Guidelines [\#678](https://github.com/christianhelle/refitter/issues/678)

**Merged pull requests:**

- Resolve build warnings and add TreatWarningsAsErrors [\#689](https://github.com/christianhelle/refitter/pull/689) ([Copilot](https://github.com/apps/copilot-swe-agent))
- docs: add velvolue as a contributor for bug [\#687](https://github.com/christianhelle/refitter/pull/687) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 8.1.2 [\#682](https://github.com/christianhelle/refitter/pull/682) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency microsoft.net.test.sdk to 17.14.0 [\#680](https://github.com/christianhelle/refitter/pull/680) ([renovate[bot]](https://github.com/apps/renovate))
- Add Contribution Guidelines [\#679](https://github.com/christianhelle/refitter/pull/679) ([Copilot](https://github.com/apps/copilot-swe-agent))
- chore\(deps\): update dependency microsoft.build.utilities.core to 17.14.8 [\#676](https://github.com/christianhelle/refitter/pull/676) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency microsoft.build.utilities.core to 17.14.7 [\#675](https://github.com/christianhelle/refitter/pull/675) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dotnet monorepo [\#674](https://github.com/christianhelle/refitter/pull/674) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add MrScottyTay as a contributor for bug [\#673](https://github.com/christianhelle/refitter/pull/673) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency refitter.sourcegenerator to 1.5.5 [\#671](https://github.com/christianhelle/refitter/pull/671) ([renovate[bot]](https://github.com/apps/renovate))

## [1.5.5](https://github.com/christianhelle/refitter/tree/1.5.5) (2025-05-04)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.5.4...1.5.5)

**Implemented enhancements:**

- Using CollectionFormats other than Multi [\#640](https://github.com/christianhelle/refitter/issues/640)
- Add collection format option to CLI tool documentation [\#664](https://github.com/christianhelle/refitter/pull/664) ([christianhelle](https://github.com/christianhelle))
- Made Security Header Parameters safe for C\# when unsafe characters are present [\#663](https://github.com/christianhelle/refitter/pull/663) ([AragornHL](https://github.com/AragornHL))

**Merged pull requests:**

- chore\(deps\): update dependency xunit.runner.visualstudio to 3.1.0 [\#670](https://github.com/christianhelle/refitter/pull/670) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add tommieemeli as a contributor for bug [\#669](https://github.com/christianhelle/refitter/pull/669) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update nswag monorepo to 14.4.0 [\#665](https://github.com/christianhelle/refitter/pull/665) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency refitter.sourcegenerator to 1.5.4 [\#662](https://github.com/christianhelle/refitter/pull/662) ([renovate[bot]](https://github.com/apps/renovate))

## [1.5.4](https://github.com/christianhelle/refitter/tree/1.5.4) (2025-04-26)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.5.3...1.5.4)

**Implemented enhancements:**

- Adding security schemes to the api interface generator  [\#106](https://github.com/christianhelle/refitter/issues/106)
- Response type handling to only use 2XX range [\#661](https://github.com/christianhelle/refitter/pull/661) ([christianhelle](https://github.com/christianhelle))
- Add support for 2XX and Default Response Objects [\#660](https://github.com/christianhelle/refitter/pull/660) ([christianhelle](https://github.com/christianhelle))
- Add Header Parameters for Security Schemes [\#653](https://github.com/christianhelle/refitter/pull/653) ([AragornHL](https://github.com/AragornHL))

**Fixed bugs:**

- 2xx and default are not recognized as responses [\#658](https://github.com/christianhelle/refitter/issues/658)

**Merged pull requests:**

- docs: add pfeigl as a contributor for bug [\#659](https://github.com/christianhelle/refitter/pull/659) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update smoke tests to generate authentication headers [\#657](https://github.com/christianhelle/refitter/pull/657) ([christianhelle](https://github.com/christianhelle))
- docs: add kmfd3s as a contributor for code [\#656](https://github.com/christianhelle/refitter/pull/656) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add AragornHL as a contributor for code [\#655](https://github.com/christianhelle/refitter/pull/655) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 8.1.1 [\#651](https://github.com/christianhelle/refitter/pull/651) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency exceptionless to 6.1.0 [\#647](https://github.com/christianhelle/refitter/pull/647) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency swashbuckle.aspnetcore to 8.1.0 [\#646](https://github.com/christianhelle/refitter/pull/646) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency refitter.sourcegenerator to 1.5.3 [\#645](https://github.com/christianhelle/refitter/pull/645) ([renovate[bot]](https://github.com/apps/renovate))

## [1.5.3](https://github.com/christianhelle/refitter/tree/1.5.3) (2025-03-29)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.5.2...1.5.3)

**Implemented enhancements:**

- Naming properties problem [\#641](https://github.com/christianhelle/refitter/issues/641)
- Can we write custom middleware for generators? [\#636](https://github.com/christianhelle/refitter/issues/636)
- Allow comments in .refitter Configuration [\#631](https://github.com/christianhelle/refitter/issues/631)
- NSwag v14.3.0 [\#644](https://github.com/christianhelle/refitter/pull/644) ([renovate[bot]](https://github.com/apps/renovate))
- Convert properties with underscores to PascalCase [\#643](https://github.com/christianhelle/refitter/pull/643) ([christianhelle](https://github.com/christianhelle))
- Add support for deserializing JSON with comments and update tests [\#637](https://github.com/christianhelle/refitter/pull/637) ([sebastian-wachsmuth](https://github.com/sebastian-wachsmuth))
- Temporary fix for Source Generator when running in Visual Studio [\#634](https://github.com/christianhelle/refitter/pull/634) ([christianhelle](https://github.com/christianhelle))
- JSON Schema generator for documentation [\#623](https://github.com/christianhelle/refitter/pull/623) ([christianhelle](https://github.com/christianhelle))
- Fix invalid characters in generated XML docs [\#607](https://github.com/christianhelle/refitter/pull/607) ([christianhelle](https://github.com/christianhelle))
- Add support for custom DateTimeFormat [\#604](https://github.com/christianhelle/refitter/pull/604) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Doesn't add Content-Type request header when body is plain JSON string [\#617](https://github.com/christianhelle/refitter/issues/617)
- Broken xml doc when swagger descriptions contains "\<" or "\>" characters [\#605](https://github.com/christianhelle/refitter/issues/605)
- date-time parameters are encoded as date when iso8601 is used [\#599](https://github.com/christianhelle/refitter/issues/599)

**Closed issues:**

- OpenAPI Schema and Authorization Attributes [\#629](https://github.com/christianhelle/refitter/issues/629)

**Merged pull requests:**

- docs: add lowern1ght as a contributor for bug [\#642](https://github.com/christianhelle/refitter/pull/642) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add qrzychu as a contributor for bug [\#639](https://github.com/christianhelle/refitter/pull/639) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add sebastian-wachsmuth as a contributor for code [\#638](https://github.com/christianhelle/refitter/pull/638) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update dependency Swashbuckle.AspNetCore to v8 [\#633](https://github.com/christianhelle/refitter/pull/633) ([renovate[bot]](https://github.com/apps/renovate))
- Update dotnet monorepo [\#630](https://github.com/christianhelle/refitter/pull/630) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Swashbuckle.AspNetCore to 7.3.0 [\#624](https://github.com/christianhelle/refitter/pull/624) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency fluentassertions to 7.2.0 [\#622](https://github.com/christianhelle/refitter/pull/622) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency apizr.integrations.fusillade to 6.4.2 [\#621](https://github.com/christianhelle/refitter/pull/621) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add Metziell as a contributor for bug [\#620](https://github.com/christianhelle/refitter/pull/620) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency microsoft.net.test.sdk to 17.13.0 [\#610](https://github.com/christianhelle/refitter/pull/610) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency xunit.runner.visualstudio to 3.0.2 [\#609](https://github.com/christianhelle/refitter/pull/609) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add wocasella as a contributor for bug [\#606](https://github.com/christianhelle/refitter/pull/606) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [1.5.2](https://github.com/christianhelle/refitter/tree/1.5.2) (2025-01-29)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.5.1...1.5.2)

**Merged pull requests:**

- chore\(deps\): update dependency refitter.sourcegenerator to 1.5.1 [\#598](https://github.com/christianhelle/refitter/pull/598) ([renovate[bot]](https://github.com/apps/renovate))

## [1.5.1](https://github.com/christianhelle/refitter/tree/1.5.1) (2025-01-25)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.5.0...1.5.1)

**Fixed bugs:**

- \[FromForm\] parameter on minimal api doesn't get generated on Interface [\#515](https://github.com/christianhelle/refitter/issues/515)

**Merged pull requests:**

- Remove dependency on System.Text.Json [\#597](https://github.com/christianhelle/refitter/pull/597) ([christianhelle](https://github.com/christianhelle))

## [1.5.0](https://github.com/christianhelle/refitter/tree/1.5.0) (2025-01-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.4.1...1.5.0)

**Implemented enhancements:**

- Fix incorrect error message shown due to Spectre.Console parsing [\#585](https://github.com/christianhelle/refitter/pull/585) ([christianhelle](https://github.com/christianhelle))
- Discard unused union types/inheritance types via config [\#575](https://github.com/christianhelle/refitter/pull/575) ([kirides](https://github.com/kirides))

**Fixed bugs:**

- "Error: Could not find color or style 'System.String'." [\#583](https://github.com/christianhelle/refitter/issues/583)
- Source generator errors are hidden [\#568](https://github.com/christianhelle/refitter/issues/568)
- Refitter -v not showing version number [\#560](https://github.com/christianhelle/refitter/issues/560)
- Not so nice behavior when generating client with trim-unused-schema [\#557](https://github.com/christianhelle/refitter/issues/557)
- Two almost identical routes that fail at validation. [\#551](https://github.com/christianhelle/refitter/issues/551)
- Code Generator creates unsafe interface method names [\#360](https://github.com/christianhelle/refitter/issues/360)

**Closed issues:**

- How to use in class library? [\#534](https://github.com/christianhelle/refitter/issues/534)
- \[ISSUE\]\[1.2.1-preview.54\] Some impediments using CLI version. Is not enough for my needs?  [\#450](https://github.com/christianhelle/refitter/issues/450)

**Merged pull requests:**

- Revert "chore\(deps\): update dependency fluentassertions to v8" [\#588](https://github.com/christianhelle/refitter/pull/588) ([christianhelle](https://github.com/christianhelle))
- chore\(deps\): update dependency fluentassertions to v8 [\#586](https://github.com/christianhelle/refitter/pull/586) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add brad-technologik as a contributor for bug [\#584](https://github.com/christianhelle/refitter/pull/584) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update dependency Atc.Test to 1.1.9 [\#574](https://github.com/christianhelle/refitter/pull/574) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency coverlet.collector to 6.0.3 [\#573](https://github.com/christianhelle/refitter/pull/573) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency fluentassertions to v7 [\#546](https://github.com/christianhelle/refitter/pull/546) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add zidad as a contributor for ideas [\#545](https://github.com/christianhelle/refitter/pull/545) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [1.4.1](https://github.com/christianhelle/refitter/tree/1.4.1) (2024-11-19)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.4.0...1.4.1)

**Implemented enhancements:**

- Thanks for the great tool! [\#522](https://github.com/christianhelle/refitter/issues/522)
- Add PropertyNameGenerator as an optional Parameter [\#516](https://github.com/christianhelle/refitter/issues/516)

## [1.4.0](https://github.com/christianhelle/refitter/tree/1.4.0) (2024-10-14)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.3.2...1.4.0)

## [1.3.2](https://github.com/christianhelle/refitter/tree/1.3.2) (2024-09-23)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.3.1...1.3.2)

**Fixed bugs:**

- Exceptionless monthly limit exceeded in only a few days [\#488](https://github.com/christianhelle/refitter/issues/488)
- "While scanning a multi-line double-quoted scalar, found wrong indentation." on valid yaml file [\#486](https://github.com/christianhelle/refitter/issues/486)

## [1.3.1](https://github.com/christianhelle/refitter/tree/1.3.1) (2024-09-20)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.3.0...1.3.1)

## [1.3.0](https://github.com/christianhelle/refitter/tree/1.3.0) (2024-09-14)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.2.0...1.3.0)

**Implemented enhancements:**

- Missing documentation for System.Text.Json polymorphic serialization [\#467](https://github.com/christianhelle/refitter/issues/467)
- Is it possible to replace the liquid files of NSwag? [\#459](https://github.com/christianhelle/refitter/issues/459)
- Support generating multiple files [\#427](https://github.com/christianhelle/refitter/issues/427)

**Fixed bugs:**

- Percent symbol '%' can be inserted as a property name [\#453](https://github.com/christianhelle/refitter/issues/453)
- Explicit \#nullable enable introduced v1.2.0 produces excessive warnings [\#451](https://github.com/christianhelle/refitter/issues/451)

## [1.2.0](https://github.com/christianhelle/refitter/tree/1.2.0) (2024-08-11)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.1.3...1.2.0)

## [1.1.3](https://github.com/christianhelle/refitter/tree/1.1.3) (2024-07-19)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.1.2...1.1.3)

## [1.1.2](https://github.com/christianhelle/refitter/tree/1.1.2) (2024-07-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.1.1...1.1.2)

**Implemented enhancements:**

- Support generating immutable records [\#407](https://github.com/christianhelle/refitter/issues/407)

## [1.1.1](https://github.com/christianhelle/refitter/tree/1.1.1) (2024-07-04)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.0.2...1.1.1)

**Implemented enhancements:**

- Polly.Extensions.Http deprecated in favour of Microsoft.Extensions.Http.Resilience [\#398](https://github.com/christianhelle/refitter/issues/398)

## [1.0.2](https://github.com/christianhelle/refitter/tree/1.0.2) (2024-06-13)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.0.1...1.0.2)

## [1.0.1](https://github.com/christianhelle/refitter/tree/1.0.1) (2024-06-07)

[Full Changelog](https://github.com/christianhelle/refitter/compare/1.0.0...1.0.1)

**Implemented enhancements:**

- Serializer improvements [\#383](https://github.com/christianhelle/refitter/issues/383)

**Merged pull requests:**

- chore\(deps\): update mcr.microsoft.com/devcontainers/dotnet docker tag to v1 [\#381](https://github.com/christianhelle/refitter/pull/381) ([renovate[bot]](https://github.com/apps/renovate))

## [1.0.0](https://github.com/christianhelle/refitter/tree/1.0.0) (2024-05-03)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.9...1.0.0)

**Implemented enhancements:**

- Add the facility to exclude namespaces from generated code [\#362](https://github.com/christianhelle/refitter/issues/362)
- Resolve SonarCloud discovered issues [\#340](https://github.com/christianhelle/refitter/pull/340) ([christianhelle](https://github.com/christianhelle))
- Fix code generator settings that are not of type string/bool [\#335](https://github.com/christianhelle/refitter/pull/335) ([david-brink-talogy](https://github.com/david-brink-talogy))

**Fixed bugs:**

- Refit Fails on Jira OpenAPI specs [\#371](https://github.com/christianhelle/refitter/issues/371)
- Asana API "cannot derive from sealed type" [\#359](https://github.com/christianhelle/refitter/issues/359)
- `remove-unused-schema` not working for collection-type responses [\#352](https://github.com/christianhelle/refitter/issues/352)
- Non string/boolean CodeGeneratorSettings are not honored [\#334](https://github.com/christianhelle/refitter/issues/334)

**Merged pull requests:**

- docs: add dammitjanet as a contributor for ideas [\#363](https://github.com/christianhelle/refitter/pull/363) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Bump Microsoft.Extensions.Http.Polly from 8.0.3 to 8.0.4 [\#358](https://github.com/christianhelle/refitter/pull/358) ([dependabot[bot]](https://github.com/apps/dependabot))
- Update nswag monorepo to v14.0.7 [\#347](https://github.com/christianhelle/refitter/pull/347) ([renovate[bot]](https://github.com/apps/renovate))
- Bump SonarAnalyzer.CSharp from 9.22.0.87781 to 9.23.0.88079 [\#346](https://github.com/christianhelle/refitter/pull/346) ([dependabot[bot]](https://github.com/apps/dependabot))
- Update dependency SonarAnalyzer.CSharp to v9.22.0.87781 [\#343](https://github.com/christianhelle/refitter/pull/343) ([renovate[bot]](https://github.com/apps/renovate))
- Update dependency Microsoft.CodeAnalysis.CSharp to v4.9.2 [\#341](https://github.com/christianhelle/refitter/pull/341) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency coverlet.collector to v6.0.2 [\#339](https://github.com/christianhelle/refitter/pull/339) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add david-brink-talogy as a contributor for code [\#338](https://github.com/christianhelle/refitter/pull/338) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency microsoft.extensions.http.polly to v8.0.3 [\#337](https://github.com/christianhelle/refitter/pull/337) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add david-brink-talogy as a contributor for bug [\#336](https://github.com/christianhelle/refitter/pull/336) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [0.9.9](https://github.com/christianhelle/refitter/tree/0.9.9) (2024-03-06)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.8...0.9.9)

**Implemented enhancements:**

- Tweak xml docs [\#332](https://github.com/christianhelle/refitter/pull/332) ([osc-nseguin](https://github.com/osc-nseguin))
- Suggest using --skip-validation CLI tool argument validation error [\#329](https://github.com/christianhelle/refitter/pull/329) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- docs: add osc-nseguin as a contributor for code [\#333](https://github.com/christianhelle/refitter/pull/333) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [0.9.8](https://github.com/christianhelle/refitter/tree/0.9.8) (2024-02-27)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.7...0.9.8)

**Implemented enhancements:**

- Support for OpenAPI version 3.1.0 [\#328](https://github.com/christianhelle/refitter/issues/328)
- IObservable\<T\> improvements [\#326](https://github.com/christianhelle/refitter/pull/326) ([christianhelle](https://github.com/christianhelle))
- Generating IObservable type response [\#322](https://github.com/christianhelle/refitter/pull/322) ([janfolbrecht](https://github.com/janfolbrecht))

**Merged pull requests:**

- Change license to MIT [\#327](https://github.com/christianhelle/refitter/pull/327) ([christianhelle](https://github.com/christianhelle))
- docs: add janfolbrecht as a contributor for ideas, and code [\#324](https://github.com/christianhelle/refitter/pull/324) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Revert "chore\(deps\): update actions/upload-artifact action to v4" [\#323](https://github.com/christianhelle/refitter/pull/323) ([christianhelle](https://github.com/christianhelle))
- Bump coverlet.collector from 6.0.0 to 6.0.1 [\#319](https://github.com/christianhelle/refitter/pull/319) ([dependabot[bot]](https://github.com/apps/dependabot))
- chore\(deps\): update actions/upload-artifact action to v4 [\#318](https://github.com/christianhelle/refitter/pull/318) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update xunit-dotnet monorepo [\#315](https://github.com/christianhelle/refitter/pull/315) ([renovate[bot]](https://github.com/apps/renovate))
- JSON schema [\#314](https://github.com/christianhelle/refitter/pull/314) ([christianhelle](https://github.com/christianhelle))

## [0.9.7](https://github.com/christianhelle/refitter/tree/0.9.7) (2024-02-07)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.6...0.9.7)

**Merged pull requests:**

- chore\(deps\): update nswag monorepo to v14.0.3 [\#309](https://github.com/christianhelle/refitter/pull/309) ([renovate[bot]](https://github.com/apps/renovate))

## [0.9.6](https://github.com/christianhelle/refitter/tree/0.9.6) (2024-01-29)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.5...0.9.6)

**Implemented enhancements:**

- More than one generated Client Api will result in an Extension Method Conflict for ConfigureRefitClients  [\#294](https://github.com/christianhelle/refitter/issues/294)

**Fixed bugs:**

- Response is always nullable under `generateNullableReferenceTypes` [\#302](https://github.com/christianhelle/refitter/issues/302)

**Merged pull requests:**

- Bump ghcr.io/devcontainers/features/dotnet from 1.1.4 to 2.0.3 [\#305](https://github.com/christianhelle/refitter/pull/305) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.9.5](https://github.com/christianhelle/refitter/tree/0.9.5) (2024-01-15)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.4...0.9.5)

**Implemented enhancements:**

- Add support for old core frameworks versions from .net6 [\#290](https://github.com/christianhelle/refitter/issues/290)
- Add support for multiple target frameworks [\#292](https://github.com/christianhelle/refitter/pull/292) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Update dependency xunit to v2.6.6 [\#291](https://github.com/christianhelle/refitter/pull/291) ([renovate[bot]](https://github.com/apps/renovate))

## [0.9.4](https://github.com/christianhelle/refitter/tree/0.9.4) (2024-01-12)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.2...0.9.4)

## [0.9.2](https://github.com/christianhelle/refitter/tree/0.9.2) (2024-01-09)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.1...0.9.2)

**Fixed bugs:**

- `operationNameGenerator` enum not resolved from .refitter file [\#277](https://github.com/christianhelle/refitter/issues/277)

## [0.9.1](https://github.com/christianhelle/refitter/tree/0.9.1) (2024-01-09)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.9.0...0.9.1)

## [0.9.0](https://github.com/christianhelle/refitter/tree/0.9.0) (2024-01-08)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.7...0.9.0)

**Implemented enhancements:**

- Support $ref references to separate files in OpenAPI specifications. [\#192](https://github.com/christianhelle/refitter/issues/192)
- Configurable IOperationNameGenerator implementations [\#272](https://github.com/christianhelle/refitter/pull/272) ([christianhelle](https://github.com/christianhelle))
- Use OasReader library for loading OAS documents with external references [\#267](https://github.com/christianhelle/refitter/pull/267) ([christianhelle](https://github.com/christianhelle))
- Add support for OAS files with external references [\#260](https://github.com/christianhelle/refitter/pull/260) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Already defines a member with the same parameter types [\#269](https://github.com/christianhelle/refitter/issues/269)
- File not found when using uri [\#258](https://github.com/christianhelle/refitter/issues/258)
- Unable to change arrayType to List or IList [\#255](https://github.com/christianhelle/refitter/issues/255)
- I can not generate interface [\#249](https://github.com/christianhelle/refitter/issues/249)

**Merged pull requests:**

- Implement CustomCSharpPropertyNameGenerator  [\#271](https://github.com/christianhelle/refitter/pull/271) ([christianhelle](https://github.com/christianhelle))
- docs: add Xeevis as a contributor for bug [\#270](https://github.com/christianhelle/refitter/pull/270) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update dependency oasreader to v1.6.11.14 [\#268](https://github.com/christianhelle/refitter/pull/268) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update dependency xunit to v2.6.5 [\#266](https://github.com/christianhelle/refitter/pull/266) ([renovate[bot]](https://github.com/apps/renovate))
- chore\(deps\): update nswag monorepo to v14 \(major\) [\#262](https://github.com/christianhelle/refitter/pull/262) ([renovate[bot]](https://github.com/apps/renovate))
- docs: add kami-poi as a contributor for ideas [\#261](https://github.com/christianhelle/refitter/pull/261) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- chore\(deps\): update xunit-dotnet monorepo [\#259](https://github.com/christianhelle/refitter/pull/259) ([renovate[bot]](https://github.com/apps/renovate))

## [0.8.7](https://github.com/christianhelle/refitter/tree/0.8.7) (2023-12-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.6...0.8.7)

**Fixed bugs:**

- Error on build - dependency to Microsoft.Bcl.AsyncInterfaces [\#233](https://github.com/christianhelle/refitter/issues/233)

## [0.8.6](https://github.com/christianhelle/refitter/tree/0.8.6) (2023-12-11)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.5...0.8.6)

## [0.8.5](https://github.com/christianhelle/refitter/tree/0.8.5) (2023-11-23)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.4...0.8.5)

**Fixed bugs:**

- if a path contain colon \(":"\) character then must be replace it [\#225](https://github.com/christianhelle/refitter/issues/225)

**Merged pull requests:**

- Bump Spectre.Console.Cli from 0.47.0 to 0.48.0 [\#230](https://github.com/christianhelle/refitter/pull/230) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.8.4](https://github.com/christianhelle/refitter/tree/0.8.4) (2023-11-07)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.3...0.8.4)

**Implemented enhancements:**

- NSwag contracts [\#186](https://github.com/christianhelle/refitter/issues/186)
- Remove unused schema definitions \(e.g. `--remove-unreferenced-schema` \)  [\#170](https://github.com/christianhelle/refitter/issues/170)

**Fixed bugs:**

- IServiceCollectionExtensions extra closing parenthesis with httpMessageHandlers [\#205](https://github.com/christianhelle/refitter/issues/205)

## [0.8.3](https://github.com/christianhelle/refitter/tree/0.8.3) (2023-10-31)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.2...0.8.3)

**Implemented enhancements:**

- Manually run refit generator so the source can be directly added to the compilation rather than writing to a file [\#196](https://github.com/christianhelle/refitter/issues/196)
- Single output [\#184](https://github.com/christianhelle/refitter/issues/184)

## [0.8.2](https://github.com/christianhelle/refitter/tree/0.8.2) (2023-10-09)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.1...0.8.2)

**Implemented enhancements:**

- Allow for naming of methods when generating interfaces by endpoint [\#176](https://github.com/christianhelle/refitter/issues/176)

## [0.8.1](https://github.com/christianhelle/refitter/tree/0.8.1) (2023-10-03)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.8.0...0.8.1)

## [0.8.0](https://github.com/christianhelle/refitter/tree/0.8.0) (2023-09-23)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.5...0.8.0)

**Implemented enhancements:**

- Generate Refit interfaces as `partial` by default [\#161](https://github.com/christianhelle/refitter/issues/161)
- Mark deprecated operations [\#147](https://github.com/christianhelle/refitter/issues/147)
- Generate Refit interfaces as partial [\#162](https://github.com/christianhelle/refitter/pull/162) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Generated nullable query method params are not set to a default value of null [\#157](https://github.com/christianhelle/refitter/issues/157)
- Path to OpenAPI spec file is required in CLI command even when using a `--settings-file` parameter. [\#149](https://github.com/christianhelle/refitter/issues/149)
- Unexpected initial token 'Boolean' when populating object [\#138](https://github.com/christianhelle/refitter/issues/138)

**Closed issues:**

- Improving documentation for --settings-file cli tool parameter [\#148](https://github.com/christianhelle/refitter/issues/148)

## [0.7.5](https://github.com/christianhelle/refitter/tree/0.7.5) (2023-09-07)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.4...0.7.5)

**Fixed bugs:**

- Filter `--tag` broken [\#142](https://github.com/christianhelle/refitter/issues/142)

## [0.7.4](https://github.com/christianhelle/refitter/tree/0.7.4) (2023-09-05)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.3...0.7.4)

**Fixed bugs:**

- Downloading OpenAPI specification from URI using `content-encoding: gzip` fails [\#135](https://github.com/christianhelle/refitter/issues/135)
- Proposal: filter generated interfaces [\#131](https://github.com/christianhelle/refitter/issues/131)

## [0.7.3](https://github.com/christianhelle/refitter/tree/0.7.3) (2023-08-25)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.2...0.7.3)

**Fixed bugs:**

- Parameters' casing in openAPI document are not honoured  in Refit interface methods [\#124](https://github.com/christianhelle/refitter/issues/124)
- Add support for using .refitter file from CLI [\#121](https://github.com/christianhelle/refitter/issues/121)
- Duplicate Accept Headers [\#118](https://github.com/christianhelle/refitter/issues/118)
- Missing "Accept" Request Header in generated files based on OAS 3.0  [\#107](https://github.com/christianhelle/refitter/issues/107)
- Refitter Source Generator - generated code not being picked up by Refit's Source Generator [\#100](https://github.com/christianhelle/refitter/issues/100)

## [0.7.2](https://github.com/christianhelle/refitter/tree/0.7.2) (2023-08-07)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.1...0.7.2)

## [0.7.1](https://github.com/christianhelle/refitter/tree/0.7.1) (2023-08-02)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.0...0.7.1)

## [0.7.0](https://github.com/christianhelle/refitter/tree/0.7.0) (2023-07-31)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.3...0.7.0)

## [0.6.3](https://github.com/christianhelle/refitter/tree/0.6.3) (2023-07-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.2...0.6.3)

## [0.6.2](https://github.com/christianhelle/refitter/tree/0.6.2) (2023-06-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.1...0.6.2)

**Fixed bugs:**

- Generated code doesn't build if operationId contains spaces [\#78](https://github.com/christianhelle/refitter/issues/78)

## [0.6.1](https://github.com/christianhelle/refitter/tree/0.6.1) (2023-06-20)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.0...0.6.1)

**Fixed bugs:**

- DirectoryNotFoundException [\#76](https://github.com/christianhelle/refitter/issues/76)

## [0.6.0](https://github.com/christianhelle/refitter/tree/0.6.0) (2023-06-15)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.30...0.6.0)

**Fixed bugs:**

- String parameters with format 'date' get no Format in the QueryAttribute [\#66](https://github.com/christianhelle/refitter/issues/66)

## [0.5.30](https://github.com/christianhelle/refitter/tree/0.5.30) (2023-06-12)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.28...0.5.30)

**Fixed bugs:**

- Model definition with property named System results in class that does not compile [\#68](https://github.com/christianhelle/refitter/issues/68)
- Refitter fails to generate FormData parameter for file upload [\#62](https://github.com/christianhelle/refitter/issues/62)
- Member with the same signature is already declared [\#58](https://github.com/christianhelle/refitter/issues/58)
- Generated Method names contains invalid characters.  [\#56](https://github.com/christianhelle/refitter/issues/56)

## [0.5.28](https://github.com/christianhelle/refitter/tree/0.5.28) (2023-06-08)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.27...0.5.28)

**Implemented enhancements:**

- Generated files have inconsistent lined endings  [\#57](https://github.com/christianhelle/refitter/issues/57)

**Fixed bugs:**

- Generated output has Task return type instead of expected Task\<T\> [\#41](https://github.com/christianhelle/refitter/issues/41)

**Closed issues:**

- Add Contributors using All-Contributors [\#46](https://github.com/christianhelle/refitter/issues/46)

## [0.5.27](https://github.com/christianhelle/refitter/tree/0.5.27) (2023-05-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.26...0.5.27)

## [0.5.26](https://github.com/christianhelle/refitter/tree/0.5.26) (2023-05-11)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.25...0.5.26)

## [0.5.25](https://github.com/christianhelle/refitter/tree/0.5.25) (2023-05-10)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.3...0.5.25)

## [0.5.3](https://github.com/christianhelle/refitter/tree/0.5.3) (2023-05-05)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.2...0.5.3)

## [0.5.2](https://github.com/christianhelle/refitter/tree/0.5.2) (2023-05-02)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.1...0.5.2)

**Fixed bugs:**

- OperationHeaders generation problem with headers containing a - [\#26](https://github.com/christianhelle/refitter/issues/26)

## [0.5.1](https://github.com/christianhelle/refitter/tree/0.5.1) (2023-05-01)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.0...0.5.1)

**Implemented enhancements:**

- Add `CancellationToken cancellationToken = default` to generated Methods [\#19](https://github.com/christianhelle/refitter/issues/19)

## [0.5.0](https://github.com/christianhelle/refitter/tree/0.5.0) (2023-04-28)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.4.2...0.5.0)

## [0.4.2](https://github.com/christianhelle/refitter/tree/0.4.2) (2023-04-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.4.1...0.4.2)

**Implemented enhancements:**

- Allow specifying access modifiers for generated classes [\#20](https://github.com/christianhelle/refitter/issues/20)
- Support for .net6.0 / Reasoning why .net7 is required [\#17](https://github.com/christianhelle/refitter/issues/17)
- Release v0.4.2 [\#22](https://github.com/christianhelle/refitter/pull/22) ([christianhelle](https://github.com/christianhelle))
- Add support for generating 'internal' types [\#21](https://github.com/christianhelle/refitter/pull/21) ([christianhelle](https://github.com/christianhelle))

## [0.4.1](https://github.com/christianhelle/refitter/tree/0.4.1) (2023-04-03)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.4.0...0.4.1)

## [0.4.0](https://github.com/christianhelle/refitter/tree/0.4.0) (2023-03-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.17...0.4.0)

**Implemented enhancements:**

- Add support for generating IApiResponse\<T\> as return types [\#13](https://github.com/christianhelle/refitter/issues/13)

## [0.3.17](https://github.com/christianhelle/refitter/tree/0.3.17) (2023-03-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.16...0.3.17)

## [0.3.16](https://github.com/christianhelle/refitter/tree/0.3.16) (2023-03-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.4...0.3.16)

## [0.3.4](https://github.com/christianhelle/refitter/tree/0.3.4) (2023-03-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.3...0.3.4)

**Implemented enhancements:**

- Please add support for kebab string casing parameters [\#10](https://github.com/christianhelle/refitter/issues/10)

## [0.3.3](https://github.com/christianhelle/refitter/tree/0.3.3) (2023-03-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.2...0.3.3)

## [0.3.2](https://github.com/christianhelle/refitter/tree/0.3.2) (2023-03-16)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.1...0.3.2)

**Fixed bugs:**

- Missing path parameters in parent [\#8](https://github.com/christianhelle/refitter/issues/8)
- Parameters from the query do not add into the resulting interface [\#5](https://github.com/christianhelle/refitter/issues/5)

## [0.3.1](https://github.com/christianhelle/refitter/tree/0.3.1) (2023-03-14)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.0...0.3.1)

## [0.3.0](https://github.com/christianhelle/refitter/tree/0.3.0) (2023-03-14)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.2.4-alpha...0.3.0)

## [0.2.4-alpha](https://github.com/christianhelle/refitter/tree/0.2.4-alpha) (2023-03-01)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.2.3-alpha...0.2.4-alpha)

## [0.2.3-alpha](https://github.com/christianhelle/refitter/tree/0.2.3-alpha) (2023-02-27)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.2.2-alpha...0.2.3-alpha)

## [0.2.2-alpha](https://github.com/christianhelle/refitter/tree/0.2.2-alpha) (2023-02-25)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.2.1-alpha...0.2.2-alpha)

## [0.2.1-alpha](https://github.com/christianhelle/refitter/tree/0.2.1-alpha) (2023-02-25)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.2.0-alpha...0.2.1-alpha)

## [0.2.0-alpha](https://github.com/christianhelle/refitter/tree/0.2.0-alpha) (2023-02-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.1.5-alpha...0.2.0-alpha)

## [0.1.5-alpha](https://github.com/christianhelle/refitter/tree/0.1.5-alpha) (2023-02-18)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.1.4-alpha...0.1.5-alpha)

## [0.1.4-alpha](https://github.com/christianhelle/refitter/tree/0.1.4-alpha) (2023-02-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.1.3-alpha...0.1.4-alpha)

## [0.1.3-alpha](https://github.com/christianhelle/refitter/tree/0.1.3-alpha) (2023-02-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/c22986295fdf6f4dfb6f07b511e47d34f03b93e5...0.1.3-alpha)



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
