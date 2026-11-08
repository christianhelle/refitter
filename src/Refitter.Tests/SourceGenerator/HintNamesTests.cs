using AwesomeAssertions;
using Refitter.SourceGenerator;

namespace Refitter.Tests.SourceGenerator;

[TestClass]
public class HintNamesTests
{
    [TestMethod]
    [DataRow("petstore.refitter", "Generated/petstore.cs", "petstore.g.cs")]
    [DataRow("petstore.refitter", "Generated/petstore.g.cs", "petstore.g.cs")]
    [DataRow("petstore.refitter", "Generated/Contracts.cs", "petstore.Contracts.g.cs")]
    [DataRow("my api.refitter", "Generated/my api.cs", "my_api.g.cs")]
    [DataRow("api.g.refitter", "Generated/api.g.cs", "api.g.g.cs")]
    public void Create_Starts_With_Settings_File_Name(string settingsFile, string plannedFile, string expected)
    {
        HintNames.Create(settingsFile, plannedFile, new HashSet<string>())
            .Should()
            .Be(expected);
    }

    [TestMethod]
    public void Create_Makes_Names_Unique()
    {
        var used = new HashSet<string>();

        HintNames.Create("a/petstore.refitter", "petstore.cs", used).Should().Be("petstore.g.cs");
        HintNames.Create("b/petstore.refitter", "petstore.cs", used).Should().Be("petstore2.g.cs");
    }
}
