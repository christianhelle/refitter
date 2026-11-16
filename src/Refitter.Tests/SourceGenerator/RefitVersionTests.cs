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
    public void Detects_Reflection_Based_Registration_Package()
    {
        RefitVersion
            .ReferencesReflectionRegistration(
            [
                ("Refit", new Version(17, 0, 0, 0)),
                ("Refit.HttpClientFactory.Reflection", new Version(17, 0, 0, 0)),
            ])
            .Should()
            .BeTrue();
    }

    [TestMethod]
    public void Does_Not_Detect_Reflection_Based_Registration_Without_The_Package()
    {
        RefitVersion
            .ReferencesReflectionRegistration(
            [
                ("Refit.HttpClientFactory", new Version(17, 0, 0, 0)),
                ("Refit.Reflection", new Version(17, 0, 0, 0)),
            ])
            .Should()
            .BeFalse();
    }

    [TestMethod]
    [DataRow(17)]
    [DataRow(18)]
    public void Forces_Generated_Refit_Client_For_Refit_17_Or_Newer(int majorVersion)
    {
        var settings = CreateSettings(generateJsonSerializerContext: true);

        Apply(settings, majorVersion).Should().BeEmpty();

        settings.DependencyInjectionSettings!.UseGeneratedRefitClient.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(16)]
    [DataRow(null)]
    public void Keeps_Configured_Registration_For_Older_Or_Unknown_Refit(int? majorVersion)
    {
        var settings = CreateSettings(useMultipartFormObject: true);

        Apply(settings, majorVersion, refitGeneratorRuns: false).Should().BeEmpty();

        settings.DependencyInjectionSettings!.UseGeneratedRefitClient.Should().BeFalse();
    }

    [TestMethod]
    public void Keeps_Configured_Registration_When_Reflection_Based_Registration_Is_Referenced()
    {
        var settings = CreateSettings(useMultipartFormObject: true);

        Apply(settings, 17, reflectionRegistration: true, refitGeneratorRuns: false).Should().BeEmpty();

        settings.DependencyInjectionSettings!.UseGeneratedRefitClient.Should().BeFalse();
    }

    [TestMethod]
    public void Ignores_Settings_Without_Dependency_Injection()
    {
        var settings = new RefitGeneratorSettings { UseMultipartFormObject = true };

        Apply(settings, 17, refitGeneratorRuns: false).Should().BeEmpty();

        settings.DependencyInjectionSettings.Should().BeNull();
    }

    [TestMethod]
    public void Warns_When_Refit_Generator_Does_Not_Run_For_Refit_17()
    {
        var settings = CreateSettings(generateJsonSerializerContext: true);

        Apply(settings, 17, refitGeneratorRuns: false)
            .Should()
            .Equal(RefitVersionWarning.RefitGeneratorNotRun);
    }

    [TestMethod]
    public void Warns_When_Multipart_Form_Object_Is_Used_With_Refit_17()
    {
        var settings = CreateSettings(useMultipartFormObject: true, generateJsonSerializerContext: true);

        Apply(settings, 17)
            .Should()
            .Equal(RefitVersionWarning.MultipartFormObject);
    }

    [TestMethod]
    public void Warns_When_No_JsonSerializerContext_Is_Generated_For_Refit_17()
    {
        var settings = CreateSettings();

        Apply(settings, 17)
            .Should()
            .Equal(RefitVersionWarning.NoJsonSerializerContext);
    }

    [TestMethod]
    public void Warns_When_Contracts_Are_Not_Generated_For_Refit_17()
    {
        var settings = CreateSettings(generateJsonSerializerContext: true);
        settings.GenerateContracts = false;

        Apply(settings, 17)
            .Should()
            .Equal(RefitVersionWarning.NoJsonSerializerContext);
    }

    [TestMethod]
    public void Checks_Explicitly_Generated_Clients_When_Reflection_Based_Registration_Is_Referenced()
    {
        var settings = CreateSettings();
        settings.DependencyInjectionSettings!.UseGeneratedRefitClient = true;

        Apply(settings, 17, reflectionRegistration: true, refitGeneratorRuns: false)
            .Should()
            .Equal(RefitVersionWarning.RefitGeneratorNotRun, RefitVersionWarning.NoJsonSerializerContext);
    }

    private static IReadOnlyList<RefitVersionWarning> Apply(
        RefitGeneratorSettings settings,
        int? majorVersion,
        bool reflectionRegistration = false,
        bool refitGeneratorRuns = true) =>
        RefitVersion.Apply(settings, majorVersion, reflectionRegistration, refitGeneratorRuns);

    private static RefitGeneratorSettings CreateSettings(
        bool useMultipartFormObject = false,
        bool generateJsonSerializerContext = false) =>
        new()
        {
            UseMultipartFormObject = useMultipartFormObject,
            GenerateJsonSerializerContext = generateJsonSerializerContext,
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };
}
