using AwesomeAssertions;
using Refitter.Core;
using Refitter.SourceGenerator;

namespace Refitter.Tests.SourceGenerator;

[TestClass]
public class RefitVersionTests
{
    [TestMethod]
    public void Finds_The_Referenced_Refit_Major_Version()
    {
        RefitVersion
            .FindMajorVersion(
            [
                ("Refit.HttpClientFactory", new Version(17, 0, 0, 0)),
                ("Refit", new Version(17, 0, 0, 0)),
                ("System.Runtime", new Version(10, 0, 0, 0)),
            ])
            .Should()
            .Be(17);
    }

    [TestMethod]
    public void Returns_Null_When_Refit_Is_Not_Referenced()
    {
        RefitVersion
            .FindMajorVersion([("System.Runtime", new Version(10, 0, 0, 0))])
            .Should()
            .BeNull();
    }

    [TestMethod]
    [DataRow(17)]
    [DataRow(18)]
    public void Forces_Generated_Refit_Client_For_Refit_17_Or_Newer(int majorVersion)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        RefitVersion.Apply(settings, majorVersion);

        settings.DependencyInjectionSettings.UseGeneratedRefitClient.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(16)]
    [DataRow(null)]
    public void Keeps_Configured_Registration_For_Older_Or_Unknown_Refit(int? majorVersion)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        RefitVersion.Apply(settings, majorVersion);

        settings.DependencyInjectionSettings.UseGeneratedRefitClient.Should().BeFalse();
    }

    [TestMethod]
    public void Ignores_Settings_Without_Dependency_Injection()
    {
        var settings = new RefitGeneratorSettings();

        RefitVersion.Apply(settings, 17);

        settings.DependencyInjectionSettings.Should().BeNull();
    }
}
