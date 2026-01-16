# Refitter

Refitter is a tool for generating C# REST API clients using the Refit library. Refitter can generate the Refit interface and contracts from OpenAPI specifications. It comes in 2 forms: a .NET CLI Tool and a C# Source Generator that outputs code during compile time.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- Install .NET 8.0 and 9.0 SDKs (project uses multi-targeting):
  - `wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh`
  - `chmod +x dotnet-install.sh`
  - `./dotnet-install.sh --channel 8.0`
  - `./dotnet-install.sh --channel 9.0`
  - `export PATH="/home/runner/.dotnet:$PATH"`

### Building and Testing
- Restore packages: `dotnet restore src/Refitter.slnx` -- takes 65 seconds. NEVER CANCEL. Set timeout to 90+ seconds.
- Build solution: `dotnet build -c Release src/Refitter.slnx` -- takes 22 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- Run tests: `dotnet test -c Release src/Refitter.slnx` -- takes 284 seconds (4 minutes 44 seconds). NEVER CANCEL. Set timeout to 480+ seconds.
  - NOTE: Some tests may fail in sandboxed environments due to network connectivity issues when downloading external OpenAPI specs. This is expected and does not indicate code issues.
- Apply code formatting: `dotnet format src/Refitter.slnx` -- takes 19 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- Verify formatting: `dotnet format --verify-no-changes src/Refitter.slnx` -- takes 23 seconds. NEVER CANCEL. Set timeout to 60+ seconds.

### Running the CLI Tool
- Run CLI with specific framework: `dotnet run --project src/Refitter --configuration Release --framework net9.0 -- [arguments]`
- Generate from OpenAPI spec: `dotnet run --project src/Refitter --configuration Release --framework net9.0 -- ./openapi.json --output ./GeneratedCode.cs --namespace "MyNamespace"`
- Code generation takes ~4 seconds for typical APIs
- Always use `--help` to see full command options

### Alternative Build Commands
- Quick build (Debug): `cd src && make build` (uses Makefile)
- Release build: `cd src && make release`
- Run tests: `cd src && make test`
- Clean: `cd src && make clean`
- PowerShell build script: `cd src && powershell -File build.ps1`

## Validation
- ALWAYS manually validate any new code by building and running tests after making changes.
- For CLI changes: Test with a real OpenAPI specification to ensure generation works correctly.
- For Core library changes: Run full test suite and verify generated code builds successfully.
- ALWAYS run `dotnet format src/Refitter.slnx` before committing or the CI (.github/workflows/build.yml) will fail.
- ALWAYS validate that EVERY command works before adding it to documentation or instructions.

## Repository Structure and Navigation

### Key Projects
- **Refitter**: CLI tool (.NET 8.0 and 9.0 multi-target) - Main entry point for command-line usage
- **Refitter.Core**: Core library (netstandard2.0) - Contains the code generation logic
- **Refitter.SourceGenerator**: Source generator (netstandard2.0) - Compile-time code generation
- **Refitter.MSBuild**: MSBuild task integration - For build-time generation
- **Refitter.Tests**: Unit tests for core functionality
- **Refitter.SourceGenerator.Tests**: Tests for source generator

### Important Files and Locations
- Main solution: `src/Refitter.slnx`
- CLI entry point: `src/Refitter/Program.cs`
- Core generation logic: `src/Refitter.Core/RefitInterfaceGenerator.cs`
- Settings model: `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`
- Test resources: `src/Refitter.Tests/Resources/`
- Documentation: `README.md`, `CONTRIBUTING.md`, `docs/`
- Build configuration: `src/Directory.Build.props`
- GitHub workflows: `.github/workflows/`

### Common Commands Output Reference

#### Repository Root Structure
```
.devcontainer/          # Development container configuration
.github/                # GitHub workflows and templates
docs/                   # Documentation and examples
src/                    # Source code and solution
  Refitter/             # CLI tool project
  Refitter.Core/        # Core library
  Refitter.SourceGenerator/  # Source generator
  Refitter.Tests/       # Tests
  Refitter.slnx         # Main solution file
images/                 # Project images and assets
```

#### Key Configuration Files
- `.editorconfig`: Code style and formatting rules
- `src/Directory.Build.props`: Common MSBuild properties
- `renovate.json`: Dependency update configuration
- `.gitignore`: Git ignore patterns

## Testing and Development Guidelines

### Unit Testing Patterns
All new code must include unit tests following the pattern used in `Refitter.Tests.Examples` namespace:
```csharp
public class MyFeatureTests
{
    private const string OpenApiSpec = @"..."; // OpenAPI specification

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Generated_Code_Contains_Expected_Pattern()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("ExpectedPattern");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }
}
```

### Validation Scenarios
After making changes, ALWAYS test the following scenarios:
1. **CLI Generation**: Generate code from a sample OpenAPI spec and verify it compiles
2. **Source Generator**: Build a project with .refitter file and verify code generation
3. **Multiple Interfaces**: Test `--multiple-interfaces ByEndpoint` and `ByTag` options
4. **Contract Generation**: Test contract-only and interface-only generation
5. **Build Integration**: Verify generated code builds successfully with C# compiler

### Example Validation Commands
```bash
# Test CLI with sample spec
dotnet run --project src/Refitter --configuration Release --framework net9.0 -- ./sample.json --output ./test.cs --namespace TestNS

# Verify generated code compiles (manual check)
# Test with different settings
dotnet run --project src/Refitter --configuration Release --framework net9.0 -- ./sample.json --multiple-interfaces ByEndpoint --output ./test.cs
```

## Common Development Tasks

### Adding New CLI Options
1. Add new property to `src/Refitter/Settings.cs` with `[CommandOption("--option-name")]` attribute
2. Update `CreateRefitGeneratorSettings()` method in `src/Refitter/GenerateCommand.cs` to map the setting
3. Add corresponding property to `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`
4. Implement logic in `src/Refitter.Core/RefitInterfaceGenerator.cs` or related generator classes
5. Add unit tests in `src/Refitter.Tests/Examples/` following the established pattern
6. Update README.md documentation with the new option

### Recent CLI Options Added
- `--use-apizr`: Integration with Apizr library for request options
- `--use-dynamic-querystring-parameters`: Enable dynamic query string parameter wrapping
- `--use-polymorphic-serialization`: Use System.Text.Json polymorphic serialization
- `--disposable`: Generate IDisposable clients
- `--collection-format`: Control query parameter collection formatting (Multi/Csv/Ssv/Tsv/Pipes)
- `--no-banner`: Hide donation banner in CLI output
- `--integer-type`: Set the .NET type for OpenAPI integers without a format specifier (Int32/Int64)
- `--custom-template-directory`: Custom directory with NSwag fluid templates for code generation. Default is null which uses the default NSwag templates. See <https://github.com/RicoSuter/NSwag/wiki/Templates>

### Working with OpenAPI Specifications
- Test resources are located in `src/Refitter.Tests/Resources/V2/` and `src/Refitter.Tests/Resources/V3/`
- Sample specifications include `SwaggerPetstore.json` for testing
- Always test with both OpenAPI 2.0 and 3.0 specifications

### Code Style and Standards
- Follow existing C# coding conventions defined in `.editorconfig`
- Use PascalCase for public members, camelCase for parameters and local variables
- Include XML documentation for public APIs
- Use meaningful variable and method names
- Keep methods focused with single responsibility

### UI and Console Output
- CLI uses **Spectre.Console** for rich terminal output with colors, tables, and panels
- ASCII art banner is displayed on startup (can be disabled with `--no-banner`)
- Progress indicators and formatted tables show generation status and file information
- Error handling includes styled panels and structured exception display
- Support key display for troubleshooting (disabled when `--no-logging` is used)

## Build and CI/CD Information

### GitHub Workflows
- **build.yml**: Main build workflow (builds and tests)
- **smoke-tests.yml**: Quick validation tests
- **release.yml**: Automated releases
- **release-preview.yml**: Preview releases
- **docfx.yml**: Documentation generation
- **msbuild.yml**: MSBuild-specific testing
- **regression-tests.yml**: Regression testing against known scenarios
- **production-tests.yml**: Production environment testing
- **codecov.yml**: Code coverage reporting

### Expected Build Times
- Package restore: ~65 seconds
- Debug build: ~15 seconds
- Release build: ~22 seconds
- Full test suite: ~284 seconds (4 minutes 44 seconds)
- Code formatting: ~19-23 seconds
- Source generator tests: ~6 seconds

### CI Requirements
- All builds must pass on Windows (primary CI environment)
- Code must be properly formatted (`dotnet format`)
- All tests must pass (network-related test failures in sandboxed environments are acceptable)
- Generated code must compile successfully
- Documentation must be updated for new features

## Important Notes
- **CRITICAL TIMING**: All build and test commands can take several minutes. Never cancel long-running operations.
- **Network Dependencies**: Some tests download OpenAPI specs from external URLs and may fail in restricted environments.
- **Multi-Targeting**: Project targets both .NET 8.0 and 9.0 - always test both when making framework-specific changes.
- **Generated Code**: Always verify that generated Refit interfaces compile and work correctly.
- **Documentation**: Update README.md for any CLI option changes or new features.

## Performance Expectations
- CLI code generation: 1-10 seconds depending on OpenAPI spec complexity
- Source generator: Runs during build time, adds minimal overhead
- Test execution: Allow full test suite to complete (~5 minutes)
- Build process: Complete clean build takes ~2-3 minutes including restore
