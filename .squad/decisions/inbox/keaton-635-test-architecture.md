# Decision: #635 Test Architecture Approach

**Issue:** Fix #635 uses context.AddSource() (correct pattern) but tests expect disk files.

**Decision:** Option B — Redesign tests for proper source generator pattern

**Why:** Source generators should NOT write to disk. The current file writing is a design 
flaw that violates source generator best practices. Tests should verify compilation output, 
not disk artifacts.

**Implementation:** Update Refitter.SourceGenerator.Tests to:
1. Remove assertions checking for .g.cs files on disk
2. Add assertions verifying generated code appears in compilation
3. Keep integration tests that verify the generated code is compilable
4. Document that generated files are not written to disk (view via IDE "Show Generated Files")

**Action:** Mark fix-635 as needing "Update test infrastructure" task

**Rationale:**
- context.AddSource() is the proper API for source generators
- Generated files should only exist in the compilation context
- Tests should validate behavior through compilation, not filesystem state
- This aligns with Microsoft's source generator best practices
- Users can view generated files through IDE tooling, not disk artifacts
