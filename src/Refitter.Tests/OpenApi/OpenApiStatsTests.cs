using AwesomeAssertions;
using Refitter.Core.Validation;

namespace Refitter.Tests.OpenApi;


public class OpenApiStatsTests
{
    [Test]
    public void Should_Initialize_With_Zero_Counts()
    {
        var stats = new OpenApiStats();

        stats.ParameterCount.Should().Be(0);
        stats.SchemaCount.Should().Be(0);
        stats.HeaderCount.Should().Be(0);
        stats.PathItemCount.Should().Be(0);
        stats.RequestBodyCount.Should().Be(0);
        stats.ResponseCount.Should().Be(0);
        stats.OperationCount.Should().Be(0);
        stats.LinkCount.Should().Be(0);
        stats.CallbackCount.Should().Be(0);
    }

    [Test]
    public void ToString_Should_Return_Formatted_Statistics()
    {
        var stats = new OpenApiStats
        {
            PathItemCount = 1,
            OperationCount = 2,
            ParameterCount = 3,
            RequestBodyCount = 4,
            ResponseCount = 5,
            LinkCount = 6,
            CallbackCount = 7,
            SchemaCount = 8,
            HeaderCount = 9,
        };

        stats.ToString().Replace("\r\n", "\n").Should().Be(
            " - Path Items: 1\n" +
            " - Operations: 2\n" +
            " - Parameters: 3\n" +
            " - Request Bodies: 4\n" +
            " - Responses: 5\n" +
            " - Links: 6\n" +
            " - Callbacks: 7\n" +
            " - Schemas: 8");
    }
}
