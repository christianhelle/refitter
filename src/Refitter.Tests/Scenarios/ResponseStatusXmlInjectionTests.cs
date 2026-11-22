using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression tests for GHSA-r2hq-86ch-hf3x: a response status key from the OpenAPI document must be
/// XML-escaped in the generated status code comments, so it cannot add elements such as include.
/// </summary>
[TestClass]
public class ResponseStatusXmlInjectionTests
{
    private const string Payload =
        "</term><description><include file='secret.xml' path='/doc/members/member/summary' /></description><term>";

    private static string CreateSpec(string successKey, string errorKey) =>
        $$"""
        {
          "openapi": "3.0.1",
          "info": { "title": "ResponseStatusProbe", "version": "1.0" },
          "paths": {
            "/probe": {
              "get": {
                "operationId": "getProbe",
                "responses": {
                  "{{successKey}}": {
                    "description": "ok",
                    "content": { "application/json": { "schema": { "type": "string" } } }
                  },
                  "{{errorKey}}": { "description": "bad" }
                }
              }
            }
          }
        }
        """;

    private static async Task<string> GenerateAsync(string spec, bool returnApiResponse)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = returnApiResponse,
            GenerateStatusCodeComments = true,
        };
        var generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Response_Status_Key_Markup_Is_Escaped(bool returnApiResponse)
    {
        string code = await GenerateAsync(
            CreateSpec($"200{Payload}200", $"400{Payload}400"),
            returnApiResponse);

        code.Should().NotContain("<include");
        code.Should().Contain("&lt;include");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Numeric_Status_Key_Is_Documented_Unchanged(bool returnApiResponse)
    {
        string code = await GenerateAsync(CreateSpec("200", "400"), returnApiResponse);

        code.Should().Contain("<term>400</term>");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Code_With_Escaped_Status_Key_Builds()
    {
        string code = await GenerateAsync(
            CreateSpec($"200{Payload}200", $"400{Payload}400"),
            returnApiResponse: false);

        BuildHelper
            .BuildCSharp(code)
            .Should()
            .BeTrue();
    }
}
