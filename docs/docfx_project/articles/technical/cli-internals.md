# CLI Tool Internals

The `refitter` .NET tool ([`src/Refitter`](https://github.com/christianhelle/refitter/tree/main/src/Refitter)) is a thin host around [`RefitterRunner`](core-pipeline.md#refitterrunner). Its job is to turn command line arguments or a `.refitter` file into `RefitGeneratorSettings`, validate them, run the generation, write files, and report what happened. It is also the engine behind [`Refitter.MSBuild`](msbuild-internals.md), which runs it as a child process.

The CLI targets `net8.0`, `net9.0` and `net10.0` and is published as a .NET tool and as a Docker image.

## End-to-end flow

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Main as Program.Main
    participant App as Spectre CommandApp
    participant Cmd as GenerateCommand
    participant Val as SettingsValidator
    participant Map as SettingsMapper / RefitterSettingsLoader
    participant Orc as GenerationOrchestrator
    participant Run as RefitterRunner
    participant FW as CliFileWriter
    participant Rep as IGenerationReporter

    User->>Main: refitter [spec] [options]
    Main->>App: Run(args) (no args → --help)
    App->>Cmd: Validate(context, settings)
    Cmd->>Val: Validate(settings, out refitSettings)
    Val-->>Cmd: ValidationResult (+ cached RefitGeneratorSettings)
    App->>Cmd: ExecuteAsync(context, settings)
    alt settings were cached by Validate
        Cmd->>Cmd: use cached settings
    else --settings-file
        Cmd->>Map: RefitterSettingsLoader.LoadSettingsFile(json, path)
    else command line only
        Cmd->>Map: SettingsMapper.Map(settings)
    end
    Cmd->>Map: ApplyCliOverrides (settings-file runs only)
    Cmd->>Cmd: CreateReporter (Silent / Simple / Rich)
    Cmd->>Orc: RunAsync(refitSettings, settings, reporter)
    Orc->>Rep: ReportHeader, ReportSupportKey
    Orc->>Run: RunAsync(settings, CliFileWriter, OpenApiValidatorAdapter?)
    Run->>FW: WriteAsync(path, content) per file
    FW->>Rep: ReportFileWritten(path)
    Run-->>Orc: RunResult
    alt ExitCode == 0
        Orc->>Orc: Analytics.LogFeatureUsage
        Orc->>Orc: write .refitter file (command line runs only)
        Orc->>Rep: warnings, ReportSuccess, banner
        Orc-->>User: exit code 0
    else failure
        Orc->>Rep: ReportGenerationFailed + diagnostics + help
        Orc->>Orc: Analytics.LogError
        Orc-->>User: non-zero exit code
    end
```

## Parsing arguments

[`Program.Main`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/Program.cs) creates a [Spectre.Console.Cli](https://spectreconsole.net/cli/) `CommandApp<GenerateCommand>`. Every command line option is a property with a `[CommandOption]` attribute on [`Settings`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/Settings.cs). Spectre calls `GenerateCommand.Validate` first and `ExecuteAsync` only if validation succeeds.

## Validating settings

[`SettingsValidator`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/SettingsValidator.cs) has two paths:

```mermaid
flowchart TD
    V["SettingsValidator.Validate"] --> AH["check authentication header style"]
    AH --> ONE{"exactly one of<br/>OpenAPI path / --settings-file?"}
    ONE -- no --> ERR["ValidationResult.Error"]
    ONE -- "--settings-file" --> FILE
    ONE -- "OpenAPI path" --> CLI

    subgraph FILE["Settings file path"]
        direction TB
        F1["deserialize .refitter JSON<br/>(friendly message for bad enum values)"] --> F2["validate file and output settings"]
        F2 --> F3["RefitterSettingsLoader.PrepareSettings<br/>apply defaults, resolve relative spec paths"]
        F3 --> F4["check every spec file exists"]
        F4 --> F5["check JSON library version conflicts"]
    end

    subgraph CLI["Command line path"]
        direction TB
        C1["validate multi-file output options"] --> C2["validate operation name template / URL"]
    end

    FILE --> OK["ValidationResult.Success<br/>+ RefitGeneratorSettings cached"]
    CLI --> OK
```

The settings produced during validation are cached on the command so `ExecuteAsync` doesn't parse the file twice.

## Building RefitGeneratorSettings

There are two sources of settings:

- **A `.refitter` file.** [`RefitterSettingsLoader`](https://github.com/christianhelle/refitter/blob/main/src/Refitter.Core/Settings/RefitterSettingsLoader.cs) (in `Refitter.Core`, shared with the source generator) deserializes the JSON and applies defaults. An OpenAPI path passed on the command line replaces the one in the file. [`SettingsMapper.ApplyCliOverrides`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/SettingsMapper.cs) then lets `--json-library-version` win only when the file still has the default value `8.0`, so an explicit value in the file always takes precedence.
- **Command line options only.** `SettingsMapper.Map` uses [`ConventionMapper`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/ConventionMapper.cs), which copies every public property that has the same name and type on both CLI `Settings` and `RefitGeneratorSettings` by reflection. Options whose meaning is inverted or combined are mapped explicitly afterwards, for example `GenerateContracts = !InterfaceOnly`, `GenerateClients = !ContractOnly`, and `GenerateMultipleFiles` is turned on by `--multiple-files` or `--contracts-output`.

Because of the convention mapper, a new CLI option that matches an existing `RefitGeneratorSettings` property by name and type needs no extra mapping code. The [`ConventionMapperTests`](https://github.com/christianhelle/refitter/blob/main/src/Refitter.Tests/ConventionMapperTests.cs) guard this.

## Reporters

All console output goes through [`IGenerationReporter`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/IGenerationReporter.cs). The command picks one implementation per run:

```mermaid
classDiagram
    class IGenerationReporter {
        <<interface>>
        ReportHeader(version)
        ReportSupportKey(supportKey)
        ReportFileWritten(path)
        ReportSuccess(duration, multipleFiles)
        ReportGenerationFailed()
        ReportValidationDiagnostic(error, isError)
    }
    class RichGenerationReporter {
        Spectre.Console markup
        spinners, tables, panels
    }
    class SimpleGenerationReporter {
        plain text
        prints "GeneratedFile: full path"
    }
    class SilentGenerationReporter {
        no output
    }
    IGenerationReporter <|.. RichGenerationReporter : default
    IGenerationReporter <|.. SimpleGenerationReporter : --simple-output
    IGenerationReporter <|.. SilentGenerationReporter : --silent / "silent": true
```

| Reporter | Selected by | Used for |
|---|---|---|
| `RichGenerationReporter` | default | Interactive use: ASCII-art header, spinners, tables with OpenAPI statistics and generated files, panels for success and errors |
| `SimpleGenerationReporter` | `--simple-output` | CI logs and other tools. Prints one `GeneratedFile: <full path>` line per written file |
| `SilentGenerationReporter` | `--silent` or `"silent": true` in the `.refitter` file | No output at all |

The `GeneratedFile:` line is a contract with [`Refitter.MSBuild`](msbuild-internals.md#the-generatedfile-contract): the MSBuild task always passes `--simple-output` and reads those lines to know which files to compile.

## Writing files

[`CliFileWriter`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/CliFileWriter.cs) implements `IFileWriter`. For every `PlannedFile` from [`OutputPlanner`](core-pipeline.md#planning-the-output-paths), it creates the directory, writes the content and tells the reporter. Writing only starts after validation has succeeded.

When the CLI was run without `--settings-file`, [`GenerationOrchestrator`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/GenerationOrchestrator.cs) also serializes the effective settings to a `.refitter` file next to the output, so the same generation can later be repeated with `--settings-file`, `Refitter.MSBuild` or `Refitter.SourceGenerator`.

## Errors and exit codes

`RefitterRunner` never throws; it returns a `RunResult` with an exit code. `GenerationOrchestrator` rethrows a captured exception so it can report it consistently:

```mermaid
flowchart TD
    R["RunResult"] --> E{"ExitCode == 0?"}
    E -- yes --> OK["success output<br/>exit 0"]
    E -- no --> T{"exception type"}
    T -- "OpenApiValidationException" --> VF["validation failed<br/>errors + warnings listed<br/>suggest --skip-validation"]
    T -- "OpenApiUnsupportedSpecVersionException" --> UV["unsupported OpenAPI version"]
    T -- "other" --> GE["exception details"]
    VF --> SUP["support help + Analytics.LogError"]
    UV --> SUP
    GE --> SUP
    SUP --> X["exit ToProcessExitCode(exception)"]
```

`ToProcessExitCode` returns the exception's `HResult`, unless its low 8 bits are zero, in which case it returns `1`. Unix keeps only the low byte of a process exit code, and common HResults such as `0x80131500` (the default for `System.Exception`) end in `0x00`. Without this mapping a failed generation would look like success to shells, CI systems and the MSBuild task.

## Telemetry

Unless `--no-logging` is passed, [`Analytics`](https://github.com/christianhelle/refitter/blob/main/src/Refitter/Analytics.cs) records which options were used (never their values for paths, URLs, namespaces or other free text) and unhandled errors. When the CLI is started by `Refitter.MSBuild`, extra `--telemetry-source msbuild`, `--telemetry-file-count` and `--telemetry-runtime` options tag the events so MSBuild usage can be told apart from direct CLI usage. Settings files are passed through `SettingsSanitizer.Sanitize`, which keeps only known setting names and redacts free-text values.
