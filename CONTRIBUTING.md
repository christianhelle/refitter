# Contributing to Refitter

Thank you for your interest in contributing to Refitter! This document explains what is expected from a contribution, so you know the full scope of the work **before** you start writing code.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Before You Start](#before-you-start)
- [Definition of Done](#definition-of-done)
- [What to Expect from Code Review](#what-to-expect-from-code-review)
- [Using AI Tools](#using-ai-tools)
- [Getting Help or Handing Over](#getting-help-or-handing-over)
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

Be respectful and assume good intent. This applies both ways: to contributors and to the maintainer. Review comments are about the code, never about the person who wrote it.

## Before You Start

Refitter has a large number of options, and many of them interact with each other (dependency injection, Polly/HttpResilience, Apizr, multiple files, contract generation, and so on). Once a feature is merged, the maintainer is the one who supports it, fixes it when it breaks, and answers the issues about it — for years.

Because of this, **every pull request is held to the same [Definition of Done](#definition-of-done), regardless of how many lines of code it changes.** A 10-line change that adds a new setting needs the same tests and documentation as a 500-line one. In practice, the code change is often the smallest part of the work; tests and documentation usually take longer.

For anything other than a small bug fix:

- **Open an issue first** (or comment on an existing one) describing what you want to add. This lets us agree on the scope, the setting name, and the default behavior before you invest time in it.
- **Read the [Definition of Done](#definition-of-done)** so you know what the full pull request will contain.
- If the scope feels like more than you want to take on, that is completely fine — say so in the issue. See [Getting Help or Handing Over](#getting-help-or-handing-over).

## Definition of Done

A pull request is ready to merge when all of the following are true. Reviews are checked against this list.

**Code**

- [ ] The solution builds in Release (`dotnet build -c Release src/Refitter.slnx`). Most projects have `TreatWarningsAsErrors` enabled.
- [ ] The code follows `.editorconfig` and the [Style Guide](#style-guide) (for example: no `_` prefix on private fields, explicit types instead of `var`).
- [ ] Existing behavior is unchanged unless the change is intentional and discussed. New options are **off by default**.

**Tests**

- [ ] New features have scenario tests in the `Refitter.Tests.Scenarios` namespace (see [Testing](#testing)) that:
  - contain an example OpenAPI specification as a `const string`,
  - assert that the expected code **is** generated,
  - assert that the code it replaces **is not** generated (where relevant),
  - compile the generated code with `BuildHelper.BuildCSharp(generatedCode)`, in a test marked `[TestCategory("Integration")]`.
- [ ] Bug fixes have a regression test that fails without the fix.
- [ ] If the option interacts with other options, those combinations are tested too. For example, a change to dependency injection output should be covered alongside `HttpMessageHandlers`, Polly/HttpResilience and the other options tested in `src/Refitter.Tests/DependencyInjection/`.
- [ ] The full test suite passes locally: `dotnet test --solution src/Refitter.slnx -c Release`. See `AGENTS.md` for the pre-build step the source generator tests need.

A test that only checks for a string in the output is not enough on its own: it can pass on code that does not compile. The build verification is what catches that.

**Documentation**

- [ ] A new `.refitter` setting is documented in **all** of these places:
  - `README.md`: the `.refitter` sample and the settings description list
  - `docs/docfx_project/articles/refitter-file-format.md`
  - `docs/json-schema.json` (editors use this for validation and auto-complete)
- [ ] A new CLI argument is also documented in the CLI usage section of `README.md`.
- [ ] Public APIs and settings have XML documentation comments.
- [ ] Any requirements or limitations are written down: for example, a minimum Refit version, or options that are ignored when another option is enabled.

**Pull request**

- [ ] The description explains what changed and why. If there is a related issue, it is linked with `Closes #<issue number>`.
- [ ] "Allow edits by maintainers" is enabled.

## What to Expect from Code Review

Every pull request is reviewed by several automated tools (CodeRabbit, SonarQube Cloud, Codecov and AI review agents). The maintainer then pulls the branch, builds it, and reviews it by hand.

The result is often a **long, itemized review — even for a small change.** That is normal here and it is not a judgement of you or your work. It is the Definition of Done applied to your pull request, written out in full so nothing is left vague. Reviews usually separate:

- **Blocking** items: these must be fixed before merge (for example, generated code that does not compile, or missing tests or documentation).
- **Smaller points**: suggestions about naming, style or wording. These are open for discussion.

If you disagree with a review comment, reply on the thread and explain why. Pushing back is welcome; it often leads to a better result.

## Using AI Tools

You are welcome to use AI coding tools. The repository includes an [`AGENTS.md`](AGENTS.md) file that describes the build, test conventions and project layout for coding agents, and it is a good way to produce the tests and documentation listed above.

Whether code is written by hand or with AI, you are responsible for the result: it has to compile, pass the tests and do what the pull request says it does.

## Getting Help or Handing Over

The requirements above can be a lot of work, especially for a first contribution. You have options:

- **Ask for help.** If you are stuck on the tests, the build helper or the documentation, ask in the pull request. The maintainer is happy to point you in the right direction or to push the missing parts to your branch.
- **Hand it over.** If you only want to contribute the core change, say so. With "Allow edits by maintainers" enabled, the maintainer can finish the pull request. Your commits and credit are kept.
- **Open an issue instead.** A well-described feature request or bug report with an example OpenAPI specification is a valuable contribution on its own.

What does not work well is a pull request that stays incomplete with no reply: it cannot be merged and it blocks others from picking up the work. Unfinished pull requests may be closed or taken over after a while.

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

1. For anything other than a small bug fix, open or comment on an issue first (see [Before You Start](#before-you-start))
2. Fork the repository
3. Create a new branch for your changes
4. Make your changes following the development guidelines below
5. Check your changes against the [Definition of Done](#definition-of-done)
6. Submit a pull request with a clear description of the changes and any related issues, with "Allow edits by maintainers" enabled
7. Follow the pull request template in `.github/PULL_REQUEST_TEMPLATE/pull_request_template.md` which asks for:
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
- **New features must have unit tests similar to those under the Refitter.Tests.Scenarios namespace**. These tests must:
  - Contain an example OpenAPI specification stored in a const string in the test class
  - Assert on expected patterns in the generated code
  - Verify that the generated code builds successfully
- Test coverage should be comprehensive, covering both normal operation and edge cases.
- All tests must pass before submitting a pull request.
- **Testing Framework**: The project uses [MSTest](https://github.com/microsoft/testfx) v4 running on Microsoft.Testing.Platform. Mark test classes with `[TestClass]`, test methods with `[TestMethod]`, and data-driven cases with `[DataRow(...)]`.
- **Assertion Library**: Assertions use [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions), the Apache-2.0 community fork of FluentAssertions, not MSTest's `Assert` class.

Example test pattern:

```csharp
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

[TestClass]
public class MyFeatureTests
{
    private const string OpenApiSpec = @"
    // Your OpenAPI specification here
    ";

    [TestMethod]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task Generated_Code_Contains_Expected_Pattern()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("ExpectedPattern");
    }

    [TestMethod]
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
  - New CLI tool arguments (`README.md`)
  - Changes to the .refitter file format (`README.md`, `docs/docfx_project/articles/refitter-file-format.md` **and** `docs/json-schema.json`)
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