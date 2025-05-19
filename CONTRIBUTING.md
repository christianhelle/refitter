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
- Any relevant error messages or logs

### Feature Requests

Feature requests are welcome! When submitting a feature request:
- Provide a clear, detailed description of the proposed feature
- Explain why the feature would be beneficial
- Include any relevant examples or use cases

### Pull Requests

1. Fork the repository
2. Create a new branch for your changes
3. Make your changes following the development guidelines below
4. Submit a pull request with a clear description of the changes and any related issues

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

Example test pattern:

```csharp
public class MyFeatureTests
{
    private const string OpenApiSpec = @"
    // Your OpenAPI specification here
    ";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Generated_Code_Contains_Expected_Pattern()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("ExpectedPattern");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
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
        var generateCode = sut.Generate();
        return generateCode;
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