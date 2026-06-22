using FluentAssertions;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter.Tests;

/// <summary>
/// Smoke tests for <see cref="SimpleGenerationReporter"/>. These exercise every method
/// for line/branch coverage; the output is not asserted because TUnit prohibits
/// redirecting Console.Out (TUnit0055). The methods only write to Console so they
/// don't throw — a clean run is the success criterion.
/// </summary>

public class SimpleGenerationReporterTests
{
    [Test]
    public void ReportHeader_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportHeader("1.2.3");

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSupportKey_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportSupportKey("TEST-KEY-123");

        act.Should().NotThrow();
    }

    [Test]
    public async Task ReportSingleFileGenerationProgressAsync_Does_Not_Throw()
    {
        await new SimpleGenerationReporter().ReportSingleFileGenerationProgressAsync();
    }

    [Test]
    public void ReportSingleFileOutput_Does_Not_Throw()
    {
        var act = () =>
            new SimpleGenerationReporter().ReportSingleFileOutput("Output.cs", "/tmp", "12 KB", 300);

        act.Should().NotThrow();
    }

    [Test]
    public async Task GenerateMultipleFilesWithProgressAsync_Invokes_Generator_And_Returns_Result()
    {
        var called = false;
        var result = await new SimpleGenerationReporter()
            .GenerateMultipleFilesWithProgressAsync(() =>
            {
                called = true;
                return new GeneratorOutput([]);
            });

        called.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Test]
    public void BeginMultiFileOutput_AddFile_And_Complete_Do_Not_Throw()
    {
        var act = () =>
        {
            var report = new SimpleGenerationReporter().BeginMultiFileOutput();
            report.AddFile("Api.cs", "/tmp", "5 KB", 100);
            report.Complete(1, "5 KB", 100);
        };

        act.Should().NotThrow();
    }

    [Test]
    public void ReportFileWritten_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportFileWritten("/output/Api.cs");

        act.Should().NotThrow();
    }

    [Test]
    public async Task ValidateWithProgressAsync_Invokes_Validator()
    {
        var called = false;
        await new SimpleGenerationReporter().ValidateWithProgressAsync(async () =>
        {
            called = true;
            return await Task.FromResult(new OpenApiValidationResult(new Microsoft.OpenApi.Reader.OpenApiDiagnostic(), new OpenApiStats()));
        });

        called.Should().BeTrue();
    }

    [Test]
    public void ReportValidationFailed_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportValidationFailed();

        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationDiagnostic_Error_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "Something went wrong"),
            isError: true);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationDiagnostic_Warning_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "A warning"),
            isError: false);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationStatistics_Does_Not_Throw()
    {
        var result = new OpenApiValidationResult(
            new Microsoft.OpenApi.Reader.OpenApiDiagnostic(),
            new OpenApiStats());

        var act = () => new SimpleGenerationReporter().ReportValidationStatistics(result);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSuccess_SingleFile_Does_Not_Throw()
    {
        var act = () =>
            new SimpleGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: false);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSuccess_MultipleFiles_Does_Not_Throw()
    {
        var act = () =>
            new SimpleGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: true);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportDonationBanner_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportDonationBanner();

        act.Should().NotThrow();
    }

    [Test]
    public void ReportConfigurationWarnings_Does_Not_Throw()
    {
        var warnings = new List<Warning>
        {
            new("Title1", "Desc1"),
            new("Title2", "Desc2"),
        };

        var act = () => new SimpleGenerationReporter().ReportConfigurationWarnings(warnings);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportAllPathsFilteredWarning_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter()
            .ReportAllPathsFilteredWarning(["^/pets", "^/users"]);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSettingsFileGenerated_Does_Not_Throw()
    {
        var act = () =>
            new SimpleGenerationReporter().ReportSettingsFileGenerated("/tmp/petstore.refitter");

        act.Should().NotThrow();
    }

    [Test]
    public void ReportGenerationFailed_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportGenerationFailed();

        act.Should().NotThrow();
    }

    [Test]
    public void ReportUnsupportedVersion_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportUnsupportedVersion("1.0");

        act.Should().NotThrow();
    }

    [Test]
    public void ReportExceptionDetails_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter()
            .ReportExceptionDetails(new InvalidOperationException("boom"));

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSkipValidationSuggestion_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportSkipValidationSuggestion();

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSupportHelp_Does_Not_Throw()
    {
        var act = () => new SimpleGenerationReporter().ReportSupportHelp();

        act.Should().NotThrow();
    }
}
