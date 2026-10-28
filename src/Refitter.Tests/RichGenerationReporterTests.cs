using AwesomeAssertions;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter.Tests;

/// <summary>
/// Smoke tests for <see cref="RichGenerationReporter"/>. These exercise every method
/// for line/branch coverage; the output is not asserted because the methods write to
/// AnsiConsole/Spectre.Console which cannot be captured in unit tests.
/// A clean run (no exception) is the success criterion.
/// </summary>

[TestClass]
public class RichGenerationReporterTests
{
    [TestMethod]
    public void ReportHeader_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportHeader("1.2.3");
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSupportKey_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportSupportKey("TEST-KEY-123");
        act.Should().NotThrow();
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task ReportSingleFileGenerationProgressAsync_Does_Not_Throw()
    {
        await new RichGenerationReporter().ReportSingleFileGenerationProgressAsync();
    }

    [TestMethod]
    public void ReportSingleFileOutput_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSingleFileOutput("Output.cs", "/tmp", "12 KB", 300);
        act.Should().NotThrow();
    }

    [TestMethod]
    [DoNotParallelize]
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

    [TestMethod]
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

    [TestMethod]
    public void ReportFileWritten_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportFileWritten("/output/Api.cs");
        act.Should().NotThrow();
    }

    [TestMethod]
    [DoNotParallelize]
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

    [TestMethod]
    public void ReportValidationFailed_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportValidationFailed();
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportValidationDiagnostic_Error_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "Something went wrong"),
            isError: true);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportValidationDiagnostic_Warning_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "A warning"),
            isError: false);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportValidationStatistics_Does_Not_Throw()
    {
        var result = new OpenApiValidationResult(
            new Microsoft.OpenApi.Reader.OpenApiDiagnostic(),
            new OpenApiStats());

        var act = () => new RichGenerationReporter().ReportValidationStatistics(result);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSuccess_SingleFile_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: false);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSuccess_MultipleFiles_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: true);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportDonationBanner_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportDonationBanner();
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportConfigurationWarnings_Does_Not_Throw()
    {
        var warnings = new List<Warning>
        {
            new("Title1", "Desc1"),
            new("Title2", "Desc2")
        };

        var act = () => new RichGenerationReporter().ReportConfigurationWarnings(warnings);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportAllPathsFilteredWarning_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter()
            .ReportAllPathsFilteredWarning(["^/pets", "^/users"]);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSettingsFileGenerated_Does_Not_Throw()
    {
        var act = () =>
            new RichGenerationReporter().ReportSettingsFileGenerated("/tmp/petstore.refitter");
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSettingsFileGenerated_Handles_Path_Without_Directory()
    {
        var act = () =>
            new RichGenerationReporter().ReportSettingsFileGenerated(Path.GetPathRoot(Environment.CurrentDirectory)!);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportGenerationFailed_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportGenerationFailed();
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportUnsupportedVersion_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportUnsupportedVersion("1.0");
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportExceptionDetails_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter()
            .ReportExceptionDetails(new InvalidOperationException("boom"));
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSkipValidationSuggestion_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportSkipValidationSuggestion();
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSupportHelp_Does_Not_Throw()
    {
        var act = () => new RichGenerationReporter().ReportSupportHelp();
        act.Should().NotThrow();
    }
}
