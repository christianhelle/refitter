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
}
