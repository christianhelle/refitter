# Lambert History

## Current Session: Issue #1094 - RefitterAutoScan (2026-05-29)

### Role

- Validation: Reviewed implementation details and corrected documentation against code
- Status: Approved corrected documentation after Dallas's revision cycle

### Learning

- **2026-05-29T12:44:43.282+02:00 issue #1094 doc re-review:** The corrected MSBuild docs now match src\Refitter.MSBuild\Refitter.MSBuild.targets: RefitterGenerate stays as the explicit on-demand target, while _RefitterGenerateOnBuild is the BeforeTargets="CoreCompile" wrapper gated by RefitterAutoScan != false.

- **2026-05-29T12:44:43.282+02:00 issue #1094 validation:** src\Refitter.MSBuild\Refitter.MSBuild.targets now defaults RefitterAutoScan to 	rue, keeps RefitterGenerate directly callable, and gates normal-build execution through _RefitterGenerateOnBuild with BeforeTargets="CoreCompile" plus Condition="'$(RefitterAutoScan)' != 'false'".

### Evidence

- Implementation in src\Refitter.MSBuild\Refitter.MSBuild.targets matches documented behavior
- Documentation in README.md and docfx article now accurately describes the feature
- No documentation-to-implementation mismatch remains

---

## Archive

For historical context on issues #1083, #1057, #1045, #1034, #1039, and earlier audit work, see history-archive.md.
