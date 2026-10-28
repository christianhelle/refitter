using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class RefitterRunnerOpenApiPathsTests
{
    [TestMethod]
    public void GetOpenApiPaths_With_OpenApiPaths_Returns_Them()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "ignored.json",
            OpenApiPaths = ["first.json", "second.json"],
        };

        RefitterRunner.GetOpenApiPaths(settings)
            .Should().Equal("first.json", "second.json");
    }

    [TestMethod]
    public void GetOpenApiPaths_With_Single_OpenApiPath_Returns_It()
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = "only.json" };

        RefitterRunner.GetOpenApiPaths(settings)
            .Should().Equal("only.json");
    }

    [TestMethod]
    public void GetOpenApiPaths_With_Empty_OpenApiPaths_Falls_Back_To_OpenApiPath()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "only.json",
            OpenApiPaths = [],
        };

        RefitterRunner.GetOpenApiPaths(settings)
            .Should().Equal("only.json");
    }

    [TestMethod]
    public void GetOpenApiPaths_Without_Any_Path_Returns_Empty()
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = null };

        RefitterRunner.GetOpenApiPaths(settings)
            .Should().BeEmpty();
    }
}
