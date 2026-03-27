---
name: "msbuild-finding-verification"
description: "Verify MSBuild and workflow review findings against the current Refitter code before changing anything"
domain: "devops-msbuild"
confidence: "high"
source: "manual"
---

## Pattern

When a review finding targets Refitter's MSBuild surface or GitHub Actions workflow:

1. **Verify the exact current code first.** Check whether the referenced lines, predicate, or flag still exists. If the file length or structure has changed, treat the finding as potentially stale.
2. **Prefer the smallest configuration alignment.** If two parallel workflow steps differ only by one build argument, fix only that argument first instead of refactoring the whole workflow.
3. **Check adjacent behavior, but do not invent new plumbing.** Review nearby `dotnet clean`, include-pattern scoping, and `-filelogger` usage to confirm consistency, but do not add new properties or log-file names unless the current code actually needs them.
4. **Validate with the repo's standard gates.** Run Refitter's build, test, and format verification commands after the edit, and compare with existing harness scripts such as `test\MSBuild\build.ps1` when judging intended behavior.

## Anti-Patterns

- Editing task code to satisfy a finding that no longer exists in the current file.
- Expanding a one-flag workflow fix into broader cleanup without evidence of a real defect.
- Adding `RefitterIncludePatterns` or custom log-file routing solely because an old review comment mentioned them.
