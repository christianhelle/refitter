### 2026-03-03T12:58:27Z: Build & Test Quality Gate Directive

**By:** Christian Helle (via Copilot)

**What:** All PRs must have passing builds and passing unit tests before creation. Implement this validation automatically in all future work — no exceptions.

**Why:** Quality gate enforcement. PRs should never be opened with known build failures. This prevents review friction and maintains code quality standards going forward.

**Action:** 
- Fix all currently failing PRs immediately
- Add pre-PR validation step to team workflow
- All agents must verify builds + tests pass before opening any PR
