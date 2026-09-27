using AwesomeAssertions;
using Refitter.Core.Validation;

namespace Refitter.Tests.OpenApi;


public class OpenApiValidationResultTests
{
    [Test]
    public void IsValid_Should_Return_True_When_No_Errors()
    {
        var diagnostics = new ValidationDiagnostics();
        diagnostics.Warnings.Add(new ValidationIssue("#/", "a warning"));
        var result = new OpenApiValidationResult(diagnostics, new OpenApiStats());

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void IsValid_Should_Return_False_When_There_Are_Errors()
    {
        var diagnostics = new ValidationDiagnostics();
        diagnostics.Errors.Add(new ValidationIssue("#/", "an error"));
        var result = new OpenApiValidationResult(diagnostics, new OpenApiStats());

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Should_Expose_Diagnostics_Property()
    {
        var diagnostics = new ValidationDiagnostics { SpecificationVersion = OpenApiSpecificationVersion.OpenApi3_1 };
        var result = new OpenApiValidationResult(diagnostics, new OpenApiStats());

        result.Diagnostics.Should().Be(diagnostics);
        result.Diagnostics.SpecificationVersion.Should().Be(OpenApiSpecificationVersion.OpenApi3_1);
    }

    [Test]
    public void Should_Expose_Statistics_Property()
    {
        var stats = new OpenApiStats();
        var result = new OpenApiValidationResult(new ValidationDiagnostics(), stats);

        result.Statistics.Should().Be(stats);
    }

    [Test]
    public void ThrowIfInvalid_Should_Not_Throw_When_Valid()
    {
        var result = new OpenApiValidationResult(new ValidationDiagnostics(), new OpenApiStats());

        var action = () => result.ThrowIfInvalid();

        action.Should().NotThrow();
    }

    [Test]
    public void ThrowIfInvalid_Should_Throw_When_There_Are_Errors()
    {
        var diagnostics = new ValidationDiagnostics();
        diagnostics.Errors.Add(new ValidationIssue("#/", "an error"));
        var result = new OpenApiValidationResult(diagnostics, new OpenApiStats());

        var action = () => result.ThrowIfInvalid();

        action.Should().Throw<OpenApiValidationException>()
            .Which.ValidationResult.Should().Be(result);
    }
}
