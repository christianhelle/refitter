# Squad Decisions

## 2026-04-20

### Core Findings Post-Audit: Tri-State Nullable Handling

**Decided By:** Parker (Core Developer)  
**Status:** IMPLEMENTED

Treat `CodeGeneratorSettings.GenerateOptionalPropertiesAsNullable` as a tri-state at the Refitter layer:
- Auto-enable for NRT only when the caller did **not** explicitly assign the setting
- Explicit `false` must win over the convenience default
- Prevents user-intended nullable behavior from being overridden by heuristics

**Rationale:** NRT is a strong signal of intent; explicit `false` indicates deliberate opt-out.

---

### Core Findings Post-Audit: Contract Type Rename Safety

**Decided By:** Parker (Core Developer)  
**Status:** IMPLEMENTED

Any post-generation contract-type rename pass must:
1. Build rename map only after collision checks
2. Restrict Roslyn `SimpleName` rewrites to type-reference contexts only
3. Exclude blanket simple-name rewriting to prevent regressions in:
   - `nameof(...)` expressions
   - Method calls
   - Other expression identifiers

**Rationale:** Regex-on-raw-source is fundamentally unsafe; word boundaries are insufficient. Type-reference-only scope prevents false positives.

---

### Core Findings Post-Audit: ParameterExtractor Regression Watch Areas

**Decided By:** Parker (Core Developer)  
**Status:** DOCUMENTED FOR FOLLOW-UP

For `ParameterExtractor` dynamic-query watch areas, prefer end-to-end regression coverage around:
- Emitted signatures
- XML documentation
- Query-wrapper DTOs

**Note:** No concrete behavior regression was reproduced beyond the already-fixed dedupe issues. Monitor for future regressions as code evolves.

---

### Tooling Findings Post-Audit: Spec Path Resolution & CLI Override Precedence

**Decided By:** Dallas (Tooling Developer)  
**Status:** IMPLEMENTED

When Refitter is driven from a `.refitter` file:
1. Every tooling surface resolves OpenAPI spec paths relative to the `.refitter` file itself **before** validation or generation
2. Explicit CLI `--output` must override settings-file output folders for multi-file generation (including `Contracts.cs`)
3. Shared CLI helper centralizes path resolution logic

**Rationale:** Relative path handling had diverged between CLI validation, CLI generation, and source generator flows, causing "works in one surface, breaks in another" regressions.

**Implementation:**
- Shared helper: `src\Refitter\SettingsFilePathResolver.cs`
- CLI writer: `src\Refitter\GenerateCommand.cs`
- CLI validator: `src\Refitter\SettingsValidator.cs`
- Source generator: `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs`
- MSBuild: `src\Refitter.MSBuild\RefitterGenerateTask.cs`

---

### Tooling Findings Post-Audit: Prediction-Free Assumption Design

**Decided By:** Dallas (Tooling Developer)  
**Status:** IMPLEMENTED

Tooling layers should avoid prediction-by-assumption:

1. **Source-generator incremental payloads**: Use value semantics so Roslyn incremental caching can treat unchanged `.refitter` inputs as unchanged work
2. **Source-generator warnings**: Emit user-visible warning when zero `.refitter` AdditionalFiles present
3. **MSBuild discovery**: Discover generated outputs by configured output locations + post-run file changes, not regex-parsed settings into hardcoded filenames
4. **Include patterns**: Use exact or wildcard matches, never substring containment

**Rationale:** Roslyn's incremental system relies on stable reference semantics; assumption-based path prediction causes sync mismatches.

---

## 2026-04-18

### P0 Audit Findings - Critical Generator Bugs

**Verified By:** Parker (Core Developer)  
**Status:** ALL VALID

- **#1011**: Source generator crashes IDE/build on duplicate filenames
- **#1012**: CI/CD silently ships stale/missing code on CLI failures
- **#1013**: Regex corrupts generated code, breaks member names
- **#1014**: Breaks Newtonsoft users, silently regresses internal enums (PARTIAL)
- **#1015**: NRE on every Swagger 2.0 document
- **#1016**: Multi-spec merge drops all schemas from split APIs

**Key Architectural Concerns:**
1. Regex-on-raw-source fundamentally unsafe (word boundaries insufficient)
2. Missing null checks in OpenAPI document traversal (Swagger 2.0 vs 3.0)
3. MSBuild task doesn't follow MSBuild contract (returns true regardless of exit code)

**Recommendation:** Fix all P0 before v2.0 release.

---

### P1 Audit Findings - High-Priority Issues

**Verified By:** Lambert (Tester)  
**Status:** 10 VALID, 1 PARTIAL

- **#1017**: AOT context non-compiling (generics, nested types, namespaces)
- **#1018**: ParameterExtractor invalid identifiers (not using IdentifierUtils)
- **#1019**: Security scheme header unsafe (leading digits, keywords)
- **#1020**: Dynamic-querystring self-assign (`_foo = _foo;`)
- **#1021**: CLI --output no longer overrides when settings file used
- **#1022**: MSBuild predicted paths diverge from actual generation
- **#1023**: MSBuild IncludePatterns uses substring matching (fragile)
- **#1024**: Refit 10 leaks to consumers (design decision, PARTIAL)
- **#1025**: OpenApi.Readers 1.x → 3.x silent change
- **#1026**: Auto-enable GenerateOptionalPropertiesAsNullable
- **#1027**: RefitInterfaceGenerator NRE on no content

**Critical:** #1018, #1019, #1020 produce non-compiling code; #1027 crashes on 204 responses.

---

### P2 Medium Audit Findings

**Verified By:** Dallas (Tooling Developer)  
**Status:** 14 VALID, 2 PARTIAL

**Critical Issues (Crashes/Corruption):**
- **#1028**: Source Generator Incremental Caching Defeated (List vs EquatableArray)
- **#1037**: Crash on Empty Namespace List
- **#1039**: Mutation of Shared NSwag Model

**Security/Correctness:**
- **#1035**: XML Doc Injection Vulnerability (unescaped parameter descriptions)
- **#1034**: Silent Data Loss in Multi-Spec Merge

**Type System Issues:**
- **#1036**: Nullable Parameter Mis-classification
- **#1038**: Reference Type Nullability (CS8632 errors)

**Tooling Issues:**
- **#1029**: Source Generator Silent Warnings (Debug.WriteLine no-op)
- **#1041**: MSBuild Task Multiple Failure Modes
- **#1043**: Breaking CLI Change (bool flag → enum)

**Partial Issues:**
- **#1032**: JsonConverter Semantics (runtime verification needed)
- **#1042**: Spectre.Console.Cli version bump (smoke testing needed)

---

### P2 Low Audit Findings

**Verified By:** Ripley (Lead)  
**Status:** 13 VALID, 0 PARTIAL

All issues appropriately classified. Systemic patterns identified:

1. **Settings Validation Gaps** (#1044, #1045, #1046)
2. **Parsing Fragility** (#1047, #1050, #1051)
3. **Double-Read/Double-Process** (#1048, #1052)
4. **Keyword Handling Gaps** (#1053)
5. **Library Async Best Practices** (#1049)
6. **Fragile Ordering Dependencies** (#1055, #1056)

Recommendation: Address incrementally in 2.1.x patches.

---

### Breaking Changes Guidance Plan

**Decided By:** Bishop (Docs Specialist)  
**Status:** APPROVED FOR PUBLICATION

**Deliverables Created:**
1. GitHub Discussion draft (ready to publish)
2. Migration guide in docs/ (breaking-changes-v2-0-0.md)
3. Documentation index updated (toc.yml)

**Publication Strategy:**
- Create Discussion under Announcements category
- Pin for 2-3 weeks during v2.0.0 adoption
- Link from CHANGELOG and README

**Reviewed By:** Ripley (Lead) - ✅ APPROVED

---

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
