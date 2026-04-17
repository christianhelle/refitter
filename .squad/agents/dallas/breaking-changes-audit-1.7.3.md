# Breaking Changes Audit: 1.7.3 → HEAD

**Auditor:** Dallas (Tooling Dev)  
**Date:** 2026-04-16  
**Scope:** CLI flags, .refitter file format, MSBuild integration, Source Generator, packaging  
**Commits analyzed:** 353 commits from 1.7.3 to HEAD

---

## Executive Summary

**BREAKING CHANGES FOUND: 1**

One breaking change was introduced in the upgrade to Spectre.Console.Cli v0.55.0. All other changes are **non-breaking** — they add new features or fix bugs while preserving backward compatibility.

---

## ⚠️ BREAKING CHANGE

### 1. Spectre.Console.Cli v0.55.0 Upgrade (Commit: 08392e69)

**Impact:** Internal API surface only — does NOT affect end users.

**Details:**
- Changed `public override` methods to `protected override` in `GenerateCommand.cs`:
  - `Validate()` method (line 26)
  - `ExecuteAsync()` method (line 38)
- This is a breaking change for anyone who subclasses `GenerateCommand` (internal implementation detail).
- **End-user impact:** NONE. CLI arguments, .refitter file format, and MSBuild integration are unaffected.

**Mitigation:** Not required for users. This is an internal implementation change only.

---

## ✅ NON-BREAKING ADDITIONS

### New CLI Options (All backward compatible)

1. **`--property-naming-policy`** (1.8.0-preview.100)
   - Controls how contract properties are named
   - Values: `PascalCase` (default), `PreserveOriginal`
   - Default: `PascalCase` (existing behavior)
   - **Non-breaking:** Defaults preserve existing behavior

2. **`--generate-authentication-header`** (1.8.0-preview.99)
   - Controls generation of Authorization header support
   - Values: `None` (default), `Parameter`, `Method`
   - Default: `None` (no auth code generated, existing behavior)
   - **Non-breaking:** Defaults to existing behavior

3. **`--security-scheme`** (1.8.0-preview.99)
   - Specifies which security scheme to generate auth headers for
   - Default: null (all schemes, existing behavior when auth is enabled)
   - **Non-breaking:** Only used when auth generation is explicitly enabled

4. **`--json-serializer-context`** (1.8.0-preview.99)
   - Generate JsonSerializerContext for AOT compilation
   - Default: `false` (disabled)
   - **Non-breaking:** Opt-in feature

### New .refitter File Properties (All backward compatible)

1. **`openApiPaths`** (array) — Alternative to `openApiPath` for merging multiple specs
   - Required only if `openApiPath` is not specified
   - **Non-breaking:** Existing files with `openApiPath` continue to work

2. **`propertyNamingPolicy`** — Controls property naming
   - Values: `PascalCase` (default), `PreserveOriginal`
   - Default: `PascalCase`
   - **Non-breaking:** Defaults preserve existing behavior

3. **`authenticationHeaderStyle`** (enum) — Replaces `generateAuthenticationHeader` (bool)
   - Values: `None`, `Method`, `Parameter`
   - Default: `None`
   - **Migration path:** The old `generateAuthenticationHeader` boolean property is NOT removed; it still works via compatibility shim
   - **Non-breaking:** Old property still functional

4. **`securityScheme`** (string) — Specifies security scheme for auth headers
   - Default: null (all schemes)
   - **Non-breaking:** Additive feature

5. **`generateJsonSerializerContext`** (bool) — AOT support
   - Default: `false`
   - **Non-breaking:** Opt-in feature

6. **`contractTypeSuffix`** (string) — Suffix for contract type names
   - Default: null (no suffix)
   - **Non-breaking:** Opt-in feature

7. **`contractsNamespace`** (string) — Separate namespace for contracts
   - Default: null (uses main namespace)
   - **Non-breaking:** Additive feature

### Settings File Deserialization Changes (Issue #998 fix)

**Commits:** 46e2d5b6, 20b6014e, 9122cd14, c34d49c2

**Changes:**
- Settings file is now deserialized **before** CLI argument processing
- Output path resolution logic hardened
- Default value checks removed for `outputFolder` when it equals `./Generated`

**Impact:** Bug fixes, not breaking changes. Users may see **improved** behavior:
- `.refitter` files now properly respected over CLI defaults
- MSBuild integration on first clean build now works correctly
- Output file naming from `.refitter` files now honored

**Backward compatibility:** Preserved. Existing workflows continue to work, with bug fixes.

### Deprecated (But Still Functional) Settings

**`dependencyInjectionSettings.usePolly`** — Marked `[Obsolete]` in DependencyInjectionSettings.cs
- Replacement: `transientErrorHandler` (values: `None`, `Polly`, `HttpResilience`)
- **Non-breaking:** Still functional via getter/setter compatibility shim
- Users should migrate, but old config files continue to work

### Other Non-Breaking Changes

1. **InlineJsonConverters behavior clarified** (Issue #300, PR #938)
   - `[JsonConverter]` now placed on enum **type** instead of enum **properties**
   - Default: `true` (existing behavior, but placement changed for better customization)
   - **Impact:** Generated code structure changes, but serialization behavior identical
   - **Non-breaking:** No user action required

2. **Multiple OpenAPI spec support** (PR #904)
   - New `openApiPaths` array property
   - **Non-breaking:** Additive feature

3. **Custom format mappings** (PR #927, Issue #438)
   - New configuration for custom type mappings
   - **Non-breaking:** Opt-in feature

4. **Contract type suffix** (Issue #193)
   - New `contractTypeSuffix` setting
   - **Non-breaking:** Opt-in feature

5. **Schema alias handling fixes** (Issues #991, #992, PR #996, #997)
   - Bug fixes for invalid property names and duplicate keys
   - **Non-breaking:** Fixes errors, improves reliability

6. **Header parameter sanitization** (PR #977)
   - Dashes in security header names now sanitized for C# variable names
   - **Non-breaking:** Fixes code generation errors

---

## MSBuild Integration

**Changes reviewed:**
- `Refitter.MSBuild.targets`: No breaking changes detected
- `RefitterGenerateTask.cs`: No API changes
- Build-time behavior: Improved with Issue #998 fixes

**Conclusion:** MSBuild integration remains fully backward compatible.

---

## Source Generator

**Changes reviewed:**
- `RefitterSourceGenerator.cs`: No breaking changes detected
- `.refitter` file discovery and parsing: Unchanged
- Code generation pipeline: Enhanced with new features, defaults preserved

**Conclusion:** Source generator remains fully backward compatible.

---

## Packaging & CI

**Changes reviewed:**
- No changes to package output structure
- No changes to required dependencies beyond Spectre.Console.Cli (internal)
- No changes to supported target frameworks
- CI workflows enhanced (no breaking changes)

**Conclusion:** Packaging and deployment remain fully backward compatible.

---

## Documentation Updates

The following documentation was updated to reflect new features:
- README.md (CLI usage examples)
- docs/docfx_project/articles/refitter-file-format.md (.refitter schema)
- src/Refitter.MSBuild/README.md (MSBuild examples)

All updates are **additive** — no removal of documented features.

---

## Testing & Validation

**Regression test coverage for new features:**
- Issue #998 output path resolution: ✅ (commits c34d49c2, 9122cd14)
- Schema alias handling: ✅ (commit cb0ecd8a)
- Property name sanitization: ✅ (commits in PR #996, #997)
- Bearer auth generation: ✅ (PR #936)

**Smoke tests status:** Passing (commit a8252d83)

---

## VERDICT

**SAFE TO RELEASE AS NON-BREAKING (MINOR OR PATCH)**

### Recommendations:

1. **Version bump:** This should be a **minor version** (1.8.0), not a major version
   - All changes are backward compatible
   - New features are opt-in
   - Bug fixes improve reliability without breaking existing behavior

2. **Migration guide:** NOT REQUIRED
   - No mandatory user action needed
   - Optional: Document new features and improvements

3. **Deprecation notices:**
   - `dependencyInjectionSettings.usePolly` → Recommend users migrate to `transientErrorHandler`
   - No removal timeline needed; compatibility shim can remain indefinitely

4. **Release notes should highlight:**
   - Bug fixes (Issue #998, #991, #992, #973)
   - New property naming control
   - New authentication header generation
   - AOT compilation support
   - Multiple OpenAPI spec support
   - All backward compatible

---

## Confidence Level

**HIGH** — Comprehensive review of 353 commits covering:
- ✅ All CLI option changes
- ✅ All .refitter file format changes
- ✅ MSBuild integration
- ✅ Source generator
- ✅ Core settings classes
- ✅ Dependency injection settings
- ✅ Documentation updates
- ✅ Test coverage

---

## Appendix: Key Commits Reviewed

- `08392e69` — Spectre.Console.Cli breaking change (internal only)
- `46e2d5b6` — Settings deserialization improvements
- `20b6014e` — Output path resolution (Issue #998 fix)
- `904` (PR) — Multiple OpenAPI specs support
- `897` (PR) — Authentication header generation
- `938` (PR) — JsonConverter placement fix
- `969` (PR) — PropertyNamingPolicy support
- `996`, `997` (PRs) — Schema alias and property name sanitization

---

**End of Audit**
