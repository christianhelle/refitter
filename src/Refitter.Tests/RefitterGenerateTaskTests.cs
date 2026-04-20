using FluentAssertions;
using Refitter.MSBuild;

namespace Refitter.Tests;

public class RefitterGenerateTaskTests
{
    [Test]
    public void FilterFiles_Should_Require_Exact_File_Name_Match()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "petstore.refitter"),
            Path.Combine(projectRoot, "internal-petstore.refitter"),
            Path.Combine(projectRoot, "petstore.refitter.bak.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, "petstore.refitter", projectRoot);

        result.Should().ContainSingle().Which.Should().Be(Path.Combine(projectRoot, "petstore.refitter"));
    }

    [Test]
    public void FilterFiles_Should_Match_Project_Relative_Path()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "apis", "petstore.refitter"),
            Path.Combine(projectRoot, "samples", "petstore.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, @"apis\petstore.refitter", projectRoot);

        result.Should().ContainSingle().Which.Should().Be(Path.Combine(projectRoot, "apis", "petstore.refitter"));
    }

    [Test]
    public void ParseGeneratedFilePath_Should_Return_File_Path_From_Marker()
    {
        var generatedFile = Path.Combine("C:", "repo", "Generated", "Petstore.cs");

        var result = RefitterGenerateTask.ParseGeneratedFilePath($"{RefitterGenerateTask.GeneratedFileMarker}{generatedFile}");

        result.Should().Be(generatedFile);
    }

    [Test]
    public void ParseGeneratedFilePath_Should_Ignore_Non_Marker_Output()
    {
        var result = RefitterGenerateTask.ParseGeneratedFilePath("Generated Output");

        result.Should().BeNull();
    }
}
