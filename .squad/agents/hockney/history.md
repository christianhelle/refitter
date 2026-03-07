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
