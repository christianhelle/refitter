using System.Text.Json.Serialization;
using AwesomeAssertions;
using Refitter.Tests.AdditionalFiles.JsonSerializerContext;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class JsonSerializerContextGeneratorTests
{
    [TestMethod]
    public void Should_Generate_Implemented_JsonSerializerContext()
    {
        var contextType = typeof(Pet).Assembly
            .GetTypes()
            .Single(t => t.Namespace == typeof(Pet).Namespace && typeof(JsonSerializerContext).IsAssignableFrom(t));

        var context = (JsonSerializerContext)contextType.GetProperty("Default")!.GetValue(null)!;

        context.GetTypeInfo(typeof(Pet)).Should().NotBeNull();
    }
}
