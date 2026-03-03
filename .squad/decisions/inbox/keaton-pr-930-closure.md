# Architectural Decision: Close PR #930

**Date:** 2026-03-03  
**Decision By:** Keaton (Lead Architect)  
**Requested By:** Christian Helle  
**Status:** Decided

---

## Decision

**Close PR #930** (Add .NET 8 source generator compatibility validation) and do not merge.

---

## Rationale

### Primary Reasons

1. **.NET 8.0 End of Support Timeline**
   - .NET 8.0 reaches end of support by end of 2026
   - .NET 10 is the current LTS version and should be the focus
   - Investing in .NET 8 specific validation has diminishing returns

2. **Tests Belong in Feature/Fix Branches**
   - PR #930 contains tests that reference specific issues: #635, #580, #672
   - These tests should logically reside in their respective feature/fix branches, not a documentation/validation branch
   - Consolidating tests in proper branches improves traceability and maintenance

3. **Project Priority Alignment**
   - Current focus should be on .NET 10 LTS and future releases
   - Resources better spent on feature development and bug fixes

---

## Test Migration Plan

### Tests Identified in PR #930

Based on Hockney's validation report embedded in the PR:

#### 1. **Issue #580: Nullable String Handling** → Branch: `fix/nullable-string-handling`

**Tests to Migrate (from PR #930):**
- `Test_Nullable_StringProperty_GeneratedWithQuestionMark`
- `Test_Nullable_DoubleProperty_GeneratedWithQuestionMark`
- `Test_Nullable_StringNotInRequired_Field`
- `Test_Nullable_StringInRequired_Field_Still_Nullable`
- `Test_NullableReferenceTypes_Directive_Present`
- `Can_Build_Generated_Code_With_Nullable_Strings`
- `Test_Multiple_Nullable_Properties_In_Same_Model`

**Status:** These tests are already present in `fix/nullable-string-handling` branch. PR #930 appears to duplicate them.

**Action:** No migration needed. Branch already has these tests in `src/Refitter.Tests/Examples/NullableStringPropertyTests.cs`.

---

#### 2. **Issue #672: Interface Method Naming Scope** → Branch: `fix/interface-method-naming-scope`

**Tests to Migrate (from PR #930):**
- `Can_Generate_Code`
- `Generates_Separate_Interfaces_For_Each_Tag`
- `Users_Interface_Should_Not_Have_Numbered_Method_Names`
- `Products_Interface_Should_Not_Have_Numbered_Method_Names`
- `Orders_Interface_Should_Not_Have_Numbered_Method_Names`
- `Method_Names_Should_Be_Identical_Across_Different_Interfaces`
- `Test_MultipleInterfacesByTag_DuplicateOperationIds_NoGlobalCounter`
- `Test_MultipleInterfacesByTag_NoConflict_WithinInterface`
- `Test_MultipleInterfacesByTag_EachInterface_HasOwnNamespace`
- `Can_Build_Generated_Code`

**Status:** These tests are already present in `fix/interface-method-naming-scope` branch. PR #930 appears to duplicate them.

**Action:** No migration needed. Branch already has these tests in `src/Refitter.Tests/Examples/MultipleInterfacesByTagMethodNamingTests.cs`.

---

#### 3. **Issue #635: Source Generator File I/O** → Branch: `fix/source-generator-addource`

**Tests to Migrate (from PR #930):**
- `Test_SourceGenerator_DoesNotWriteToFileSystem`
- `Test_SourceGenerator_WithApiDescriptionServer_NoFileConflicts`
- `Test_SourceGenerator_GeneratedCodeIsValid`
- `Test_SourceGenerator_OutputFilename_NotCreatedOnDisk`

**Location in PR #930:** `src/Refitter.Tests/SourceGeneratorFileIOTests.cs`

**Status:** These tests are NEW and specific to source generator file I/O behavior.

**Action:** **MIGRATION REQUIRED**
- Move `SourceGeneratorFileIOTests.cs` from PR #930 to `fix/source-generator-addource` branch
- This is the only actual new content in PR #930 that needs preservation
- Tests validate that source generators use `context.AddSource()` instead of file system I/O

---

## Cleanup Identified

1. **Duplicate Test Code**
   - PR #930 duplicates tests already present in `fix/nullable-string-handling` and `fix/interface-method-naming-scope`
   - No cleanup needed as PR will be closed without merge

2. **Consolidation Opportunity**
   - All source generator specific tests should be in dedicated test files
   - `SourceGeneratorFileIOTests.cs` is properly structured and should be migrated to the appropriate branch

---

## Action Items

1. ✅ **Close PR #930** with explanation comment
2. ⚠️ **Migrate `SourceGeneratorFileIOTests.cs`** to branch `fix/source-generator-addource`
   - File path: `src/Refitter.Tests/SourceGeneratorFileIOTests.cs`
   - This is the only unique contribution from PR #930
3. ✅ **Document decision** in `.squad/decisions.md` (via Scribe)

---

## Communication

The PR closure comment will:
- Explain the .NET 8.0 EOL rationale
- Note that .NET 10 is the current LTS focus
- Clarify that tests belong in their logical feature/fix branches
- Maintain professional tone aligned with project priorities

---

## Impact Assessment

- **Risk:** Low - Tests are already present in feature branches or will be migrated
- **Effort Saved:** Avoiding unnecessary .NET 8 validation work
- **Focus Gained:** Resources redirected to .NET 10 and feature development
- **Technical Debt:** None introduced, actually reduces duplication

---

## Related References

- Issue #580: Nullable Strings not being marked correctly
- Issue #635: Build errors when combined with `Microsoft.Extensions.ApiDescription.Server`
- Issue #672: Multiple interfaces "ByTag" increments method names globally
- Branch: `fix/nullable-string-handling` (ff0b3487f7b6be100c4c71a121bd5843e8a1dd77)
- Branch: `fix/interface-method-naming-scope` (3cc5f2b35d4a1c3fe58300c49b815c77e90b7797)
- Branch: `fix/source-generator-addource` (fb834ae312e6fc3bbd89a324e944b965b2c5307c)

---

**Keaton, Lead Architect**
