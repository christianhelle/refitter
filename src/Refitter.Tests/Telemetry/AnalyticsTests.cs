using System.Reflection;
using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Telemetry;

public class AnalyticsTests
{
    [Test]
    public void Configure_Should_Not_Throw()
    {
        var action = () => Analytics.Configure();

        action.Should().NotThrow();
    }

    [Test]
    public void LogFeatureUsage_Should_Skip_When_NoLogging_Is_True()
    {
        var settings = new Settings
        {
            NoLogging = true,
            ReturnIApiResponse = true
        };
        var refitSettings = new RefitGeneratorSettings();

        var action = () => Analytics.LogFeatureUsage(settings, refitSettings);

        action.Should().NotThrow();
    }

    [Test]
    public async Task LogError_Should_Skip_When_NoLogging_Is_True()
    {
        var settings = new Settings
        {
            NoLogging = true,
            OpenApiPath = "test.json"
        };
        var exception = new Exception("Test exception");

        var action = async () => await Analytics.LogError(exception, settings);

        await action.Should().NotThrowAsync();
    }

    [Test]
    public void LogFeatureUsage_Should_Handle_Null_Properties()
    {
        var settings = new Settings
        {
            NoLogging = true,
            OpenApiPath = null,
            SettingsFilePath = null,
            ContractsNamespace = null,
            OperationNameTemplate = null
        };
        var refitSettings = new RefitGeneratorSettings();

        var action = () => Analytics.LogFeatureUsage(settings, refitSettings);

        action.Should().NotThrow();
    }

    [Test]
    public void Telemetry_Properties_Should_Redact_Paths_Urls_And_Metadata()
    {
        var settings = new Settings
        {
            SettingsFilePath = @"C:\secret\api.refitter",
            OpenApiPath = "https://internal.example.test/openapi.json",
            OutputPath = @"C:\secret\Generated.cs",
            ContractsOutputPath = @"C:\secret\Contracts",
            ContractsNamespace = "Internal.Contracts",
            CustomTemplateDirectory = @"C:\secret\templates",
            SecurityScheme = "InternalBearer",
            Tags = ["private-tag"],
            MatchPaths = ["/private"]
        };
        var refitSettings = new RefitGeneratorSettings
        {
            OpenApiPath = settings.OpenApiPath,
            OpenApiPaths = [settings.OpenApiPath],
            OutputFolder = @"C:\secret\out",
            OutputFilename = "Generated.cs",
            ContractsOutputFolder = @"C:\secret\Contracts",
            ContractsNamespace = settings.ContractsNamespace,
            CustomTemplateDirectory = settings.CustomTemplateDirectory,
            SecurityScheme = settings.SecurityScheme,
            IncludePathMatches = settings.MatchPaths,
            IncludeTags = settings.Tags
        };

        var properties = GetTelemetryProperties(settings, refitSettings);
        var serialized = string.Join(Environment.NewLine, properties.Select(pair => $"{pair.Key}={pair.Value}"));

        serialized.Should().NotContain("internal.example.test");
        serialized.Should().NotContain("secret");
        serialized.Should().NotContain("Internal.Contracts");
        serialized.Should().NotContain("InternalBearer");
        serialized.Should().NotContain("private-tag");
        serialized.Should().NotContain("/private");
        properties.Should().ContainKey("usedSettingsFile");
        properties.Should().ContainKey("openApiPathsCount");
    }

    private static Dictionary<string, string> GetTelemetryProperties(
        Settings settings,
        RefitGeneratorSettings refitSettings)
    {
        var method = typeof(Analytics).GetMethod(
            "GetTelemetryProperties",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        return (Dictionary<string, string>)method!.Invoke(null, [settings, refitSettings])!;
    }
}
