using AwesomeAssertions;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter.Tests;

/// <summary>
/// Smoke tests for <see cref="SilentGenerationReporter"/>. These exercise every method
/// for line/branch coverage; nothing is written to the console because the reporter
/// is a no-op. The delegate-wrapping methods must still invoke their delegates.
/// </summary>

[TestClass]
public class SilentGenerationReporterTests
{
    [TestMethod]
    public void ReportHeader_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportHeader("1.2.3");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSupportKey_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportSupportKey("TEST-KEY-123");

        act.Should().NotThrow();
    }

    [TestMethod]
    public async Task ReportSingleFileGenerationProgressAsync_Does_Not_Throw()
    {
        await new SilentGenerationReporter().ReportSingleFileGenerationProgressAsync();
    }

    [TestMethod]
    public void ReportSingleFileOutput_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSingleFileOutput("Output.cs", "/tmp", "12 KB", 300);

        act.Should().NotThrow();
    }

    [TestMethod]
    public async Task GenerateMultipleFilesWithProgressAsync_Invokes_Generator_And_Returns_Result()
    {
        bool called = false;
        GeneratorOutput result = await new SilentGenerationReporter()
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
        Action act = () =>
        {
            IMultiFileOutputReport report = new SilentGenerationReporter().BeginMultiFileOutput();
            report.AddFile("Api.cs", "/tmp", "5 KB", 100);
            report.Complete(1, "5 KB", 100);
        };

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportFileWritten_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportFileWritten("/output/Api.cs");

        act.Should().NotThrow();
    }

    [TestMethod]
    public async Task ValidateWithProgressAsync_Invokes_Validator()
    {
        bool called = false;
        await new SilentGenerationReporter().ValidateWithProgressAsync(async () =>
        {
            called = true;
            return await Task.FromResult(new OpenApiValidationResult(new Microsoft.OpenApi.Reader.OpenApiDiagnostic(), new OpenApiStats()));
        });

        called.Should().BeTrue();
    }

    [TestMethod]
    public void ReportValidationFailed_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportValidationFailed();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportValidationDiagnostic_Error_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "Something went wrong"),
            isError: true);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportValidationDiagnostic_Warning_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "A warning"),
            isError: false);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportValidationStatistics_Does_Not_Throw()
    {
        OpenApiValidationResult result = new OpenApiValidationResult(
            new Microsoft.OpenApi.Reader.OpenApiDiagnostic(),
            new OpenApiStats());

        Action act = () => new SilentGenerationReporter().ReportValidationStatistics(result);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSuccess_SingleFile_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: false);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSuccess_MultipleFiles_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: true);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportDonationBanner_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportDonationBanner();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportConfigurationWarnings_Does_Not_Throw()
    {
        List<Warning> warnings = new List<Warning>
        {
            new("Title1", "Desc1"),
            new("Title2", "Desc2"),
        };

        Action act = () => new SilentGenerationReporter().ReportConfigurationWarnings(warnings);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportAllPathsFilteredWarning_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter()
            .ReportAllPathsFilteredWarning(["^/pets", "^/users"]);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSettingsFileGenerated_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSettingsFileGenerated("/tmp/petstore.refitter");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportGenerationFailed_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportGenerationFailed();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportUnsupportedVersion_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportUnsupportedVersion("1.0");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportExceptionDetails_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter()
            .ReportExceptionDetails(new InvalidOperationException("boom"));

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSkipValidationSuggestion_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportSkipValidationSuggestion();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void ReportSupportHelp_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportSupportHelp();

        act.Should().NotThrow();
    }
}
