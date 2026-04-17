# Squad Decisions

## 2026-04-17

### Release Compatibility Audit: 1.7.3 → HEAD (All Agents Consensus)

**Verdict:** BREAKING CHANGES FOUND. Cannot be marketed as non-breaking release. Major version bump (2.0.0) required.

#### Breaking Changes (2 Confirmed)

1. **Auth Property Renamed (MEDIUM RISK)**
   - `.refitter` setting: `generateAuthenticationHeader` (bool) → `authenticationHeaderStyle` (enum: None, Method, Parameter)
   - No backward compatibility layer or JSON mapping
   - Old JSON key silently ignored; defaults to `AuthenticationHeaderStyle.None`
   - Affected: users with `"generateAuthenticationHeader": true` in `.refitter` files
   - Evidence: Commits 7dbf6c0c, 14101a49; confirmed by Lambert's deserialization tests
   - Migration: Replace `"generateAuthenticationHeader": true` with `"authenticationHeaderStyle": "Method"` or `"Parameter"`

2. **Source Generator Disk Files (HIGH RISK)**
   - Source generator no longer writes `.g.cs` files to disk
   - Changed from `File.WriteAllText()` to `context.AddSource()` (Roslyn best practice)
   - Fixes issues #635, #520, #310 (file locking, process access errors)
   - Affected: source generator users expecting physical files in `./Generated` folder
   - Users must view generated code via IDE or switch to CLI/MSBuild for disk files
   - Evidence: Commit f853bcf2 (PR #923); confirmed by Dallas tie-breaker audit

#### Non-Breaking Changes

- **MSBuild output path fix (Issue #998):** NOT a breaking change. MSBuild now respects default `./Generated` instead of incorrectly outputting to `.refitter` directory. This is a bug fix, not a break. Users relying on old buggy behavior can set `"outputFolder": "."` explicitly.
- **8 Additive Features** (all backward compatible with safe defaults):
  - PropertyNamingPolicy (defaults to PascalCase)
  - OpenApiPaths (multi-spec merge)
  - ContractTypeSuffix
  - GenerateJsonSerializerContext (AOT)
  - SecurityScheme filtering
  - CustomTemplateDirectory
  - New CLI options for all above
  - Auto-enable GenerateOptionalPropertiesAsNullable (scoped)
- **4 Bug Fixes** (only affect previously broken inputs):
  - Stack overflow in recursive schemas
  - Digit-prefixed property naming (invalid C# identifiers)
  - Multipart form-data parameter extraction
  - OneOf discriminator handling
- **Generated Code Quality Improvements:**
  - JsonConverter attribute placement: properties → enum types (semantically equivalent)
  - Method naming in ByTag mode: numeric suffixes now scoped per-interface

#### Release Recommendation

- **Version:** 2.0.0 (major bump required)
- **CHANGELOG:** Document both breaking changes with clear migration paths
- **Migration Guide:** Provide search/replace instructions and generated-code viewing guidance
- **Timeline:** All agents aligned; ready for release decision

#### Agents Aligned

✅ Ripley (Lead): BREAKING CHANGES FOUND - cannot approve as non-breaking  
✅ Parker (Core Dev): BREAKING CHANGE DETECTED in auth settings surface  
✅ Dallas (Tooling Dev): CONFIRMED 2 breaking changes; bug fix is non-breaking  
✅ Lambert (Tester): BREAKING CHANGE CONFIRMED with concrete deserialization evidence  

---

## 2026-04-16

- Squad initialized for Refitter.
- Team root uses the worktree-local strategy at `C:\projects\christianhelle\refitter`.
- Shared append-only Squad files use Git's `union` merge driver.
- **Issue #998 Investigation Complete:** Verdict is a real product bug, not user error. CLI ignores `outputFolder` when it equals the default `./Generated`, causing MSBuild to search for files in wrong location. First clean build fails due to sync mismatch between MSBuild prediction and CLI output. Fix: remove default-value check in `GenerateCommand.cs:648`.
