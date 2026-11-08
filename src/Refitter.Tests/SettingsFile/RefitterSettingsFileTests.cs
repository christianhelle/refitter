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
    public void LoadSettingsFile_Throws_When_The_File_Contains_Null()
    {
        var act = () => RefitterSettingsLoader.LoadSettingsFile("null", SettingsFilePath);

        act.Should().Throw<System.Text.Json.JsonException>().WithMessage("*does not contain any settings*");
    }

    [TestMethod]
    public void LoadSettingsFile_Throws_On_Invalid_Json()
    {
        var act = () => RefitterSettingsLoader.LoadSettingsFile("{ not valid json", SettingsFilePath);

        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [TestMethod]
    public void GetSettingsDirectory_Returns_The_Directory_Of_The_Settings_File()
    {
        var directory = Path.Combine(SettingsDirectory, "api");

        RefitterSettingsLoader.GetSettingsDirectory(Path.Combine(directory, "petstore.refitter")).Should().Be(directory);
        RefitterSettingsLoader.GetSettingsDirectory("petstore.refitter").Should().BeEmpty();
    }

    [TestMethod]
    public void GetSettingsDirectory_Returns_Empty_For_A_Root_Path()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;

        RefitterSettingsLoader.GetSettingsDirectory(root).Should().BeEmpty();
    }

    [TestMethod]
    public void GetSettingsDirectory_Treats_Backslash_As_A_File_Name_Character_Off_Windows()
    {
        if (OperatingSystem.IsWindows())
            return;

        RefitterSettingsLoader.GetSettingsDirectory(@"configs\petstore.refitter").Should().BeEmpty();
        RefitterSettingsLoader
            .LoadSettingsFile("{}", Path.Combine(SettingsDirectory, @"configs\petstore.refitter"))
            .OutputFilename
            .Should()
            .Be(@"configs\petstore.cs");
    }
}
