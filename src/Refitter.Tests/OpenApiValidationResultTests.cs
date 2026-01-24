using FluentAssertions;
using Microsoft.OpenApi.Readers;
using Refitter.Validation;
using TUnit.Core;

namespace Refitter.Tests;

public class OpenApiValidationResultTests
{
    [Test]
    public void IsValid_Should_Return_True_When_No_Errors()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var result = new OpenApiValidationResult(diagnostic, stats);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Should_Expose_Diagnostics_Property()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var result = new OpenApiValidationResult(diagnostic, stats);

        result.Diagnostics.Should().Be(diagnostic);
    }

    [Test]
    public void Should_Expose_Statistics_Property()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var result = new OpenApiValidationResult(diagnostic, stats);

        result.Statistics.Should().Be(stats);
    }

    [Test]
    public void ThrowIfInvalid_Should_Not_Throw_When_Valid()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var result = new OpenApiValidationResult(diagnostic, stats);

        var action = () => result.ThrowIfInvalid();

        action.Should().NotThrow();
    }
}
