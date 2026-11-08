using AwesomeAssertions;
using Refitter.SourceGenerator;

namespace Refitter.Tests.SourceGenerator;

[TestClass]
public class LoadedGeneratorLocatorTests
{
    private static readonly Version Reference = new(10, 0, 0, 0);

    [TestMethod]
    public void Prefers_The_Generator_Matching_The_Referenced_Major_Version()
    {
        LoadedGeneratorLocator
            .OrderCandidates([new Version(9, 0, 0, 0), new Version(10, 0, 0, 0), new Version(42, 42, 42, 42)], Reference)
            .Should()
            .Equal(new Version(10, 0, 0, 0), new Version(42, 42, 42, 42));
    }

    [TestMethod]
    public void Accepts_A_Source_Built_Generator()
    {
        // Source-built .NET SDKs, like the ones Linux distributions ship, version their analyzers 42.42.42.42
        LoadedGeneratorLocator
            .OrderCandidates([new Version(42, 42, 42, 42)], Reference)
            .Should()
            .Equal(new Version(42, 42, 42, 42));
    }

    [TestMethod]
    public void Rejects_Generators_For_Other_Major_Versions()
    {
        LoadedGeneratorLocator
            .OrderCandidates([new Version(9, 0, 0, 0), new Version(11, 0, 0, 0)], Reference)
            .Should()
            .BeEmpty();
    }

    [TestMethod]
    public void Prefers_The_Newest_Matching_Generator()
    {
        LoadedGeneratorLocator
            .OrderCandidates([new Version(10, 0, 0, 0), new Version(10, 0, 1, 0)], Reference)
            .Should()
            .Equal(new Version(10, 0, 1, 0), new Version(10, 0, 0, 0));
    }
}
