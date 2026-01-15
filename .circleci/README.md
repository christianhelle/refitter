# CircleCI Configuration

This directory contains the CircleCI configuration for the Refitter project.

## Overview

The CircleCI pipeline is configured to:

1. **Checkout code** from the repository
2. **Setup .NET SDK** environment (versions 8.0, 9.0, and 10.0)
3. **Restore dependencies** using `dotnet restore`
4. **Build solution** in Release configuration with versioning
5. **Run tests** across all test projects
6. **Store test results** and artifacts
7. **Upload NuGet packages** as build artifacts

## Configuration Details

### Executor
- Uses the official Microsoft .NET SDK Docker image (`mcr.microsoft.com/dotnet/sdk:10.0`)
- Installs additional .NET SDK versions (8.0 and 9.0) to support multi-targeting
- Sets environment variables to skip first-time experience and telemetry

### Build Process

The pipeline executes the following steps:

1. **Setup .NET SDK**: Installs .NET 8.0 and 9.0 SDKs alongside the base 10.0 SDK
2. **Restore Dependencies**: Runs `dotnet restore src/Refitter.sln`
3. **Build Solution**: Builds in Release mode with SourceLink and versioning
4. **Run Tests**: Executes all tests with TRX logging
5. **Store Results**: Converts test results to JUnit format and stores artifacts
6. **Upload Packages**: Collects and stores generated NuGet packages

### Environment Variables

The following CircleCI environment variables are used:

- `CIRCLE_BUILD_NUM`: Used for package versioning (e.g., `1.0.0-ci.123`)
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE`: Set to `true` to skip .NET first-time setup
- `DOTNET_CLI_TELEMETRY_OPTOUT`: Set to `true` to disable telemetry

### Artifacts

The pipeline stores:

- **Test Results**: TRX and JUnit formatted test results
- **Build Artifacts**: All build outputs
- **NuGet Packages**: Generated `.nupkg` files

## Workflow

The default workflow `build-and-test` runs on every commit and pull request, executing the `build` job.

## Testing Locally

To validate the CircleCI configuration locally:

```bash
# Install CircleCI CLI
curl -fLSs https://raw.githubusercontent.com/CircleCI-Public/circleci-cli/master/install.sh | bash

# Validate configuration
circleci config validate

# Run the pipeline locally (requires Docker)
circleci local execute
```

## Requirements

- CircleCI account connected to the repository
- Docker support enabled in CircleCI project settings
- Repository configured to trigger builds on commits

## Troubleshooting

### Build Failures

If builds fail, check:

1. .NET SDK installation logs in the "Install .NET SDK versions" step
2. Package restore logs for dependency issues
3. Build output for compilation errors
4. Test results for failing tests

### Multi-Targeting Issues

The project targets .NET 8.0, 9.0, and 10.0. Ensure all SDK versions are properly installed by checking the output of `dotnet --list-sdks`.

## Further Reading

- [CircleCI Documentation](https://circleci.com/docs/)
- [.NET Core on CircleCI](https://circleci.com/docs/language-dotnet/)
- [CircleCI Configuration Reference](https://circleci.com/docs/configuration-reference/)
