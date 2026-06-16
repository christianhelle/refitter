using FluentAssertions;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;
using TUnit.Core;

namespace Refitter.Tests;

/// <summary>
/// Smoke tests for <see cref="RichGenerationReporter"/>. These exercise every method
/// for line/branch coverage; the output is not asserted because the methods write to
/// AnsiConsole/Spectre.Console which cannot be captured in unit tests.
/// A clean run (no exception) is the success criterion.
/// </summary>
public class RichGenerationReporterTests
{
    [Test]
    public void ReportHeader_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportHeader("1.2.3");
        act.Should().NotThrow();
    }

    [Test]
    public void ReportSupportKey_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportSupportKey("TEST-KEY-123");
        act.Should().NotThrow();
    }

    [Test]
    [NotInParallel("SpectreConsole")]
    public async Task ReportSingleFileGenerationProgressAsync_Does_Not_Throw()
    {
        await new RichGenerationReporter().ReportSingleFileGenerationProgressAsync();
    }

    [Test]
    public void ReportSingleFileOutput_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSingleFileOutput("Output.cs", "/tmp", "12 KB", 300);
        act.Should().NotThrow();
    }

    [Test]
    [NotInParallel("SpectreConsole")]
    public async Task GenerateMultipleFilesWithProgressAsync_Invokes_Generator()
    {
        var called = false;
        var result = await new RichGenerationReporter()
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
            var report = new RichGenerationReporter().BeginMultiFileOutput();
            report.AddFile("Api.cs", "/tmp", "5 KB", 100);
            report.Complete(1, "5 KB", 100);
        };

        act.Should().NotThrow();
    }

    [Test]
    public void ReportFileWritten_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportFileWritten("/output/Api.cs");
        act.Should().NotThrow();
    }

    [Test]
    [NotInParallel("SpectreConsole")]
    public async Task ValidateWithProgressAsync_Invokes_Validator()
    {
        var called = false;
        await new RichGenerationReporter().ValidateWithProgressAsync(async () =>
        {
            called = true;
            return await Task.FromResult(new OpenApiValidationResult(
                new Microsoft.OpenApi.Reader.OpenApiDiagnostic(),
                new OpenApiStats()));
        });

        called.Should().BeTrue();
    }

    [Test]
    public void ReportValidationFailed_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportValidationFailed();
        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationDiagnostic_Error_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "Something went wrong"),
            isError: true);
        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationDiagnostic_Warning_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportValidationDiagnostic(
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

        var act = () => new RichGenerationReporter().ReportValidationStatistics(result);
        act.Should().NotThrow();
    }

    [Test]
    public void ReportSuccess_SingleFile_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: false);
        act.Should().NotThrow();
    }

    [Test]
    public void ReportSuccess_MultipleFiles_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: true);
        act.Should().NotThrow();
    }

    [Test]
    public void ReportDonationBanner_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportDonationBanner();
        act.Should().NotThrow();
    }

    [Test]
    public void ReportConfigurationWarnings_Does_Not_Throw()
    {
        var warnings = new List<(string Title, string Description)>
        {
            ("Title1", "Desc1"),
            ("Title2", "Desc2")
        };

        var act = () => new RichGenerationReporter().ReportConfigurationWarnings(warnings);
        act.Should().NotThrow();
    }

    [Test]
    public void ReportAllPathsFilteredWarning_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter()
            .ReportAllPathsFilteredWarning(["^/pets", "^/users"]);
        act.Should().NotThrow();
    }

    [Test]
    public void ReportSettingsFileGenerated_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSettingsFileGenerated("/tmp/petstore.refitter");
        act.Should().NotThrow();
    }

    [Test]
    public void ReportGenerationFailed_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportGenerationFailed();
        act.Should().NotThrow();
    }

    [Test]
    public void ReportUnsupportedVersion_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportUnsupportedVersion("1.0");
        act.Should().NotThrow();
    }

    [Test]
    public void ReportExceptionDetails_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter()
            .ReportExceptionDetails(new InvalidOperationException("boom"));
        act.Should().NotThrow();
    }

    [Test]
    public void ReportSkipValidationSuggestion_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportSkipValidationSuggestion();
        act.Should().NotThrow();
    }

    [Test]
    public void ReportSupportHelp_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportSupportHelp();
        act.Should().NotThrow();
    }
}
