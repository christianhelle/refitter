using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class MissingParameterDescriptionTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Test API
  version: v1.0.0
paths:
  '/users/{userId}':
    get:
      summary: Get user by ID
      operationId: GetUser
      parameters:
        - name: userId
          in: path
          required: true
          schema:
            type: string
        - name: includeDetails
          in: query
          schema:
            type: boolean
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/User'
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_Param_Tags_For_All_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("/// <param name=\"userId\">Parameter</param>");
        generatedCode.Should().Contain("/// <param name=\"includeDetails\">Parameter</param>");
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
