using FluentAssertions;
using Refitter.Core;
using Spectre.Console.Cli;
using TUnit.Core;

namespace Refitter.Tests;

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
}
