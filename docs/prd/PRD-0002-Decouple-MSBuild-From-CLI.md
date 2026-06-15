# PRD-0002: Decouple MSBuild Task from CLI Binary Dependency

## Problem

The MSBuild task (`RefitterGenerateTask`) has a **hard dependency on the CLI binary**. It spawns `dotnet refitter.dll` as a child process and parses stdout for `GeneratedFile: ` markers.

### Friction points

- **Testability**: MSBuild tests cannot run without the CLI being built and available on disk. The test harness must build the CLI first, then run the MSBuild task.
- **Interface leak**: The MSBuild task's interface is nearly as complex as its implementation. Testers must know about stdout parsing, process lifecycle, and CLI argument formatting.
- **Fragility**: If the CLI's output format changes (e.g., a new log line appears), the MSBuild task's stdout parsing breaks silently. There is no isolation between the two.
- **Bug coupling**: The MSBuild task inherits all CLI bugs and behavior changes. A CLI bug fix that changes output format requires a separate MSBuild test fix.

### Deletion test

If you delete the CLI dependency from the MSBuild task, complexity does **not** vanish — it concentrates in a new adapter. This signals a **real seam** worth deepening.

## Current Architecture

```
MSBuild Task (RefitterGenerateTask)
  ├── Spawns: dotnet refitter.dll --settings-file "..." --simple-output
  ├── Parses stdout: "GeneratedFile: C:\path\to\file.cs"
  ├── Returns: ITaskItem[] GeneratedFiles
  └── Depends on: CLI binary location resolution (RuntimeResolver, PreferredRuntimeOrder)
```

**Interface complexity**: The MSBuild task's public surface (properties, methods, MSBuild integration) is nearly as complex as its implementation. It manages process lifecycle, runtime resolution, stdout parsing, and error handling — all through one module.

**One adapter = hypothetical seam**: The current design has only one adapter (the CLI process). But the MSBuild task and CLI share the same generation logic — this is a real seam that should have two adapters: a CLI adapter and a direct Core adapter.

## Proposed Solution

Define a port at the seam between MSBuild and Core. Both the CLI and the MSBuild task implement this port.

### New module: `IGeneratorRunner` (port)

```csharp
public interface IGeneratorRunner
{
    Task<IReadOnlyList<GeneratedFile>> RunAsync(
        RefitGeneratorSettings settings,
        bool skipValidation,
        bool noLogging,
        CancellationToken cancellationToken);
}
```

**Interface**: One method, returns a list of generated file paths. No knowledge of stdout, process, or CLI args.

**Error modes**: Throws on generation failure. MSBuild task converts to MSBuild errors.

**Ordering**: Sequential — one `.refitter` file at a time.

**Config**: Settings passed as parameters.

### Adapter 1: `CliGeneratorRunner` (production — CLI process)

- Wraps the existing CLI invocation logic (process spawning, stdout parsing)
- Implements `IGeneratorRunner`
- Used by MSBuild task when CLI is preferred (existing behavior)

### Adapter 2: `CoreGeneratorRunner` (production — direct Core call)

- Calls `RefitGenerator.CreateAsync()` → `Generate()` / `GenerateMultipleFiles()` directly
- Implements `IGeneratorRunner`
- Used by MSBuild task as the new default (avoids process overhead)

### Adapter 3: `TestGeneratorRunner` (test)

- In-memory implementation for MSBuild task tests
- Returns mock `GeneratedFile` list
- No CLI binary needed

### MSBuild Task changes

```csharp
public class RefitterGenerateTask : MSBuildTask
{
    private readonly IGeneratorRunner _generatorRunner;

    public RefitterGenerateTask(IGeneratorRunner generatorRunner)
        => _generatorRunner = generatorRunner;

    // Execute() calls _generatorRunner.RunAsync() instead of spawning a process
}
```

## Files changed

| File | Action | Notes |
|------|--------|-------|
| `src/Refitter.Core/IGeneratorRunner.cs` | New | Port definition |
| `src/Refitter.Core/CliGeneratorRunner.cs` | New | CLI adapter |
| `src/Refitter.Core/CoreGeneratorRunner.cs` | New | Direct Core adapter |
| `src/Refitter.MSBuild/RefitterGenerateTask.cs` | Modify | Use `IGeneratorRunner` instead of process spawning |
| `src/Refitter.Tests/MSBuildTaskTests.cs` | Modify | Use `TestGeneratorRunner` for unit tests |
| `test/MSBuild/` (integration test) | Modify | Test both adapters |

## Benefits

### Locality

- MSBuild logic (file scanning, MSBuild integration) concentrates in `RefitterGenerateTask`
- Generation logic concentrates in `IGeneratorRunner` adapters
- Each adapter is tested independently — no cross-contamination

### Leverage

- One port pays back across: MSBuild task (two adapters), CLI tool (one adapter), and tests (one adapter)
- Callers (MSBuild) learn one interface regardless of which adapter is used

### Test improvements

- **Before**: MSBuild tests require CLI binary to be built. Tests are integration-level only.
- **After**: MSBuild unit tests use `TestGeneratorRunner` — no CLI needed. Generation tests use `CoreGeneratorRunner` — no process overhead.
- **New test surface**: Each adapter is independently testable. Test CLI argument formatting, process timeout handling, and stdout parsing in `CliGeneratorRunner` tests. Test direct Core calls in `CoreGeneratorRunner` tests.

## Trade-offs

- **Added complexity**: One new interface and two new adapters for a module that works today.
- **Justification**: The seam is real (two production adapters: CLI process + direct Core call). The MSBuild task's current process-spawning approach adds ~300 lines of infrastructure code that obscures the actual generation logic.
- **Risk**: Low. The current CLI adapter preserves existing behavior. The Core adapter can be added incrementally.

## Testing strategy

1. **Unit tests**: `TestGeneratorRunner` for MSBuild task logic (file scanning, MSBuild integration)
2. **Adapter tests**: `CliGeneratorRunner` tests verify CLI argument formatting and stdout parsing
3. **Adapter tests**: `CoreGeneratorRunner` tests verify direct Core calls produce correct output
4. **Integration tests**: End-to-end MSBuild task with both adapters

## Notes

- The MSBuild task's existing `ProcessRunner` and `RuntimeResolver` abstractions become the `CliGeneratorRunner` adapter's internal implementation details.
- The `CoreGeneratorRunner` adapter is simpler: just calls `RefitGenerator.CreateAsync()` and maps `GeneratorOutput` to `GeneratedFile`.
- This deepening makes the MSBuild task **more** testable and **less** coupled to the CLI, which aligns with the overall goal of deepening the `RefitGenerator` module (PRD-0001).
