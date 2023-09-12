# Changelog

## [Unreleased](https://github.com/christianhelle/refitter/tree/HEAD)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.5...HEAD)

**Implemented enhancements:**

- Skip default values when collecting feature usages for Analytics [\#145](https://github.com/christianhelle/refitter/pull/145) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Unexpected initial token 'Boolean' when populating object [\#138](https://github.com/christianhelle/refitter/issues/138)

**Closed issues:**

- Improving documentation for --settings-file cli tool parameter [\#148](https://github.com/christianhelle/refitter/issues/148)

**Merged pull requests:**

- Fix documentation regarding --settings-file usage [\#152](https://github.com/christianhelle/refitter/pull/152) ([christianhelle](https://github.com/christianhelle))
- docs: add Ekkeir as a contributor for bug [\#151](https://github.com/christianhelle/refitter/pull/151) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add Ekkeir as a contributor for doc [\#150](https://github.com/christianhelle/refitter/pull/150) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Add matchPath option in example .refitter file in README [\#146](https://github.com/christianhelle/refitter/pull/146) ([christianhelle](https://github.com/christianhelle))
- Bump Microsoft.OpenApi.Readers from 1.6.7 to 1.6.8 [\#144](https://github.com/christianhelle/refitter/pull/144) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.7.5](https://github.com/christianhelle/refitter/tree/0.7.5) (2023-09-07)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.4...0.7.5)

**Fixed bugs:**

- Filter `--tag` broken [\#142](https://github.com/christianhelle/refitter/issues/142)

**Merged pull requests:**

- Fix \#142 by changing includeTags filtering and restoring collection snapshots [\#143](https://github.com/christianhelle/refitter/pull/143) ([kirides](https://github.com/kirides))

## [0.7.4](https://github.com/christianhelle/refitter/tree/0.7.4) (2023-09-05)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.3...0.7.4)

**Implemented enhancements:**

- Introduce OpenAPI validation [\#141](https://github.com/christianhelle/refitter/pull/141) ([christianhelle](https://github.com/christianhelle))
- Show help text if no arguments are passed [\#140](https://github.com/christianhelle/refitter/pull/140) ([christianhelle](https://github.com/christianhelle))
- Resolve SonarCloud Code Smells [\#137](https://github.com/christianhelle/refitter/pull/137) ([christianhelle](https://github.com/christianhelle))
- Fix issue when downloading an OpenAPI spec from a URL that returns a GZIP stream [\#136](https://github.com/christianhelle/refitter/pull/136) ([christianhelle](https://github.com/christianhelle))
- Feature: Filtering of endpoints and tags [\#132](https://github.com/christianhelle/refitter/pull/132) ([kirides](https://github.com/kirides))

**Fixed bugs:**

- Downloading OpenAPI specification from URI using `content-encoding: gzip` fails [\#135](https://github.com/christianhelle/refitter/issues/135)
- Proposal: filter generated interfaces [\#131](https://github.com/christianhelle/refitter/issues/131)

**Merged pull requests:**

- docs: add danpowell88 as a contributor for bug [\#139](https://github.com/christianhelle/refitter/pull/139) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Update docs with new --tag and --match-path features [\#134](https://github.com/christianhelle/refitter/pull/134) ([christianhelle](https://github.com/christianhelle))
- Bump H.Generators.Extensions from 1.16.0 to 1.18.0 [\#133](https://github.com/christianhelle/refitter/pull/133) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Microsoft.NET.Test.Sdk from 17.7.1 to 17.7.2 [\#130](https://github.com/christianhelle/refitter/pull/130) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.7.3](https://github.com/christianhelle/refitter/tree/0.7.3) (2023-08-25)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.2...0.7.3)

**Implemented enhancements:**

- Fix incorrect assembly and file version [\#129](https://github.com/christianhelle/refitter/pull/129) ([christianhelle](https://github.com/christianhelle))
- Improve local smoke tests and introduce dev containers support [\#128](https://github.com/christianhelle/refitter/pull/128) ([christianhelle](https://github.com/christianhelle))
- Add support for using .refitter file from CLI [\#127](https://github.com/christianhelle/refitter/pull/127) ([christianhelle](https://github.com/christianhelle))
- Fix parameters' casing in openAPI document are not honoured in Refit interface methods [\#125](https://github.com/christianhelle/refitter/pull/125) ([christianhelle](https://github.com/christianhelle))
- Exclude from generated code from code coverage [\#123](https://github.com/christianhelle/refitter/pull/123) ([christianhelle](https://github.com/christianhelle))
- Add Codecov workflow and badge [\#120](https://github.com/christianhelle/refitter/pull/120) ([christianhelle](https://github.com/christianhelle))
- Fix duplicate Accept headers [\#119](https://github.com/christianhelle/refitter/pull/119) ([christianhelle](https://github.com/christianhelle))
- Fix issue where \[Headers\("Accept"\)\] is always generated [\#111](https://github.com/christianhelle/refitter/pull/111) ([christianhelle](https://github.com/christianhelle))
- Add Accept Request Header [\#108](https://github.com/christianhelle/refitter/pull/108) ([guillaumeserale](https://github.com/guillaumeserale))
- Resolve SonarCloud detected code smells [\#105](https://github.com/christianhelle/refitter/pull/105) ([christianhelle](https://github.com/christianhelle))
- Enable SonarCloud [\#103](https://github.com/christianhelle/refitter/pull/103) ([christianhelle](https://github.com/christianhelle))
- Source Generator Hack - Write to disk instead of the syntax tree [\#102](https://github.com/christianhelle/refitter/pull/102) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Parameters' casing in openAPI document are not honoured  in Refit interface methods [\#124](https://github.com/christianhelle/refitter/issues/124)
- Add support for using .refitter file from CLI [\#121](https://github.com/christianhelle/refitter/issues/121)
- Duplicate Accept Headers [\#118](https://github.com/christianhelle/refitter/issues/118)
- Missing "Accept" Request Header in generated files based on OAS 3.0  [\#107](https://github.com/christianhelle/refitter/issues/107)
- Refitter Source Generator - generated code not being picked up by Refit's Source Generator [\#100](https://github.com/christianhelle/refitter/issues/100)
- Remove source generator from docs and builds [\#101](https://github.com/christianhelle/refitter/pull/101) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Bump FluentAssertions from 6.11.0 to 6.12.0 [\#126](https://github.com/christianhelle/refitter/pull/126) ([dependabot[bot]](https://github.com/apps/dependabot))
- docs: add yadanilov19 as a contributor for ideas [\#122](https://github.com/christianhelle/refitter/pull/122) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Bump Microsoft.CodeAnalysis.CSharp from 4.6.0 to 4.7.0 [\#117](https://github.com/christianhelle/refitter/pull/117) ([dependabot[bot]](https://github.com/apps/dependabot))
- Update docs regarding accept headers [\#116](https://github.com/christianhelle/refitter/pull/116) ([christianhelle](https://github.com/christianhelle))
- Bump Microsoft.NET.Test.Sdk from 17.7.0 to 17.7.1 [\#115](https://github.com/christianhelle/refitter/pull/115) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump H.Generators.Extensions from 1.15.1 to 1.16.0 [\#114](https://github.com/christianhelle/refitter/pull/114) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NSwag.CodeGeneration.CSharp from 13.19.0 to 13.20.0 [\#113](https://github.com/christianhelle/refitter/pull/113) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NSwag.Core.Yaml from 13.19.0 to 13.20.0 [\#112](https://github.com/christianhelle/refitter/pull/112) ([dependabot[bot]](https://github.com/apps/dependabot))
- docs: add Roflincopter as a contributor for ideas [\#110](https://github.com/christianhelle/refitter/pull/110) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add guillaumeserale as a contributor for bug [\#109](https://github.com/christianhelle/refitter/pull/109) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Restore source generator in docs and builds [\#104](https://github.com/christianhelle/refitter/pull/104) ([christianhelle](https://github.com/christianhelle))

## [0.7.2](https://github.com/christianhelle/refitter/tree/0.7.2) (2023-08-07)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.1...0.7.2)

**Implemented enhancements:**

- Local smoke test bash script [\#98](https://github.com/christianhelle/refitter/pull/98) ([christianhelle](https://github.com/christianhelle))
- Small code cleanup in Generators [\#97](https://github.com/christianhelle/refitter/pull/97) ([kirides](https://github.com/kirides))
- Generate Multiple interfaces based on first Tag [\#95](https://github.com/christianhelle/refitter/pull/95) ([kirides](https://github.com/kirides))

**Merged pull requests:**

- Bump Microsoft.NET.Test.Sdk from 17.6.3 to 17.7.0 [\#99](https://github.com/christianhelle/refitter/pull/99) ([dependabot[bot]](https://github.com/apps/dependabot))
- docs: add kirides as a contributor for code [\#96](https://github.com/christianhelle/refitter/pull/96) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [0.7.1](https://github.com/christianhelle/refitter/tree/0.7.1) (2023-08-02)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.7.0...0.7.1)

**Implemented enhancements:**

- Rename source generator output to use .refitter file and replace extension with .g.cs [\#94](https://github.com/christianhelle/refitter/pull/94) ([christianhelle](https://github.com/christianhelle))
- Add support for generating multiple interfaces [\#93](https://github.com/christianhelle/refitter/pull/93) ([christianhelle](https://github.com/christianhelle))

## [0.7.0](https://github.com/christianhelle/refitter/tree/0.7.0) (2023-07-31)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.3...0.7.0)

**Implemented enhancements:**

- Add production tests for source generators [\#92](https://github.com/christianhelle/refitter/pull/92) ([christianhelle](https://github.com/christianhelle))
- Update NuGet package icon with 128x128 image [\#91](https://github.com/christianhelle/refitter/pull/91) ([christianhelle](https://github.com/christianhelle))
- Add NuGet package icon [\#90](https://github.com/christianhelle/refitter/pull/90) ([christianhelle](https://github.com/christianhelle))
- Introduce C\# Source Generator and the .refitter file format [\#86](https://github.com/christianhelle/refitter/pull/86) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Bump xunit.runner.visualstudio from 2.4.5 to 2.5.0 [\#89](https://github.com/christianhelle/refitter/pull/89) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Microsoft.CodeAnalysis.CSharp from 4.5.0 to 4.6.0 [\#88](https://github.com/christianhelle/refitter/pull/88) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump xunit from 2.4.2 to 2.5.0 [\#87](https://github.com/christianhelle/refitter/pull/87) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.6.3](https://github.com/christianhelle/refitter/tree/0.6.3) (2023-07-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.2...0.6.3)

**Implemented enhancements:**

- Add .editorconfig [\#85](https://github.com/christianhelle/refitter/pull/85) ([angelofb](https://github.com/angelofb))
- General improvements on code, docs, and workflows [\#84](https://github.com/christianhelle/refitter/pull/84) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Bump xunit from 2.4.2 to 2.5.0 [\#83](https://github.com/christianhelle/refitter/pull/83) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump xunit.runner.visualstudio from 2.4.5 to 2.5.0 [\#82](https://github.com/christianhelle/refitter/pull/82) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Microsoft.NET.Test.Sdk from 17.6.2 to 17.6.3 [\#81](https://github.com/christianhelle/refitter/pull/81) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.6.2](https://github.com/christianhelle/refitter/tree/0.6.2) (2023-06-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.1...0.6.2)

**Implemented enhancements:**

- Additonal Namespaces for generated Types [\#80](https://github.com/christianhelle/refitter/pull/80) ([angelofb](https://github.com/angelofb))

**Fixed bugs:**

- Generated code doesn't build if operationId contains spaces [\#78](https://github.com/christianhelle/refitter/issues/78)

## [0.6.1](https://github.com/christianhelle/refitter/tree/0.6.1) (2023-06-20)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.6.0...0.6.1)

**Fixed bugs:**

- DirectoryNotFoundException [\#76](https://github.com/christianhelle/refitter/issues/76)
- Fix support for spaces in operationid [\#79](https://github.com/christianhelle/refitter/pull/79) ([christianhelle](https://github.com/christianhelle))
- Fix DirectoryNotFoundException [\#77](https://github.com/christianhelle/refitter/pull/77) ([christianhelle](https://github.com/christianhelle))

## [0.6.0](https://github.com/christianhelle/refitter/tree/0.6.0) (2023-06-15)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.30...0.6.0)

**Implemented enhancements:**

- Enhanced HTTP status code 200 handling for API responses [\#74](https://github.com/christianhelle/refitter/pull/74) ([NoGRo](https://github.com/NoGRo))
- Introduce --use-iso-date-format CLI tool argument [\#73](https://github.com/christianhelle/refitter/pull/73) ([christianhelle](https://github.com/christianhelle))
- make use of new language features [\#72](https://github.com/christianhelle/refitter/pull/72) ([angelofb](https://github.com/angelofb))
- check if query parameter is an array [\#70](https://github.com/christianhelle/refitter/pull/70) ([angelofb](https://github.com/angelofb))

**Fixed bugs:**

- String parameters with format 'date' get no Format in the QueryAttribute [\#66](https://github.com/christianhelle/refitter/issues/66)

**Merged pull requests:**

- docs: add NoGRo as a contributor for code [\#75](https://github.com/christianhelle/refitter/pull/75) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add angelofb as a contributor for code [\#71](https://github.com/christianhelle/refitter/pull/71) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [0.5.30](https://github.com/christianhelle/refitter/tree/0.5.30) (2023-06-12)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.28...0.5.30)

**Fixed bugs:**

- Model definition with property named System results in class that does not compile [\#68](https://github.com/christianhelle/refitter/issues/68)
- Refitter fails to generate FormData parameter for file upload [\#62](https://github.com/christianhelle/refitter/issues/62)
- Member with the same signature is already declared [\#58](https://github.com/christianhelle/refitter/issues/58)
- Generated Method names contains invalid characters.  [\#56](https://github.com/christianhelle/refitter/issues/56)
- Remove inline namespace imports [\#69](https://github.com/christianhelle/refitter/pull/69) ([christianhelle](https://github.com/christianhelle))
- Add support for multipart/form-data file uploads [\#65](https://github.com/christianhelle/refitter/pull/65) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- docs: add brease-colin as a contributor for bug [\#67](https://github.com/christianhelle/refitter/pull/67) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [0.5.28](https://github.com/christianhelle/refitter/tree/0.5.28) (2023-06-08)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.27...0.5.28)

**Implemented enhancements:**

- Generated files have inconsistent lined endings  [\#57](https://github.com/christianhelle/refitter/issues/57)
- Fix inconsistent line endings in generated file [\#60](https://github.com/christianhelle/refitter/pull/60) ([christianhelle](https://github.com/christianhelle))
- Configure All-Contributors bot [\#44](https://github.com/christianhelle/refitter/pull/44) ([christianhelle](https://github.com/christianhelle))

**Fixed bugs:**

- Generated output has Task return type instead of expected Task\<T\> [\#41](https://github.com/christianhelle/refitter/issues/41)
- Fix broken interface generated from HubSpot API's [\#64](https://github.com/christianhelle/refitter/pull/64) ([christianhelle](https://github.com/christianhelle))

**Closed issues:**

- Add Contributors using All-Contributors [\#46](https://github.com/christianhelle/refitter/issues/46)

**Merged pull requests:**

- docs: add richardhu-lmg as a contributor for bug [\#63](https://github.com/christianhelle/refitter/pull/63) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Bump Microsoft.NET.Test.Sdk from 17.6.1 to 17.6.2 [\#61](https://github.com/christianhelle/refitter/pull/61) ([dependabot[bot]](https://github.com/apps/dependabot))
- docs: add damianh as a contributor for bug [\#59](https://github.com/christianhelle/refitter/pull/59) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- Bump Microsoft.NET.Test.Sdk from 17.6.0 to 17.6.1 [\#55](https://github.com/christianhelle/refitter/pull/55) ([dependabot[bot]](https://github.com/apps/dependabot))
- docs: add Roflincopter as a contributor for code [\#54](https://github.com/christianhelle/refitter/pull/54) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add guillaumeserale as a contributor for code [\#53](https://github.com/christianhelle/refitter/pull/53) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add kirides as a contributor for bug [\#52](https://github.com/christianhelle/refitter/pull/52) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add m7clarke as a contributor for bug [\#51](https://github.com/christianhelle/refitter/pull/51) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add 1kvin as a contributor for bug [\#50](https://github.com/christianhelle/refitter/pull/50) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add yrki as a contributor for code [\#49](https://github.com/christianhelle/refitter/pull/49) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add kgamecarter as a contributor for code [\#48](https://github.com/christianhelle/refitter/pull/48) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add distantcam as a contributor for code [\#47](https://github.com/christianhelle/refitter/pull/47) ([allcontributors[bot]](https://github.com/apps/allcontributors))
- docs: add neoGeneva as a contributor for code [\#45](https://github.com/christianhelle/refitter/pull/45) ([allcontributors[bot]](https://github.com/apps/allcontributors))

## [0.5.27](https://github.com/christianhelle/refitter/tree/0.5.27) (2023-05-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.26...0.5.27)

**Fixed bugs:**

- Fixes Interface generator in case the response uses a ref in yaml spec. [\#42](https://github.com/christianhelle/refitter/pull/42) ([Roflincopter](https://github.com/Roflincopter))

**Merged pull requests:**

- Bump coverlet.collector from 3.2.0 to 6.0.0 [\#40](https://github.com/christianhelle/refitter/pull/40) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Spectre.Console.Cli from 0.46.0 to 0.47.0 [\#39](https://github.com/christianhelle/refitter/pull/39) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Exceptionless from 6.0.1 to 6.0.2 [\#38](https://github.com/christianhelle/refitter/pull/38) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Microsoft.NET.Test.Sdk from 17.5.0 to 17.6.0 [\#37](https://github.com/christianhelle/refitter/pull/37) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.5.26](https://github.com/christianhelle/refitter/tree/0.5.26) (2023-05-11)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.25...0.5.26)

## [0.5.25](https://github.com/christianhelle/refitter/tree/0.5.25) (2023-05-10)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.3...0.5.25)

**Implemented enhancements:**

- Introduce Feature Usage tracking to Exceptionless [\#35](https://github.com/christianhelle/refitter/pull/35) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Bump Exceptionless from 6.0.0 to 6.0.1 [\#36](https://github.com/christianhelle/refitter/pull/36) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.5.3](https://github.com/christianhelle/refitter/tree/0.5.3) (2023-05-05)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.2...0.5.3)

**Implemented enhancements:**

- Introduce Error logging using Exceptionless [\#32](https://github.com/christianhelle/refitter/pull/32) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Bump NSwag.CodeGeneration.CSharp from 13.18.5 to 13.19.0 [\#34](https://github.com/christianhelle/refitter/pull/34) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NSwag.Core.Yaml from 13.18.5 to 13.19.0 [\#33](https://github.com/christianhelle/refitter/pull/33) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NSwag.Core.Yaml from 13.18.3 to 13.18.5 [\#31](https://github.com/christianhelle/refitter/pull/31) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NSwag.CodeGeneration.CSharp from 13.18.3 to 13.18.5 [\#30](https://github.com/christianhelle/refitter/pull/30) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NSwag.Core.Yaml from 13.18.2 to 13.18.3 [\#29](https://github.com/christianhelle/refitter/pull/29) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NSwag.CodeGeneration.CSharp from 13.18.2 to 13.18.3 [\#28](https://github.com/christianhelle/refitter/pull/28) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.5.2](https://github.com/christianhelle/refitter/tree/0.5.2) (2023-05-02)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.1...0.5.2)

**Fixed bugs:**

- OperationHeaders generation problem with headers containing a - [\#26](https://github.com/christianhelle/refitter/issues/26)
- Fix for \#26 [\#27](https://github.com/christianhelle/refitter/pull/27) ([guillaumeserale](https://github.com/guillaumeserale))

## [0.5.1](https://github.com/christianhelle/refitter/tree/0.5.1) (2023-05-01)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.5.0...0.5.1)

**Implemented enhancements:**

- Add `CancellationToken cancellationToken = default` to generated Methods [\#19](https://github.com/christianhelle/refitter/issues/19)
- Add --no-operation-headers CLI tool argument [\#25](https://github.com/christianhelle/refitter/pull/25) ([christianhelle](https://github.com/christianhelle))
- Add injecting header parameters [\#24](https://github.com/christianhelle/refitter/pull/24) ([guillaumeserale](https://github.com/guillaumeserale))

## [0.5.0](https://github.com/christianhelle/refitter/tree/0.5.0) (2023-04-28)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.4.2...0.5.0)

**Implemented enhancements:**

- Add support for using Cancellation Tokens [\#23](https://github.com/christianhelle/refitter/pull/23) ([christianhelle](https://github.com/christianhelle))

## [0.4.2](https://github.com/christianhelle/refitter/tree/0.4.2) (2023-04-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.4.1...0.4.2)

**Implemented enhancements:**

- Allow specifying access modifiers for generated classes [\#20](https://github.com/christianhelle/refitter/issues/20)
- Support for .net6.0 / Reasoning why .net7 is required [\#17](https://github.com/christianhelle/refitter/issues/17)
- Release v0.4.2 [\#22](https://github.com/christianhelle/refitter/pull/22) ([christianhelle](https://github.com/christianhelle))
- Add support for generating 'internal' types [\#21](https://github.com/christianhelle/refitter/pull/21) ([christianhelle](https://github.com/christianhelle))
- Charge Target Framework to .NET 6.0 \(LTS\) [\#18](https://github.com/christianhelle/refitter/pull/18) ([christianhelle](https://github.com/christianhelle))

**Merged pull requests:**

- Bump FluentAssertions from 6.10.0 to 6.11.0 [\#16](https://github.com/christianhelle/refitter/pull/16) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.4.1](https://github.com/christianhelle/refitter/tree/0.4.1) (2023-04-03)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.4.0...0.4.1)

**Implemented enhancements:**

- Functionality to use URL [\#15](https://github.com/christianhelle/refitter/pull/15) ([yrki](https://github.com/yrki))

## [0.4.0](https://github.com/christianhelle/refitter/tree/0.4.0) (2023-03-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.17...0.4.0)

**Implemented enhancements:**

- Add support for generating IApiResponse\<T\> as return types [\#13](https://github.com/christianhelle/refitter/issues/13)

## [0.3.17](https://github.com/christianhelle/refitter/tree/0.3.17) (2023-03-24)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.16...0.3.17)

**Implemented enhancements:**

- Add support for generating IApiResponse\<T\> as return types [\#14](https://github.com/christianhelle/refitter/pull/14) ([christianhelle](https://github.com/christianhelle))

## [0.3.16](https://github.com/christianhelle/refitter/tree/0.3.16) (2023-03-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.4...0.3.16)

**Implemented enhancements:**

- Various name encoding fixes, fix multiline descriptions [\#12](https://github.com/christianhelle/refitter/pull/12) ([neoGeneva](https://github.com/neoGeneva))

## [0.3.4](https://github.com/christianhelle/refitter/tree/0.3.4) (2023-03-22)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.3...0.3.4)

**Implemented enhancements:**

- Please add support for kebab string casing parameters [\#10](https://github.com/christianhelle/refitter/issues/10)

**Fixed bugs:**

- Add support for kebab-string-casing parameters [\#11](https://github.com/christianhelle/refitter/pull/11) ([christianhelle](https://github.com/christianhelle))

## [0.3.3](https://github.com/christianhelle/refitter/tree/0.3.3) (2023-03-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.2...0.3.3)

**Fixed bugs:**

- Handle multipart/form-data parameters [\#9](https://github.com/christianhelle/refitter/pull/9) ([distantcam](https://github.com/distantcam))

## [0.3.2](https://github.com/christianhelle/refitter/tree/0.3.2) (2023-03-16)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.1...0.3.2)

**Fixed bugs:**

- Missing path parameters in parent [\#8](https://github.com/christianhelle/refitter/issues/8)
- Parameters from the query do not add into the resulting interface [\#5](https://github.com/christianhelle/refitter/issues/5)
- fix path parameters in parent [\#7](https://github.com/christianhelle/refitter/pull/7) ([kgamecarter](https://github.com/kgamecarter))

## [0.3.1](https://github.com/christianhelle/refitter/tree/0.3.1) (2023-03-14)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.3.0...0.3.1)

**Fixed bugs:**

- Fix missing support for Query Parameters [\#6](https://github.com/christianhelle/refitter/pull/6) ([christianhelle](https://github.com/christianhelle))

## [0.3.0](https://github.com/christianhelle/refitter/tree/0.3.0) (2023-03-14)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.2.4-alpha...0.3.0)

**Implemented enhancements:**

- Introduce --interface-only CLI tool argument [\#4](https://github.com/christianhelle/refitter/pull/4) ([christianhelle](https://github.com/christianhelle))

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

**Merged pull requests:**

- Bump Microsoft.NET.Test.Sdk from 17.4.1 to 17.5.0 [\#3](https://github.com/christianhelle/refitter/pull/3) ([dependabot[bot]](https://github.com/apps/dependabot))

## [0.1.5-alpha](https://github.com/christianhelle/refitter/tree/0.1.5-alpha) (2023-02-18)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.1.4-alpha...0.1.5-alpha)

## [0.1.4-alpha](https://github.com/christianhelle/refitter/tree/0.1.4-alpha) (2023-02-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/0.1.3-alpha...0.1.4-alpha)

## [0.1.3-alpha](https://github.com/christianhelle/refitter/tree/0.1.3-alpha) (2023-02-17)

[Full Changelog](https://github.com/christianhelle/refitter/compare/c22986295fdf6f4dfb6f07b511e47d34f03b93e5...0.1.3-alpha)

**Merged pull requests:**

- Bump Microsoft.NET.Test.Sdk from 17.3.2 to 17.4.1 [\#2](https://github.com/christianhelle/refitter/pull/2) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump coverlet.collector from 3.1.2 to 3.2.0 [\#1](https://github.com/christianhelle/refitter/pull/1) ([dependabot[bot]](https://github.com/apps/dependabot))



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
