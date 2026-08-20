using FluentAssertions;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter.Tests;

/// <summary>
/// Smoke tests for <see cref="SilentGenerationReporter"/>. These exercise every method
/// for line/branch coverage; nothing is written to the console because the reporter
/// is a no-op. The delegate-wrapping methods must still invoke their delegates.
/// </summary>

public class SilentGenerationReporterTests
{
    [Test]
    public void ReportHeader_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportHeader("1.2.3");

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSupportKey_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportSupportKey("TEST-KEY-123");

        act.Should().NotThrow();
    }

    [Test]
    public async Task ReportSingleFileGenerationProgressAsync_Does_Not_Throw()
    {
        await new SilentGenerationReporter().ReportSingleFileGenerationProgressAsync();
    }

    [Test]
    public void ReportSingleFileOutput_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSingleFileOutput("Output.cs", "/tmp", "12 KB", 300);

        act.Should().NotThrow();
    }

    [Test]
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

    [Test]
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

    [Test]
    public void ReportFileWritten_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportFileWritten("/output/Api.cs");

        act.Should().NotThrow();
    }

    [Test]
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

    [Test]
    public void ReportValidationFailed_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportValidationFailed();

        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationDiagnostic_Error_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "Something went wrong"),
            isError: true);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationDiagnostic_Warning_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportValidationDiagnostic(
            new OpenApiError("field", "A warning"),
            isError: false);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportValidationStatistics_Does_Not_Throw()
    {
        OpenApiValidationResult result = new OpenApiValidationResult(
            new Microsoft.OpenApi.Reader.OpenApiDiagnostic(),
            new OpenApiStats());

        Action act = () => new SilentGenerationReporter().ReportValidationStatistics(result);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSuccess_SingleFile_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: false);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSuccess_MultipleFiles_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSuccess(TimeSpan.FromMilliseconds(1234), multipleFiles: true);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportDonationBanner_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportDonationBanner();

        act.Should().NotThrow();
    }

    [Test]
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

    [Test]
    public void ReportAllPathsFilteredWarning_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter()
            .ReportAllPathsFilteredWarning(["^/pets", "^/users"]);

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSettingsFileGenerated_Does_Not_Throw()
    {
        Action act = () =>
            new SilentGenerationReporter().ReportSettingsFileGenerated("/tmp/petstore.refitter");

        act.Should().NotThrow();
    }

    [Test]
    public void ReportGenerationFailed_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportGenerationFailed();

        act.Should().NotThrow();
    }

    [Test]
    public void ReportUnsupportedVersion_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportUnsupportedVersion("1.0");

        act.Should().NotThrow();
    }

    [Test]
    public void ReportExceptionDetails_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter()
            .ReportExceptionDetails(new InvalidOperationException("boom"));

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSkipValidationSuggestion_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportSkipValidationSuggestion();

        act.Should().NotThrow();
    }

    [Test]
    public void ReportSupportHelp_Does_Not_Throw()
    {
        Action act = () => new SilentGenerationReporter().ReportSupportHelp();

        act.Should().NotThrow();
    }
}
