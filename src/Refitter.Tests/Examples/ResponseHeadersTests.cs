using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class ResponseHeadersTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
paths:
  /foo:
    post:
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
        var generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Not_Generate_ContentType_Header_Attribute()
    {
        var generateCode = await GenerateCode();
        using var scope = new AssertionScope();
        generateCode.Should().NotContain("Content-Type: \")]");
    }

    [Fact]
    public async Task Generates_Accept_Header_Attribute()
    {
        var generateCode = await GenerateCode();
        using var scope = new AssertionScope();
        generateCode.Should().Contain("Accept: application/json");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        var generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile, AddAcceptHeaders = true };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
