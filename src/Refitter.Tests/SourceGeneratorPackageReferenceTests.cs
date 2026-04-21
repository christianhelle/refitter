using System.Xml.Linq;
using FluentAssertions;

namespace Refitter.Tests;

public class SourceGeneratorPackageReferenceTests
{
    [Test]
    public void SourceGenerator_Project_Should_Hide_Generator_Only_Dependencies_From_Consumers()
    {
        var projectFile = Path.Combine(GetRepositoryRoot(), "src", "Refitter.SourceGenerator", "Refitter.SourceGenerator.csproj");
        var document = XDocument.Load(projectFile);

        var packageReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .ToDictionary(
                element => element.Attribute("Include")?.Value ?? string.Empty,
                element => element.Attribute("PrivateAssets")?.Value,
                StringComparer.OrdinalIgnoreCase);

        packageReferences["OasReader"].Should().Be("all");
        packageReferences["Refit"].Should().Be("all");
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "README.md")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("tests should run from within the repository workspace");
        return directory!.FullName;
    }
}
