using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class OperationIdWithSpacesTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /jobs/{job-id}:
    get:
      tags:
      - 'Jobs'
      operationId: 'Get job details'
      description: 'Get the details of the specified job.'
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
    public async Task Replaces_KababCase_Parameters_With_PascalCase()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("string job_id");
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
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
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
