# Parker History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.

### 2026-04-17: Release Compatibility Audit (1.7.3 to HEAD)

**Task**: Audit generator-core changes between 1.7.3 and HEAD for breaking behavior ahead of major release.

**Key Finding - CONFIRMED BREAKING CHANGE**: 
- `GenerateAuthenticationHeader` property changed from `bool` to `AuthenticationHeaderStyle` enum (commit 7dbf6c0c, 14101a49).
- **Impact**: Source compatibility break. Existing .refitter files using `"generateAuthenticationHeader": true` will fail JSON deserialization.
- **Runtime impact**: `AuthenticationHeaderStyle` defaults to `None` (0), whereas old `bool` defaulted to `false`. Users who explicitly set `true` will see authentication headers **disappear** after upgrade unless they migrate to `"authenticationHeaderStyle": "Parameter"`.
- **Confidence**: 100% - This is a hard breaking change affecting settings deserialization.

**Non-Breaking Changes (Bug Fixes, Safe Additions)**:
1. **JsonConverter attribute placement** (1b9a76c8): Moved from enum properties to enum types. Fixes user ability to override converters. Not breaking - only affects previously broken scenarios (hyphened enum values).
2. **Stack overflow fix** (3d9cdb6c): Schema traversal cycle detection. Only affects previously crashing inputs (recursive schemas).
3. **PropertyNamingPolicy** (76230c9e): New optional setting, defaults to `PascalCase` (existing behavior). No break.
4. **Auto-enable GenerateOptionalPropertiesAsNullable** (29de01ed): Only activates when `GenerateNullableReferenceTypes` is already true. Behavioral change but makes nullable reference types actually work correctly - likely expected behavior.
5. **Digit-prefixed property names** (fdcf675b): Now prefixed with underscore for C# compilation. Fixes previously invalid generated code.
6. **Multipart form-data extraction** (57498ea5): Fixes missing parameters. Only affects previously incomplete code generation.
7. **Method naming in ByTag mode** (79939948): Scopes per-interface instead of global. Prevents unexpected numeric suffixes - improves output quality.
8. **OneOf with discriminator transformation** (CSharpClientGeneratorFactory): Converts to allOf pattern for NSwag. Fixes undefined anonymous types.
9. **ContractTypeSuffix, SecurityScheme, OpenApiPaths, GenerateJsonSerializerContext**: New optional features with safe defaults.

**Audit Result**: BREAKING CHANGE confirmed. Team consensus: major version 2.0.0 required.

**Code Patterns Learned**:
- Schema traversal must use instance-based visited tracking with `HashSet<JsonSchema>` and `ActualSchema` resolution to prevent stack overflow.
- Settings format changes (type changes, not just new properties) constitute hard breaks for .refitter file users.
- NSwag's oneOf/anyOf handling requires preprocessing transformation to allOf for proper inheritance generation.

**File Locations**:
- Core settings: `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`
- Schema preprocessing: `src/Refitter.Core/CSharpClientGeneratorFactory.cs`
- Generator logic: `src/Refitter.Core/RefitGenerator.cs`
- Parameter extraction: `src/Refitter.Core/ParameterExtractor.cs`
