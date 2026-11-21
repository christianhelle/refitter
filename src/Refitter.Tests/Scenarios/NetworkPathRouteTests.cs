using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-7hgj-q592-v892: a path key such as a double-slash prefix is emitted
/// as a Refit route and resolved as a network-path reference, sending requests (and the
/// Authorization header) to a different host.
/// </summary>
[TestClass]
public class NetworkPathRouteTests
{
    private static string BuildSpec(string path) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{"
        + System.Text.Json.JsonSerializer.Serialize(path)
        + ":{\"get\":{\"operationId\":\"probe\",\"responses\":{\"200\":{\"description\":\"ok\"}}}}}}";

    [TestMethod]
    [DataRow("//127.0.0.1:1/collect")]
    [DataRow("/\\127.0.0.1:1/collect")]
    [DataRow("///x")]
    [DataRow("127.0.0.1:1/collect")]
    public async Task Rejects_Network_Path_Routes(string path)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(BuildSpec(path));

        Func<Task<string>> act = () => GenerateCode(swaggerFile);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    [DataRow("/probe")]
    [DataRow("/a//b")]
    [DataRow("/")]
    public async Task Allows_Relative_Routes(string path)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(BuildSpec(path));

        string code = await GenerateCode(swaggerFile);

        code.Should().Contain($"[Get(\"{path}\")]");
    }

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        RefitGeneratorSettings settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
