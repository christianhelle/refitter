using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.SettingsFile;

[TestClass]
public class RefitterSettingsFileTests
{
    private static readonly string SettingsDirectory =
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "refitter-settings-file-tests"));

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "petstore.refitter");

    [TestMethod]
    public void LoadSettingsFile_Resolves_Relative_OpenApiPath_Against_Settings_File_Directory()
    {
        var settings = RefitterSettingsLoader.LoadSettingsFile(
            """{"openApiPath": "specs/openapi.json"}""",
            SettingsFilePath);

        settings.OpenApiPath.Should().Be(Path.Combine(SettingsDirectory, "specs", "openapi.json"));
    }

    [TestMethod]
    public void LoadSettingsFile_Resolves_Relative_OpenApiPaths_Against_Settings_File_Directory()
    {
        var settings = RefitterSettingsLoader.LoadSettingsFile(
            """{"openApiPaths": ["a.json", "https://example.com/b.json"]}""",
            SettingsFilePath);

        settings.OpenApiPaths.Should().Equal(Path.Combine(SettingsDirectory, "a.json"), "https://example.com/b.json");
    }

    [TestMethod]
    public void LoadSettingsFile_Names_Output_After_Settings_File()
    {
        var settings = RefitterSettingsLoader.LoadSettingsFile("{}", SettingsFilePath);

        settings.OutputFilename.Should().Be("petstore.cs");
        settings.OutputFolder.Should().Be(RefitGeneratorSettings.DefaultOutputFolder);
    }

    [TestMethod]
    public void LoadSettingsFile_Generates_Multiple_Files_When_ContractsOutputFolder_Is_Set()
    {
        var settings = RefitterSettingsLoader.LoadSettingsFile(
            """{"contractsOutputFolder": "Contracts"}""",
            SettingsFilePath);

        settings.GenerateMultipleFiles.Should().BeTrue();
    }

    [TestMethod]
    public void LoadSettingsFile_Throws_On_Invalid_Json()
    {
        var act = () => RefitterSettingsLoader.LoadSettingsFile("{ not valid json", SettingsFilePath);

        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [TestMethod]
    [DataRow("petstore.refitter", "")]
    [DataRow("/petstore.refitter", "/")]
    [DataRow("/work/api/petstore.refitter", "/work/api")]
    [DataRow(@"C:\petstore.refitter", @"C:\")]
    [DataRow(@"C:\work\api\petstore.refitter", @"C:\work\api")]
    [DataRow(@"work\petstore.refitter", "work")]
    public void GetSettingsDirectory_Handles_Both_Separators(string settingsFilePath, string expected)
    {
        RefitterSettingsLoader.GetSettingsDirectory(settingsFilePath).Should().Be(expected);
    }
}
