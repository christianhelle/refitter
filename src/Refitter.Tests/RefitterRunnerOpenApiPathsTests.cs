using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitterRunnerOpenApiPathsTests
{
    [Test]
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

    [Test]
    public void GetOpenApiPaths_With_Single_OpenApiPath_Returns_It()
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = "only.json" };

        RefitterRunner.GetOpenApiPaths(settings)
            .Should().Equal("only.json");
    }

    [Test]
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

    [Test]
    public void GetOpenApiPaths_Without_Any_Path_Returns_Empty()
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = null };

        RefitterRunner.GetOpenApiPaths(settings)
            .Should().BeEmpty();
    }
}
