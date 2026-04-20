# Plan: GitHub Discussions Post for Breaking Changes (1.7.3 → HEAD)

## Status
**INPUT STAGE**: All research complete. Team decisions finalized in `.squad/decisions.md` (2026-04-17).

## Executive Summary

Since version 1.7.3 (2026-01-24), Refitter has introduced **2 breaking changes** and numerous backward-compatible enhancements. A GitHub Discussions post is needed to help users understand the impact and migration steps.

### What the Project Already Knows

**From `.squad/decisions.md` (unanimous team consensus, 4 agents):**

#### Breaking Changes (2)

1. **`.refitter` Config Key Renamed: `generateAuthenticationHeader` → `authenticationHeaderStyle`** (MEDIUM RISK)
   - Old setting: boolean `"generateAuthenticationHeader": true`
   - New setting: enum `"authenticationHeaderStyle": "Method"` or `"Parameter"` or `"None"`
   - Impact: Old JSON keys silently ignored; users lose auth header behavior
   - Affected: Users with custom `.refitter` files using `generateAuthenticationHeader: true`
   - Fixes: PR #897, #936

2. **Source Generator No Longer Writes Disk Files** (HIGH RISK)
   - Changed from `File.WriteAllText()` to `context.AddSource()` (Roslyn best practice)
   - Impact: Physical `.g.cs` files no longer appear in `./Generated` folder
   - Users must view generated code via IDE (F12 / "Go to Definition") instead
   - Alternatively, switch back to CLI or MSBuild tasks for disk files
   - Fixes: Issues #635, #520, #310 (file locking, process access errors)
   - Evidence: PR #923

#### Non-Breaking (for reference)
- **MSBuild output path fix (Issue #998):** Corrects buggy behavior; users on old .refitter can explicitly set `"outputFolder": "."` to opt into old path
- **8 Additive Features:** All backward compatible (safe defaults)
- **4 Bug Fixes:** Only affect previously broken inputs

#### Target Version
- **Current HEAD:** 1.8.0-preview.103 (approaching 1.8.0 stable or 2.0.0)
- **Recommendation:** Major bump to **2.0.0** required (consensus from all 4 agents)

---

## What Still Needs Verification

The team decisions are sound, but before posting publicly, confirm:

### 1. **User Impact Assessment** (Validation)
   - [ ] **How many users likely affected?**
     - Estimate: Small (auth setting) to Medium (source generator; estimated 30-40% of source gen users)
     - Action: Check GitHub Issues search for affected bug reports or feature requests since 1.7.3 released (2026-01-24)
   
### 2. **Migration Path Clarity** (Validation)
   - [ ] **For `.refitter` config change:**
     - Provide search-and-replace recipe (e.g., find `"generateAuthenticationHeader": true`, replace with `"authenticationHeaderStyle": "Method"`)
     - Test on sample `.refitter` files to confirm parsing works
   - [ ] **For source generator disk files:**
     - Document where to find generated code in IDE (IDE version-specific navigation)
     - Provide fallback: "Switch to CLI or MSBuild if you need disk files"

### 3. **Release Timing & Version Clarity** (Ambiguity)
   - [ ] **Is 2.0.0 already released, or is this for an upcoming 2.0.0?**
     - Current state: HEAD = 1.8.0-preview.103, no stable 2.0.0 yet
     - Decision needed: When should the Discussion be posted?
       - Option A: Post before 2.0.0 stable release (as heads-up to pre-release users)
       - Option B: Post alongside 2.0.0 stable release (standard release notes)
   - [ ] **Confirm version number** for the Discussion title

### 4. **Documentation Companion** (Verification)
   - [ ] **Is a Migration Guide needed in docs/?**
     - Current state: CHANGELOG.md covers issues; no dedicated migration guide yet
     - Recommendation: Create lightweight `docs/MIGRATION_1.8_to_2.0.md` (or similar) with concrete examples

### 5. **Refit v10 Upgrade Impact** (Cross-check)
   - [ ] PR #893 updated Refit from v6/v7/v8/v9 to **v10 (major)**
   - [ ] Is this reflected in the Discussion or separate from the breaking changes post?
     - Likely separate (dependency update), but verify if Refit v10 introduces user-facing breaks

---

## Execution Plan (4 Phases)

### **Phase 1: Verification & Clarity** (Lead: Ripley)
1. Clarify release timing (2.0.0 stable vs. pre-release) with Christian Helle
2. Check GitHub Issues for user impact signals since 1.7.3
3. Confirm MSBuild v10 support and Refit v10 scope

**Outcome:** Release context document

### **Phase 2: Content Generation** (Lead: Ripley + Parker)
1. Draft Discussion post content:
   - Title: "Breaking Changes in 1.8.0 / 2.0.0"
   - Sections:
     - Executive Summary (2 breaking changes, 8 additive features)
     - Breaking Change #1: Auth Config Key Rename (why, how to migrate)
     - Breaking Change #2: Source Generator Disk Files (why, where to find code)
     - Non-Breaking: Bug fixes and additive features (for context)
     - FAQ: "Will this break my CI/CD?", "How do I view generated code?", "Can I revert?"
2. Test migration recipes on real `.refitter` files
3. Gather sample screenshots/GIF for IDE navigation (if helpful)

**Outcome:** Draft Discussion post (Markdown ready for GitHub)

### **Phase 3: Review & Approval** (Lead: Ripley + Christian)
1. Christian Helle reviews draft
2. Incorporate feedback
3. Finalize tone and messaging

**Outcome:** Approved Discussion post (ready to publish)

### **Phase 4: Publishing** (Lead: Christian Helle)
1. Post to GitHub Discussions (category: Announcements)
2. Pin the Discussion
3. Reference in stable release notes

**Outcome:** Live Discussion post

---

## Ambiguities Requiring User (Christian Helle) Confirmation

### A. Release Timing
**Question:** When should the Discussion be posted?
- **Option 1:** Now, ahead of 2.0.0 stable (warns pre-release users)
- **Option 2:** When 2.0.0 stable ships (standard release notes)
- **Option 3:** Only if a 1.8.0 stable is released first (and then again for 2.0.0)

**Impact:** Affects tone ("What's Coming" vs. "What Changed")

### B. Target Audience Scope
**Question:** Should the Discussion post also cover Refit v10 migration, or keep focus narrow?
- **Option 1:** Narrow (only Refitter breaking changes)
- **Option 2:** Broad (Refitter + Refit v10 + dependency changes)

**Impact:** Affects post length and complexity

### C. Migration Guide Companion Artifact
**Question:** Should a formal Migration Guide be created in `docs/`, or is inline help in the Discussion enough?
- **Option 1:** Lightweight Discussion-only (keep simple)
- **Option 2:** Create `docs/MIGRATION_1.8_to_2.0.md` with examples (more discoverable, searchable)

**Impact:** Affects discoverability for future users

### D. Automation for `.refitter` Migration
**Question:** Should a tool be created to auto-migrate `.refitter` files, or is the search-and-replace recipe sufficient?
- **Option 1:** Search-and-replace only (simple, documented)
- **Option 2:** Create migration script or tool (higher effort, better UX)

**Impact:** Affects time-to-resolution for end users

---

## Reference: Team Decision Context

All findings based on systematic audit by 4-agent team (Ripley, Parker, Dallas, Lambert) on 2026-04-17:

- **Evidence Source:** CHANGELOG.md (lines 1–172), git commits, PRs #897, #923, #938, #936, #907, #904
- **Consensus:** 2 breaking changes confirmed; major version bump required
- **Sign-off:** All 4 agents agreed independently before Ripley synthesized findings

---

## Next Step

**Await Christian Helle's responses to the 4 ambiguities above, then enter Phase 1 execution.**
