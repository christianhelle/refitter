---
name: "generated-file-manifest"
description: "Use CLI-emitted generated-file markers instead of duplicating output-path prediction logic"
domain: "tooling"
confidence: "high"
source: "earned"
---

## Context
Use this when one tooling surface (MSBuild task, IDE host, wrapper script) needs to know which files another tool actually wrote. It is especially useful when filenames depend on runtime generation choices and can drift from any duplicated prediction logic.

## Patterns
- Have the authoritative generator emit a stable, machine-readable marker for every successful write, e.g. `GeneratedFile: C:\path\to\File.cs`.
- Emit the marker only after the file write succeeds so downstream tooling can trust the path.
- Make the consuming host parse those markers and treat them as the source of truth for compile items, manifests, or follow-up processing.
- If the generator exits successfully but reports no generated files, fail loudly instead of silently continuing with an empty item list.

## Examples
- `src\Refitter\GenerateCommand.cs` writes `GeneratedFile:` markers in `--simple-output` mode after each file is saved.
- `src\Refitter.MSBuild\RefitterGenerateTask.cs` parses those markers and uses them for `GeneratedFiles` instead of parsing `.refitter` settings.

## Anti-Patterns
- Re-parsing config files to guess outputs when the generator already knows the final filenames.
- Substring-based or heuristic path prediction that can silently drift as generator behavior evolves.
- Treating a successful process exit with zero reported files as success.
