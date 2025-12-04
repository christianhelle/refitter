# Contributing to Refitter

Thank you for your interest in contributing to Refitter! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How to Contribute](#how-to-contribute)
  - [Reporting Issues](#reporting-issues)
  - [Feature Requests](#feature-requests)
  - [Pull Requests](#pull-requests)
- [Development Guidelines](#development-guidelines)
  - [Code Quality](#code-quality)
  - [Testing](#testing)
  - [Documentation](#documentation)
- [Style Guide](#style-guide)

## Code of Conduct

Please review and adhere to our code of conduct to ensure a positive and inclusive environment for all contributors.

## How to Contribute

### Reporting Issues

If you encounter a bug or issue, please create a GitHub issue with the following information:

- A clear, descriptive title
- A detailed description of the issue
- Steps to reproduce the problem
- Expected behavior
- Actual behavior
- Environment information (OS, .NET version, etc.)
- **Support Key**: `[your-support-key]` (The support key is a unique identifier generated on your machine, displayed in the output when running Refitter since v0.5.4, and used for telemetry data)
- Any relevant error messages or logs

### Feature Requests

Feature requests are welcome! Please follow the feature request template in `.github/ISSUE_TEMPLATE/feature_request.md` when submitting a feature request. The template guides you to provide:

- A description of whether your feature request is related to a problem
- A clear, detailed description of the solution you'd like
- Alternative solutions or features you've considered
- Any additional context or screenshots

### Pull Requests

1. Fork the repository
2. Create a new branch for your changes
3. Make your changes following the development guidelines below
4. Submit a pull request with a clear description of the changes and any related issues
5. Follow the pull request template in `.github/PULL_REQUEST_TEMPLATE/pull_request_template.md` which asks for:
   - A description of the changes being made
   - Association with existing issues if applicable
   - Example OpenAPI specifications
   - Example generated Refit interface

## Development Guidelines

### Code Quality

- **All new code must not break existing features**. Ensure your changes don't introduce regressions.
- Maintain consistent code style with the existing codebase.
- Follow the [Style Guide](#style-guide) for the project.
- Prioritize readability and maintainability over cleverness.

### Testing

- **All new code must include unit tests** that verify the functionality works as expected.
- **New features must have unit tests similar to those under the Refitter.Tests.Examples namespace**. These tests must:
  - Contain an example OpenAPI specification stored in a const string in the test class
  - Assert on expected patterns in the generated code
  - Verify that the generated code builds successfully
- Test coverage should be comprehensive, covering both normal operation and edge cases.
- All tests must pass before submitting a pull request.
- **Testing Framework**: The project uses [TUnit](https://github.com/thomhurst/TUnit) instead of xUnit for unit testing, which provides at least 40% faster test execution.

Example test pattern:

```csharp
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

public class MyFeatureTests
{
    private const string OpenApiSpec = @"
    // Your OpenAPI specification here
    ";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_Expected_Pattern()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("ExpectedPattern");
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            // Configure your feature settings here
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
```

### Documentation

- **New features must be documented in the README files**.
- Documentation is especially important for:
  - New CLI tool arguments
  - Changes to the .refitter file format
  - API changes or additions
- Update both code comments and user-facing documentation.
- Documentation should be clear, concise, and include examples where appropriate.

## Style Guide

- Use consistent naming conventions throughout the codebase.
- Use meaningful variable and method names that clearly express their purpose.
- Write clear XML documentation comments for public APIs.
- Keep methods focused and concise, following the single responsibility principle.
- Use proper formatting and indentation.

---

Thank you for contributing to Refitter! Your help improves the project for everyone.