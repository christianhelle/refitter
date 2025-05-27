using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests;

public class NetFxCompatibilityTests
{
    private const string OpenApiSpec =
        @"
openapi: '3.0.1'
paths:
  '/pets/{id}':
    get:
      operationId: getPetById
      parameters:
        - name: id
          in: path
          required: true
          format: int64
          type: integer
      responses:
        '200':
          description: Successful operation
";

    [Fact]
    public async Task Can_Build_Generated_Code_With_Net80()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
        BuildHelper.BuildCSharpWithNet80(generatedCode).Should().BeTrue();
    }

    [Fact]
    public async Task Can_Build_Generated_Code_With_Net90()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
        BuildHelper.BuildCSharpWithNet90(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yaml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}