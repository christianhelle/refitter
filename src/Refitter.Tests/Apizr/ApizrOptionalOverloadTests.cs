using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Apizr;

[TestClass]
public class ApizrOptionalOverloadTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'Items'
  version: 'v1'
paths:
  /items:
    get:
      operationId: 'GetItems'
      parameters:
        - name: 'ids'
          in: 'query'
          required: true
          schema:
            type: 'array'
            items:
              type: 'integer'
              format: 'int32'
              nullable: true
        - name: 'name'
          in: 'query'
          required: false
          schema:
            type: 'string'
      responses:
        '200':
          description: 'OK'
";

    [TestMethod]
    public async Task Overload_Without_Optional_Parameters_Keeps_Collections_Of_Nullable_Items()
    {
        var generatedCode = await GenerateCode();

        var signatures = generatedCode
            .Split('\n')
            .Where(line => line.Contains("Task GetItems("))
            .ToList();
        signatures.Should().HaveCount(2, generatedCode);
        signatures.Should().OnlyContain(signature => signature.Contains("IEnumerable<int?> ids"));
        signatures.Should().ContainSingle(signature => signature.Contains("string? name"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();

        BuildHelper
            .BuildApizrCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OptionalParameters = true,
            ApizrSettings = new ApizrSettings(),
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
