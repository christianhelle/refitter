using FluentAssertions;
using Refitter.Core;
using Refitter.SourceGenerators.Tests.Build;
using Refitter.SourceGenerators.Tests.TestUtilities;

namespace Refitter.SourceGenerators.Tests;


public class SourceGeneratorFileIOTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0.0'
paths:
  /users:
    get:
      operationId: 'GetUsers'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                type: array
                items:
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
    public async Task Test_CoreGenerator_DoesNotWriteToFileSystem()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = true,
        };

        var outputDir = Path.GetDirectoryName(swaggerFile)!;
        var possibleOutputFiles = Directory.GetFiles(outputDir, "*.g.cs");
        var initialFileCount = possibleOutputFiles.Length;

        var generator = await RefitGenerator.CreateAsync(settings);
        var generatedCode = generator.Generate();

        var finalOutputFiles = Directory.GetFiles(outputDir, "*.g.cs");
        var finalFileCount = finalOutputFiles.Length;

        finalFileCount.Should().Be(initialFileCount,
            "core generator should not write files to disk");

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("partial interface ITestAPI");
    }

    [Test]
    public async Task Test_CoreGenerator_ConcurrentGeneration()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = true,
        };

        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        });

        var results = await Task.WhenAll(tasks);

        foreach (var code in results)
        {
            code.Should().NotBeNullOrWhiteSpace();
            code.Should().Contain("partial interface ITestAPI");
        }
    }

    [Test]
    public async Task Test_CoreGenerator_GeneratedCodeIsValid()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = true,
        };

        var generator = await RefitGenerator.CreateAsync(settings);
        var generatedCode = generator.Generate();

        generatedCode.Should().Contain("partial interface ITestAPI");
        generatedCode.Should().Contain("Task<IApiResponse<ICollection<User>>> GetUsers(");

        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("generated code should compile successfully");
    }

    [Test]
    public async Task Test_CoreGenerator_OutputFilename_NotCreatedOnDisk()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var outputFilename = "CustomOutput.g.cs";
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OutputFilename = outputFilename,
            ReturnIApiResponse = true,
        };

        var outputDir = Path.GetDirectoryName(swaggerFile)!;
        var expectedFilePath = Path.Combine(outputDir, outputFilename);

        var generator = await RefitGenerator.CreateAsync(settings);
        var generatedCode = generator.Generate();

        File.Exists(expectedFilePath).Should().BeFalse(
            "core generator should not write files to disk");

        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

}
