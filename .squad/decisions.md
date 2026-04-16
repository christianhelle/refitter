# Squad Decisions

## 2026-04-16

- Squad initialized for Refitter.
- Team root uses the worktree-local strategy at `C:\projects\christianhelle\refitter`.
- Shared append-only Squad files use Git's `union` merge driver.
- **Issue #998 Investigation Complete:** Verdict is a real product bug, not user error. CLI ignores `outputFolder` when it equals the default `./Generated`, causing MSBuild to search for files in wrong location. First clean build fails due to sync mismatch between MSBuild prediction and CLI output. Fix: remove default-value check in `GenerateCommand.cs:648`.
