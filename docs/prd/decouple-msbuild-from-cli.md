# Decouple CLI Binary Dependency from MSBuild Task

## Status

**Draft** — Ready for implementation

## Context

`Refitter.MSBuild` (`netstandard2.0`) currently has **zero compile-time dependency** on `Refitter.Core` or `Refitter`. Instead, it bundles the entire CLI output (`refitter.dll` + all transitive dependencies — Spectre.Console, Exceptionless, ApplicationInsights, Fluid, etc.) for `net8.0`, `net9.0`, and `net10.0` into the NuGet package.

At build time, the task discovers `.refitter` files and spawns `dotnet <refitter.dll> --settings-file <file> --simple-output` as an **out-of-process** subprocess. It parses stdout for `GeneratedFile: <path>` markers and returns them to MSBuild as `@(Compile)` items.

The CLI (`Refitter`) handles everything else: settings deserialization, applying defaults, resolving relative spec paths, OpenAPI validation, generation, output path planning, file writing, and telemetry.

## Problem

1. **Build dependency order:** The MSBuild project must be built after the CLI project, and its package depends on CLI binaries being present in the correct output folders.
2. **Package bloat:** The MSBuild package contains hundreds of files (CLI + deps for 3 TFMs) that are never directly used by the task itself.
3. **Complexity:** The task manages process spawning, runtime detection, timeout handling, and stdout parsing — all just to invoke a CLI tool that could be called directly as a library.

## Decision

**Approach:** `ProjectReference` to `Refitter.Core` with `SuppressDependenciesWhenPacking`.

- The MSBuild task will call `Refitter.Core` APIs directly (in-process).
- No subprocess spawning, no runtime detection, no stdout parsing.
- The NuGet package will have zero `<dependencies>` entries.
- `Refitter.Core` and its transitive dependencies will be bundled into `tasks/netstandard2.0/` in the package.

## Trade-offs

| Concern | Mitigation |
|---|---|
| **Assembly loading conflicts in MSBuild process** | `Refitter.Core` depends on `Microsoft.CodeAnalysis.CSharp` (Roslyn) and `NSwag`. The MSBuild process may already load different versions. The same risk is already handled successfully by `Refitter.SourceGenerator` in the Roslyn compiler process. If conflicts arise, binding redirects can resolve them. |
| **Package size** | The MSBuild package will contain `Refitter.Core.dll` + `NSwag` + `OasReader` + `Microsoft.CodeAnalysis.CSharp` + etc. This is comparable to the current package size (which already bundles the CLI + all its deps for 3 TFMs). |
| **Behavioral changes** | Zero. The orchestration logic is moved to Core, not rewritten. The task performs the same operations as the CLI did. |

## Implementation Plan

### Phase 1: Move shared orchestration logic to `Refitter.Core`

The MSBuild task needs to perform the same operations as the CLI (settings loading, output path planning, validation, file writing). These must be shared to avoid duplication and behavioral drift.

| File | From | To | Action |
|---|---|---|---|
| `OutputPlanner.cs` | `src/Refitter/` | `src/Refitter.Core/` | Move to `Refitter.Core` namespace. Change `Settings` parameter to `string? settingsFilePath` + `string? cliOutputPath` (or keep dual overloads for CLI compatibility). |
| `Validation/OpenApiValidator.cs` | `src/Refitter/Validation/` | `src/Refitter.Core/Validation/` | Move to `Refitter.Core` namespace. |
| `Validation/OpenApiValidationResult.cs` | `src/Refitter/Validation/` | `src/Refitter.Core/Validation/` | Move to `Refitter.Core` namespace. |
| `Validation/OpenApiStats.cs` | `src/Refitter/Validation/` | `src/Refitter.Core/Validation/` | Move to `Refitter.Core` namespace. |
| `Validation/OpenApiValidationException.cs` | `src/Refitter/Validation/` | `src/Refitter.Core/Validation/` | Move to `Refitter.Core` namespace. |
| `ApplySettingsFileDefaults` (in `GenerateCommand.cs`) | `src/Refitter/GenerateCommand.cs` | `src/Refitter.Core/RefitterSettingsLoader.cs` | Extract to `RefitterSettingsLoader.ApplyDefaults(string settingsFilePath, RefitGeneratorSettings settings)` or a new `RefitGeneratorSettingsDefaults` class. |

**Files to update in `src/Refitter/`:**
- `GenerateCommand.cs` — remove `ApplySettingsFileDefaults`, use Core's version. Keep `FormatGeneratedFileMarker` (CLI still needs it for `--simple-output`).
- `GenerationOrchestrator.cs` — update `using` statements for moved types.
- `IGenerationReporter.cs`, `RichGenerationReporter.cs`, `SimpleGenerationReporter.cs` — update `using` for `OpenApiValidationResult`.

**Files to update in `src/Refitter.Tests/`:**
- `OutputPlannerTests.cs` — update `using` if namespace changes.
- `OpenApi/*Tests.cs` — update `using` for moved validation types.
- `GenerationOrchestratorTests.cs` — update `using` for moved validation types.
- `GenerateCommandTests.cs` — remove `FormatGeneratedFileMarker_Should_Emit_A_Task_Parseable_Full_Path` test (MSBuild no longer parses CLI markers).

### Phase 2: Rewrite `Refitter.MSBuild` task

**Project file changes (`src/Refitter.MSBuild/Refitter.MSBuild.csproj`):**

```xml
<!-- Add -->
<ProjectReference Include="..\Refitter.Core\Refitter.Core.csproj">
  <PrivateAssets>all</PrivateAssets>
</ProjectReference>
<SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>

<!-- Remove -->
<!-- Content items copying ../Refitter/bin/$(Configuration)/net8.0/**/* etc. -->
```

**Task rewrite (`src/Refitter.MSBuild/RefitterGenerateTask.cs`):**

- **Remove** all subprocess-related properties:
  - `ProcessRunner`, `RuntimeResolver`, `FileExists`, `ProcessTimeoutMilliseconds`
  - `DefaultRuntimeResolver`, `PreferredRuntimeOrder`, `CompatibilityFallbackOrder`
- **Remove** methods:
  - `ResolveRefitterDll`, `StartProcess`, `TryExecuteRefitter`
  - `HandleProcessStandardOutput`, `HandleProcessErrorOutput`, `ParseGeneratedFilePath`, `ResolveGeneratedFiles`
- **Keep** `FilterFiles` (still needed)
- **Implement new `Execute` flow:**
  1. Find `.refitter` files (same as before)
  2. Filter by `IncludePatterns` (same as before)
  3. For each file:
     - Read JSON
     - `var settings = RefitterSettingsLoader.Load(json, baseDirectory)`
     - `RefitterSettingsLoader.ApplyDefaults(filePath, settings)` (or equivalent)
     - `var generator = await RefitGenerator.CreateAsync(settings)`
     - If `settings.GenerateMultipleFiles`:
       - `var output = generator.GenerateMultipleFiles()`
       - For each file in `output.Files`:
         - Compute path using `OutputPlanner.GetMultiFileOutputPath(...)`
         - Write to disk
         - Collect absolute path
     - Else:
       - `var code = generator.Generate()`
       - Compute path using `OutputPlanner.GetSingleFileOutputPath(...)`
       - Write to disk
       - Collect absolute path
     - If `!SkipValidation`:
       - Validate each OpenAPI spec using `OpenApiValidator.Validate(...)`
       - `validationResult.ThrowIfInvalid()`
  4. Return collected paths as `GeneratedFiles`

**Files to delete:**
- `IProcessRunner.cs`
- `DefaultProcessRunner.cs`
- `IRuntimeResolver.cs`
- `DefaultRuntimeResolver.cs`
- `ProcessExecutionResult.cs`

### Phase 3: Update `Refitter` CLI to use moved Core types

**Files to update:**
- `src/Refitter/GenerateCommand.cs` — use `RefitterSettingsLoader.ApplyDefaults`, `Refitter.Core.OutputPlanner`
- `src/Refitter/GenerationOrchestrator.cs` — use `Refitter.Core.OpenApiValidator`, `Refitter.Core.OutputPlanner`

### Phase 4: Update tests

**`src/Refitter.Tests/MSBuild/RefitterGenerateTaskTests.cs` — major rewrite:**

- **Remove** tests for:
  - `ResolveRefitterDll` (all variants)
  - `GetInstalledDotnetRuntimes` (timeout, termination failure, non-zero exit)
  - `HandleProcessStandardOutput` / `HandleProcessErrorOutput`
  - `ParseGeneratedFilePath`
  - `Execute_Should_Return_False_When_Runtime_Discovery_Throws`
  - `Execute_Should_Fall_Back_When_Runtime_Discovery_Times_Out`
  - `Execute_Should_Use_DotNet9_Runtime_When_Available`
  - `Execute_Should_Fall_Back_To_DotNet8_Runtime_When_Newer_Runtimes_Are_Unavailable`
  - `Execute_Should_Return_False_When_Refitter_Cli_Cannot_Be_Located`
  - `Execute_Should_Log_Timeout_When_Process_Does_Not_Exit`
  - `Execute_Should_Log_Millisecond_Timeout_Value`
  - `Execute_Should_Log_When_Timed_Out_Process_Cannot_Be_Terminated`
  - `Execute_Should_Log_Configured_Timeout_Value`
  - `Execute_Should_Log_ProcessRunner_Exception_And_Return_False`
  - `Execute_Should_Log_When_Process_Exits_With_Non_Zero_Code`
  - `RunProcess_Should_Return_TimedOut_Result_When_Process_Exceeds_Timeout`
  - `RunProcess_Should_Return_Termination_Exception_When_Kill_Fails_After_Timeout`
- **Keep** tests for:
  - `FilterFiles` (all variants)
  - `Execute_Should_Generate_Files_Reported_By_Refitter` (rewrite to verify Core integration)
  - `TryLogCommandLine_Should_Swallow_BuildEngine_Exceptions`
  - `TryLogError_Should_Swallow_BuildEngine_Exceptions`
  - `TryLogErrorFromException_Should_Swallow_BuildEngine_Exceptions`
  - `TryLogErrorFromException_Should_Log_When_BuildEngine_Allows_It`
- **Add** tests for:
  - `Execute_Should_Skip_Validation_When_SkipValidation_Is_True`
  - `Execute_Should_Fail_When_Validation_Fails_And_SkipValidation_Is_False`
  - `Execute_Should_Generate_Multiple_Files`
  - `Execute_Should_Respect_ContractsOutputFolder`
  - `Execute_Should_Apply_SettingsFileDefaults`

**`src/Refitter.Tests/GenerateCommandTests.cs`:**
- Remove `FormatGeneratedFileMarker_Should_Emit_A_Task_Parseable_Full_Path` test

**`src/Refitter.Tests/OutputPlannerTests.cs`:**
- Update namespace if `OutputPlanner` moves to `Refitter.Core`

### Phase 5: Update CI and build scripts

**`.github/workflows/msbuild.yml`:**
- Remove step: `dotnet build -c release ../../src/Refitter/Refitter.csproj`
- Remove step: `dotnet build -c release ../../src/Refitter/Refitter.csproj` (there are two instances)

**`test/MSBuild/build.ps1`:**
- Remove step: `dotnet build -c release ../../src/Refitter/Refitter.csproj`

**`AGENTS.md`:**
- Update MSBuild row: "Requires Refitter CLI binaries to be built first" → "References Refitter.Core directly; no CLI binary dependency"

### Phase 6: Verify

1. `dotnet build -c Release src/Refitter.slnx`
2. `dotnet test --solution src/Refitter.slnx -c Release`
3. Run `test/MSBuild/build.ps1` to verify package smoke tests

## Behavioral Notes (Zero behavioral changes expected)

- **Generated file paths:** `OutputPlanner` logic is preserved exactly (moved to Core, not rewritten).
- **Validation:** `SkipValidation` still works; validation runs the same `OpenApiValidator` logic.
- **Settings defaults:** `ApplySettingsFileDefaults` is preserved exactly (moved to Core).
- **Logging:** MSBuild task logs change from "process stdout" to "direct Core logging." The same information is logged (which file is being processed, success/failure).
- **No CLI marker parsing:** The task no longer needs `GeneratedFile:` markers because it writes files directly and knows their paths.

## Appendix: NuGet Packaging Strategy

### Why `SuppressDependenciesWhenPacking` works

The NuGet `PackTask` supports `SuppressDependenciesWhenPacking` (NuGet/Home#6354). When set to `true`:

1. The `_GetFrameworksWithSuppressedDependencies` target returns the current TFM as suppressed.
2. The `PackTask` receives `FrameworksWithSuppressedDependencies` and suppresses all dependencies for that TFM in the generated `.nuspec`.
3. The package still contains all `BuildOutput` (DLLs) and `Content` files.

### What the package will contain

- `tasks/netstandard2.0/Refitter.MSBuild.dll` — the task assembly
- `tasks/netstandard2.0/Refitter.Core.dll` — the core library
- `tasks/netstandard2.0/NSwag.*.dll` — NSwag dependencies
- `tasks/netstandard2.0/OasReader.dll` — OpenAPI reader
- `tasks/netstandard2.0/Microsoft.OpenApi.dll` — OpenAPI model
- `tasks/netstandard2.0/Microsoft.CodeAnalysis.CSharp.dll` — Roslyn
- `tasks/netstandard2.0/System.Text.Json.dll` — etc.
- `build/Refitter.MSBuild.props` — props file
- `build/Refitter.MSBuild.targets` — targets file
- `tasks/Refitter.MSBuild.props` — copy for MSBuild
- `tasks/Refitter.MSBuild.targets` — copy for MSBuild

The `.nuspec` will have **zero** `<dependencies>` entries. The user's project will not see any of these assemblies at compile time or runtime. They are only loaded by MSBuild when the task executes.

## Appendix: Current MSBuild Task Architecture

```
User Project
    ↓
Refitter.MSBuild (NuGet)
    ↓
Refitter.MSBuild.props / .targets
    ↓
RefitterGenerateTask (netstandard2.0)
    ↓
    ┌─────────────────────────────────────────────────────────┐
    │  1. Find .refitter files                                │
    │  2. Filter by IncludePatterns                           │
    │  3. For each file:                                      │
    │     a. Resolve refitter.dll (runtime detection)         │
    │     b. Spawn: dotnet refitter.dll --settings-file <file> │
    │     c. Parse stdout for "GeneratedFile: " markers       │
    │     d. Return paths to MSBuild                            │
    └─────────────────────────────────────────────────────────┘
    ↓
@(Compile) += @(GeneratedFiles)
```

## Appendix: Target MSBuild Task Architecture

```
User Project
    ↓
Refitter.MSBuild (NuGet)
    ↓
Refitter.MSBuild.props / .targets
    ↓
RefitterGenerateTask (netstandard2.0)
    ↓
    ┌─────────────────────────────────────────────────────────┐
    │  1. Find .refitter files                                │
    │  2. Filter by IncludePatterns                           │
    │  3. For each file:                                      │
    │     a. Read JSON                                          │
    │     b. Load settings (RefitterSettingsLoader)           │
    │     c. Apply defaults                                     │
    │     d. Create generator (RefitGenerator.CreateAsync)    │
    │     e. Generate code (generator.Generate*)              │
    │     f. Plan output paths (OutputPlanner)                │
    │     g. Write files to disk                              │
    │     h. Validate OpenAPI (OpenApiValidator)              │
    │     i. Return paths to MSBuild                            │
    └─────────────────────────────────────────────────────────┘
    ↓
@(Compile) += @(GeneratedFiles)
```
