using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class ByTagWithoutOpenApiTitleTests
{
    private const string EmptyTitle = "  title: ''";
    private const string WhitespaceTitle = "  title: '   '";

    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: 'v1'
{0}
paths:
  /jobs:
    get:
      tags:
      - 'Jobs'
      operationId: 'GetJobs'
      responses:
        '200':
          description: 'successful operation'
  /health:
    get:
      operationId: 'GetHealth'
      responses:
        '200':
          description: 'successful operation'
";

    [TestMethod]
    [DataRow(EmptyTitle)]
    [DataRow(WhitespaceTitle)]
    public async Task Untagged_Operations_Use_InterfaceName_When_Title_Is_Blank(string titleLine)
    {
        string generatedCode = await GenerateCode(titleLine);
        generatedCode.Should().Contain("partial interface IMyClientApi");
    }

    [TestMethod]
    public async Task Tagged_Operations_Use_Tag()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IJobsApi");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(string titleLine = EmptyTitle)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(string.Format(OpenApiSpec, titleLine));
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag,
            Naming = new NamingSettings { InterfaceName = "MyClient" },
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
