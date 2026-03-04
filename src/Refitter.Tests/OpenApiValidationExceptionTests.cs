using FluentAssertions;
using Microsoft.OpenApi.Reader;
using Refitter.Validation;
using TUnit.Core;

namespace Refitter.Tests;

public class OpenApiValidationExceptionTests
{
    [Test]
    public void Should_Have_Correct_Message()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var validationResult = new OpenApiValidationResult(diagnostic, stats);

        var exception = new OpenApiValidationException(validationResult);

        exception.Message.Should().Be("OpenAPI validation failed");
    }

    [Test]
    public void Should_Expose_ValidationResult_Property()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var validationResult = new OpenApiValidationResult(diagnostic, stats);

        var exception = new OpenApiValidationException(validationResult);

        exception.ValidationResult.Should().Be(validationResult);
    }

    [Test]
    public void Should_Be_Throwable()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var validationResult = new OpenApiValidationResult(diagnostic, stats);

        Action action = () => throw new OpenApiValidationException(validationResult);

        action.Should().Throw<OpenApiValidationException>()
            .Which.ValidationResult.Should().Be(validationResult);
    }

    [Test]
    public void Should_Inherit_From_Exception()
    {
        var diagnostic = new OpenApiDiagnostic();
        var stats = new OpenApiStats();
        var validationResult = new OpenApiValidationResult(diagnostic, stats);

        var exception = new OpenApiValidationException(validationResult);

        exception.Should().BeAssignableTo<Exception>();
    }
}
