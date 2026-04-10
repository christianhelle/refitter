using System.Reflection;
using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class GenerateCommandTests
{
    [Test]
    public void Validate_Should_Call_SettingsValidator()
    {
        var command = new GenerateCommand();
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            NoLogging = true
        };

        // We cannot easily test Validate() method without a real CommandContext
        // which is sealed. Instead, we'll test SettingsValidator directly
        // which is what Validate() calls internally.
        var validationResult = SettingsValidator.Validate(settings);

        validationResult.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Work_With_Valid_URL()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Fail_When_Both_OpenApiPath_And_SettingsFilePath_Are_Empty()
    {
        var settings = new Settings
        {
            OpenApiPath = null,
            SettingsFilePath = null,
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
    }

    [Test]
    public void Validate_Should_Fail_When_File_Does_Not_Exist()
    {
        var settings = new Settings
        {
            OpenApiPath = "nonexistent.json",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("File not found");
    }

    [Test]
    public void Validate_Should_Fail_For_Invalid_OperationNameTemplate()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = "InvalidTemplate",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("{operationName}");
    }

    [Test]
    public void Validate_Should_Succeed_For_Valid_OperationNameTemplate()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = "{operationName}Async",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void CreateRefitGeneratorSettings_Should_Map_PropertyNamingPolicy()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            PropertyNamingPolicy = PropertyNamingPolicy.PreserveOriginal
        };

        var method = typeof(GenerateCommand).GetMethod(
            "CreateRefitGeneratorSettings",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var refitSettings = method!
            .Invoke(null, [settings])
            .Should()
            .BeOfType<RefitGeneratorSettings>()
            .Subject;

        refitSettings.PropertyNamingPolicy.Should().Be(PropertyNamingPolicy.PreserveOriginal);
    }

    [Test]
    public void Command_Should_Have_Protected_Validate_Method()
    {
        var command = new GenerateCommand();
        var method = command.GetType().GetMethod(
            "Validate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.IsFamily.Should().BeTrue();
    }

    [Test]
    public void Command_Should_Have_Protected_ExecuteAsync_Method()
    {
        var command = new GenerateCommand();
        var method = command.GetType().GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.IsFamily.Should().BeTrue();
    }
}
