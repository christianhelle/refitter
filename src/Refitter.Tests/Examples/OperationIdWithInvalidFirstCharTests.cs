using FluentAssertions;
using Refitter.Tests.TestUtilities;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class OperationIdWithInvalidFirstCharTests
{

    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /jobs/{job-id}:
    get:
      tags:
      - 'Jobs'
      operationId: '2fa'
      description: '2 factr auth'
      parameters:
        - in: 'path'
          name: 'job-id'
          description: 'Job ID'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Adds_Underscore_At_Beginning_With_Ivalid_Methode_Name()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("_2fa");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
