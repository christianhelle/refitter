using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class RouteTemplateDisambiguatorTests
{
    [Test]
    public void Returns_Path_And_Parameters_Unchanged_Without_Collisions()
    {
        string[] parameters = ["long id", "[Query] string name", "CancellationToken cancellationToken = default"];

        var (path, result) = RouteTemplateDisambiguator.Disambiguate("/items/{id}", parameters, ["id"]);

        path.Should().Be("/items/{id}");
        result.Should().Equal(parameters);
    }

    [Test]
    public void Renames_Placeholder_Colliding_With_Query_Alias_Differing_By_Case()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/api/parent/{id}/child",
            ["int id", "[Query, AliasAs(\"ID\")] int? iD"],
            ["id"]);

        path.Should().Be("/api/parent/{id2}/child");
        result.Should().Equal("[AliasAs(\"id2\")] int id", "[Query, AliasAs(\"ID\")] int? iD");
    }
}
