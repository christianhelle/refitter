using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.SettingsFile;


public class RefitterSettingsLoaderTests
{
    private static readonly string BaseDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "refitter-loader-tests"));

    [Test]
    public void Load_Deserializes_Settings()
    {
        const string json = """{"namespace": "Acme.Api", "openApiPath": "https://example.com/openapi.json"}""";

        var settings = RefitterSettingsLoader.Load(json, BaseDirectory);

        settings.Namespace.Should().Be("Acme.Api");
    }

    [Test]
    public void Load_Throws_On_Invalid_Json()
    {
        var act = () => RefitterSettingsLoader.Load("{ not valid json", BaseDirectory);

        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Test]
    public void Resolves_Relative_OpenApiPath_Against_BaseDirectory()
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = "specs/openapi.json" };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPath.Should().Be(Path.GetFullPath(Path.Combine(BaseDirectory, "specs/openapi.json")));
    }

    [Test]
    public void Leaves_Url_OpenApiPath_Untouched()
    {
        const string url = "https://example.com/openapi.json";
        var settings = new RefitGeneratorSettings { OpenApiPath = url };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPath.Should().Be(url);
    }

    [Test]
    public void Leaves_Rooted_OpenApiPath_Untouched()
    {
        var rooted = Path.GetFullPath(Path.Combine(BaseDirectory, "absolute", "openapi.json"));
        var settings = new RefitGeneratorSettings { OpenApiPath = rooted };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPath.Should().Be(rooted);
    }

    [Test]
    public void Resolves_Relative_Entries_In_OpenApiPaths_And_Preserves_Urls()
    {
        const string url = "https://example.com/openapi.json";
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = new[] { "specs/a.json", url }
        };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPaths![0].Should().Be(Path.GetFullPath(Path.Combine(BaseDirectory, "specs/a.json")));
        settings.OpenApiPaths![1].Should().Be(url);
    }

    [Test]
    public void Leaves_Rooted_Entry_In_OpenApiPaths_Untouched()
    {
        var rootedPath = Path.GetFullPath(Path.Combine(BaseDirectory, "absolute", "spec.json"));
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = new[] { rootedPath }
        };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPaths![0].Should().Be(rootedPath);
    }

    [Test]
    public void Does_Not_Modify_OpenApiPath_When_Null_Or_Empty()
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = null };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPath.Should().BeNull();
    }

    [Test]
    public void Does_Not_Modify_OpenApiPaths_When_Empty_Array()
    {
        var settings = new RefitGeneratorSettings { OpenApiPaths = Array.Empty<string>() };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPaths.Should().BeEmpty();
    }

    [Test]
    [Arguments("https://example.com/openapi.json", true)]
    [Arguments("http://example.com/openapi.json", true)]
    [Arguments("specs/openapi.json", false)]
    [Arguments("ftp://example.com/openapi.json", false)]
    public void IsUrl_Detects_Http_And_Https(string path, bool expected)
    {
        RefitterSettingsLoader.IsUrl(path).Should().Be(expected);
    }

    [Test]
    public void ApplyDefaults_Should_Enable_GenerateMultipleFiles_When_ContractsOutputFolder_Is_Set()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            ContractsOutputFolder = "./Contracts"
        };

        RefitterSettingsLoader.ApplyDefaults(settingsFilePath, refitSettings);

        refitSettings.GenerateMultipleFiles.Should().BeTrue();
        refitSettings.OutputFolder.Should().Be(RefitGeneratorSettings.DefaultOutputFolder);
    }

    [Test]
    public void ApplyDefaults_Should_Fallback_To_Output_When_SettingsFilePath_Has_No_Filename()
    {
        var refitSettings = new RefitGeneratorSettings();

        RefitterSettingsLoader.ApplyDefaults(null!, refitSettings);
        refitSettings.OutputFilename.Should().Be("Output.cs");

        refitSettings.OutputFilename = null!;
        RefitterSettingsLoader.ApplyDefaults(string.Empty, refitSettings);
        refitSettings.OutputFilename.Should().Be("Output.cs");
    }

    [Test]
    public void ApplyDefaults_Should_Fallback_To_Output_When_OutputFilename_Is_Whitespace()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFilename = " "
        };

        RefitterSettingsLoader.ApplyDefaults(settingsFilePath, refitSettings);

        refitSettings.OutputFilename.Should().Be("petstore.cs");
    }

    [Test]
    public void ApplyDefaults_Should_Preserve_Existing_OutputFilename()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFilename = "CustomClient.cs"
        };

        RefitterSettingsLoader.ApplyDefaults(settingsFilePath, refitSettings);

        refitSettings.OutputFilename.Should().Be("CustomClient.cs");
    }

    [Test]
    public void ApplyDefaults_Should_Not_Set_GenerateMultipleFiles_When_No_ContractsFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings();

        RefitterSettingsLoader.ApplyDefaults(settingsFilePath, refitSettings);

        refitSettings.GenerateMultipleFiles.Should().BeFalse();
    }

    [Test]
    public void ResolveRelativeSpecPaths_Null_OpenApiPaths_Does_Not_Throw()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = null,
            OpenApiPaths = null
        };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPath.Should().BeNull();
        settings.OpenApiPaths.Should().BeNull();
    }

    [Test]
    public void ResolveRelativeSpecPaths_Empty_OpenApiPath_Does_Not_Resolve()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = string.Empty
        };

        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, BaseDirectory);

        settings.OpenApiPath.Should().Be(string.Empty);
    }
}
