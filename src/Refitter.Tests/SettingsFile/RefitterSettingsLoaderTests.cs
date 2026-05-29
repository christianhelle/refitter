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
    [Arguments("https://example.com/openapi.json", true)]
    [Arguments("http://example.com/openapi.json", true)]
    [Arguments("specs/openapi.json", false)]
    [Arguments("ftp://example.com/openapi.json", false)]
    public void IsUrl_Detects_Http_And_Https(string path, bool expected)
    {
        RefitterSettingsLoader.IsUrl(path).Should().Be(expected);
    }
}
