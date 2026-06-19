# AGENTS.md — Refitter

OpenAPI-to-Refit code generator. Multi-package solution: CLI tool, MSBuild task, C# Source Generator, and Core library.

## Build & Test

- Solution: `src/Refitter.slnx`
- SDK: `10.0.100` with `rollForward: latestFeature` (see `global.json`)
- Build: `dotnet build -c Release src/Refitter.slnx`
- Test: `dotnet test --solution src/Refitter.slnx -c Release`

**Source Generator tests require a pre-build step.** Before running `dotnet test` on `Refitter.SourceGenerator.Tests`, the CI does:

```bash
rm -rf src/Refitter.SourceGenerator.Tests/AdditionalFiles/Generated
dotnet restore src/Refitter.SourceGenerator.Tests/Refitter.SourceGenerator.Tests.csproj
dotnet msbuild src/Refitter.SourceGenerator.Tests/Refitter.SourceGenerator.Tests.csproj
```

Run these first if source generator tests fail with missing generated types.

## Package Boundaries

| Project | TFM | Role |
|---|---|---|
| `Refitter.Core` | `netstandard2.0` | Core generation logic (NSwag-based) |
| `Refitter` | `net8.0;net9.0;net10.0` | CLI tool (`PackAsTool`, container support) |
| `Refitter.MSBuild` | `netstandard2.0` | MSBuild task; references `Refitter.Core` directly (in-process); no CLI binary dependency |
| `Refitter.SourceGenerator` | `netstandard2.0` | Roslyn source generator; emits code in-memory via `AddSource()` (not to disk since v2.0.0) |
| `Refitter.Tests` | `net10.0` | Unit tests |
| `Refitter.SourceGenerator.Tests` | `net8.0;net10.0` | Source generator integration tests |

## Testing

- **Framework**: TUnit (not xUnit). Attributes: `[Test]`, `[Arguments(...)]`.
- **Runner**: `Microsoft.Testing.Platform` (configured in `global.json`). Both test projects set `OutputType=Exe` and `GenerateProgramFile=false`.
- **Unit test pattern**: Scenario tests under `Refitter.Tests.Scenarios` contain an OpenAPI spec as a `const string`, generate code via `RefitGenerator.CreateAsync(...).Generate()`, assert string patterns, and verify compilation with `BuildHelper.BuildCSharp(generatedCode)`.
- **Temp file helper**: `SwaggerFileHelper.CreateSwaggerFile(contents)` creates a temp YAML file; `CreateSwaggerJsonFile` for JSON.
- **Build helper**: `BuildHelper.BuildCSharp()` writes generated code to a temp project and invokes `dotnet build` to verify it compiles. It defaults to `net8.0` but supports `net9.0` and `net10.0`.
- **Source generator tests**: Reference the generator as an `Analyzer` with `ReferenceOutputAssembly=false`. They verify generated types exist at runtime and have Refit HTTP attributes.

## CI / Smoke Tests

- Build workflow: `.github/workflows/build.yml`
- Smoke tests: `.github/workflows/smoke-tests.yml` runs `test/smoke-tests.ps1`
- The smoke test script publishes the CLI from source, generates clients against dozens of OpenAPI specs (v2.0, v3.0, v3.1, v3.4), and compiles them against console apps in `test/ConsoleApp/`.

## Code Style

- `.editorconfig` is authoritative.
- 4-space indentation; `crlf` line endings for C# files per `.editorconfig`.
- `var` is discouraged (`csharp_style_var_* = false:silent`).
- `TreatWarningsAsErrors` is enabled on most production projects.
- **Private fields must NOT be prefixed with `_`** — use camelCase without underscore prefix (e.g., `openApiPath` not `_openApiPath`).

## Key Conventions

- **New features** must have unit tests in the `Refitter.Tests.Scenarios` namespace following the pattern: spec string → generate → assert → build.
- **New CLI arguments** must be documented in `README.md` and in the `.refitter` file format docs.
- **`.refitter` files** are the settings format for all three distribution forms (CLI, MSBuild, Source Generator).
- **Source generator `.refitter` files** are automatically included as `AdditionalFiles` via the package props. The test project explicitly excludes `PropertyNamingPolicy.refitter` during design-time builds (`Condition="'$(DesignTimeBuild)' != 'true'"`).
- **MSBuild** package includes a custom `.targets` file that runs `RefitterGenerateTask` before `BeforeCompile`.

## Commit Discipline

- **Commit as often as possible** in small, logical groups. Each commit should represent one coherent change (e.g., one file created, one bug fix, one adapter added).
- **Build and run tests before every commit.** The minimum check is `dotnet build -c Release src/Refitter.slnx` followed by `dotnet test --solution src/Refitter.slnx -c Release --no-build`.
- **Never commit broken code.** If tests fail, fix them before committing. If the fix requires additional changes, commit those together as a "fix: ..." commit.
- **Commit messages** follow the pattern: `type: description` where type is one of `feat`, `fix`, `refactor`, `docs`, `test`, `chore`. Keep the subject line under 50 characters when possible.

## Agent skills

### Issue tracker

Issues are tracked as GitHub Issues against `christianhelle/refitter`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five-label vocabulary (needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context repo. See `docs/agents/domain.md`.

## Documentation

- API docs are generated with DocFX from `docs/docfx_project/docfx.json`. To build locally:
  ```bash
  dotnet tool update -g docfx
  docfx docs/docfx_project/docfx.json
  ```
