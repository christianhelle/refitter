using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class SymbolsInDescriptionTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Reference parameters
  version: v0.0.1
paths:
  '/symbols':
    get:
      description: >-
        Something something <test> >> << </test>
      responses:
        '204':
          description: No Content.
        default:
          description: Default response
          schema:
            $ref: '#/definitions/Error'
definitions:
  Error:
    type: object
    properties:
      message:
          type: string
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Symbols_In_Description_Are_Escaped()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("&lt;test&gt; &gt;&gt; &lt;&lt; &lt;/test&gt;");
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
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
