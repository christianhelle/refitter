using System.Text.Json;
using AwesomeAssertions;

namespace Refitter.Tests;

/// <summary>
/// Guards the dependency graph against packages Refitter no longer depends on.
/// See docs/oasreader-removal-plan.md.
/// </summary>
public class DependencyGraphTests
{
    [Test]
    [Arguments("OasReader")]
    [Arguments("Microsoft.OpenApi")]
    public void Package_Is_Not_A_Dependency(string packagePrefix)
    {
        var depsFile = Path.Combine(AppContext.BaseDirectory, "Refitter.Tests.deps.json");
        using var deps = JsonDocument.Parse(File.ReadAllText(depsFile));

        var packages = deps.RootElement
            .GetProperty("libraries")
            .EnumerateObject()
            .Select(library => library.Name)
            .Where(name => name.StartsWith(packagePrefix + "/", StringComparison.OrdinalIgnoreCase)
                           || name.StartsWith(packagePrefix + ".", StringComparison.OrdinalIgnoreCase))
            .ToList();

        packages.Should().BeEmpty();
    }
}
