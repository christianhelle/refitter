# Hockney — History

## Core Context

**Project:** Refitter — generates C# Refit interfaces and contracts from OpenAPI (Swagger) specs  
**User:** Christian Helle  
**Stack:** C# / .NET, TUnit, FluentAssertions  
**Repo root:** C:/projects/christianhelle/refitter  
**Solution:** src/Refitter.slnx  

My domain: src/Refitter.Tests/, src/Refitter.SourceGenerator.Tests/.

Test pattern: class in Refitter.Tests.Examples, const OpenAPI spec string, async [Test] methods using GenerateCode(), assertions via FluentAssertions, BuildHelper.BuildCSharp(code).Should().BeTrue() to verify generated code compiles.

Test resources (OpenAPI specs): src/Refitter.Tests/Resources/V2/ and V3/. Use SwaggerPetstore.json for general testing.

Run tests: dotnet test -c Release src/Refitter.slnx (~5 min — NEVER cancel). Network test failures in sandboxed environments are acceptable.

**Framework Details:** TUnit 1.15.11 + FluentAssertions 8.8.0, targets net10.0. BuildHelper shells out to dotnet build for compilation verification. Test scale: 103 files, 58 in Examples/, ~609 [Test] methods.

**2025 Work:** 103-file test suite audit; identified 40+ well-covered features (collection formats, polymorphic serialization, DI, Apizr, multiple interfaces, filtering, schema trimming, inheritance). Implemented 31 new tests across 8 files to fill coverage gaps in JsonSerializerContextGenerator, SchemaCleaner discriminators, RefitGenerator multiple files, ParameterExtractor edge cases, CSharpClientGeneratorFactory integer format handling, OpenApiDocumentFactory multi-file merge strategy, and CustomCSharpTypeResolver numeric type mapping.

## Learnings

### 2026 Recent Work — Issue #944 Unicode XML Documentation

Added regression test coverage for non-ASCII XML documentation fix:
- Unit test: XmlDocumentationGeneratorTests.cs validates Unicode preservation and escape sequence decoding
- Integration test: GenerateStatusCodeCommentsTests.cs end-to-end spec→generation→compilation check
- Tests confirm fix works across full pipeline; all 1415 tests pass

### 2026 Investigation — Issue #967 Raw Property Name Support

**Findings:** Raw snake_case property names (e.g., `payMethod_SumBank`) are **valid C# and safe with System.Text.Json**. Compiled and tested standalone console app confirming deserialization/serialization work correctly.

**Current State:** Feature infrastructure exists (`CodeGeneratorSettings.PropertyNameGenerator` property, NJsonSchema integration in `CSharpClientGeneratorFactory`) but is NOT exposed to users:
1. No CLI option `--property-name-generator`
2. JSON schema still lists `propertyNameGenerator` despite `[JsonIgnore]` in code
3. SerializerTests explicitly exclude PropertyNameGenerator from round-trip validation

**Safety Assessment:**
- ✅ C# compilation: Valid identifiers, no syntax errors
- ✅ System.Text.Json: Correctly maps via `[JsonPropertyName]` attribute
- ⚠️ Edge cases: Hyphens/invalid chars would break (need validation), reserved keywords require `@` prefix
- ⚠️ Config consistency: PropertyNameGenerator field marked `[JsonIgnore]`, so custom generators won't serialize

**Test Design Recommendations (if feature enabled):**
- Can_Generate_Code_With_Raw_Property_Names()
- Generated_Code_Uses_Raw_Property_Names (assertion: no PascalCase)
- Can_Build_Generated_Code_With_Raw_Names
- Serialization_Preserves_Mapping_With_Raw_Names (JSON roundtrip)
- Invalid_Property_Names_Are_Rejected (hyphens, spaces, etc.)

**Feasibility:** 8.5 hours to fully expose (CLI option, tests, documentation). Feature is technically proven safe; blockers are purely user-exposure and validation design decisions.

## 2026-03-25: Issue #967 Team Assessment

Team consensus reached on GitHub issue #967 (Preserve Original Property Names):
- ✅ APPROVED FOR IMPLEMENTATION
- Safety verdict confirmed: Raw property names compile safely with System.Text.Json
- Recommended test coverage: reserved keywords, invalid identifiers, build success, serialization round-trip
- Edge cases requiring mitigation: hyphens, leading digits, name collisions
- Sanitization strategy: preserve casing/underscores, validate C# identifier legality

Consolidated decision entry created in decisions.md. See orchestration logs for full team assessment.

