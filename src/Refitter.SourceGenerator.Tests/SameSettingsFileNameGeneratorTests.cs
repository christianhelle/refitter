using AwesomeAssertions;
using Refitter.Tests.AdditionalFiles.SameName;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class SameSettingsFileNameGeneratorTests
{
    [TestMethod]
    public void Should_Generate_Both_Settings_Files_With_The_Same_Name() =>
        typeof(ISwaggerPetstoreWithSameSettingsFileName)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.SameName");
}
