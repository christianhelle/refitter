using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-4f49-pm8q-mx4f: a remote <c>$ref</c> hidden behind JSON or YAML
/// escape sequences must be blocked before any reference resolver can fetch it.
/// </summary>
[TestClass]
public class EscapedRemoteReferenceTests
{
    // Port 1 refuses connections, so a reference that slipped past the guard would surface as an
    // HTTP failure rather than a ReferenceResolutionException
    private const string EscapedJsonSpec =
        @"{""openapi"":""3.0.0"",""info"":{""title"":""t"",""version"":""1""},""paths"":{},""components"":{""schemas"":{""Pet"":{""\u0024ref"":""http:\/\/127.0.0.1:1\/schema.json""}}}}";

    private const string EscapedYamlSpec = @"openapi: 3.0.0
info:
  title: t
  version: '1'
paths: {}
components:
  schemas:
    Pet:
      ""\x24ref"": ""http:\x2F\x2F127.0.0.1:1\x2Fschema.json""
";

    [TestMethod]
    public async Task Blocks_Json_Escaped_Remote_Reference()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(EscapedJsonSpec);

        var act = () => GenerateCode(swaggerFile);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*http://127.0.0.1:1/schema.json*");
    }

    [TestMethod]
    public async Task Blocks_Yaml_Escaped_Remote_Reference()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(EscapedYamlSpec);

        var act = () => GenerateCode(swaggerFile);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*http://127.0.0.1:1/schema.json*");
    }

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
