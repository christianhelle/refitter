using FluentAssertions;
using Refitter.Tests.TestUtilities;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class RequestResponseHeadersTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
paths:
  /foo:
    post:
      requestBody:
        content:
          application/json:
            schema:
              type: string
        required: true
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Generates_ContentType_Header_Attribute()
    {
        var generatedCode = await GenerateCode();
        using var scope = new AssertionScope();
        generatedCode.Should().Contain("Content-Type: application/json");
    }

    [Fact]
    public async Task Generates_Accept_Header_Attribute()
    {
        var generatedCode = await GenerateCode();
        using var scope = new AssertionScope();
        generatedCode.Should().Contain("Accept: application/json");
    }

    [Fact]
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
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile, AddAcceptHeaders = true };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
