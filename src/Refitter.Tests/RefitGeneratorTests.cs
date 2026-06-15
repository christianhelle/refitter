using FluentAssertions;
using NSwag;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitGeneratorTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0'
paths:
  /foo:
    get:
      tags: ['Foo']
      operationId: 'GetAllFoos'
      responses:
        '200':
          description: success
  /bar:
    get:
      tags: ['Bar']
      operationId: 'GetAllBars'
      responses:
        '200':
          description: success
  /baz:
    get:
      tags: ['Baz']
      operationId: 'GetAllBazs'
      responses:
        '200':
          description: success
";

    private const string SwaggerSpec = @"
swagger: '2.0'
info:
  title: Test API
  version: '1.0'
host: api.example.com
basePath: /v1
paths:
  /foo:
    get:
      tags: ['Foo']
      operationId: 'GetAllFoos'
      responses:
        '200':
          description: success
  /bar:
    get:
      tags: ['Bar']
      operationId: 'GetAllBars'
      responses:
        '200':
          description: success
  /baz:
    get:
      tags: ['Baz']
      operationId: 'GetAllBazs'
      responses:
        '200':
          description: success
";

    [Test]
    public async Task CreateAsync_LoadsAndProcessesDocument()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest",
            IncludeTags = ["Foo", "Bar"]
        };

        var sut = await RefitGenerator.CreateAsync(settings);

        sut.Should().NotBeNull();
        sut.OpenApiDocument.Paths.Should().ContainKey("/foo");
        sut.OpenApiDocument.Paths.Should().ContainKey("/bar");
        sut.OpenApiDocument.Paths.Should().NotContainKey("/baz");
        var generatedCode = sut.Generate();
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("PipelineTest");
    }

    [Test]
    public async Task CreateAsync_WithFilteredDocument_ReturnsGeneratorWithFilteredDocument()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest",
            IncludePathMatches = ["^/foo"]
        };

        var generator = await RefitGenerator.CreateAsync(settings);
        generator.OpenApiDocument.Paths.Should().ContainKey("/foo");
        generator.OpenApiDocument.Paths.Should().NotContainKey("/bar");
        generator.OpenApiDocument.Paths.Should().NotContainKey("/baz");
        var generatedCode = generator.Generate();

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("PipelineTest");
    }

    [Test]
    public async Task CreateAsync_FilterByTags_PreservesOnlyMatchingPaths()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest",
            IncludeTags = ["Foo"]
        };

        var generator = await RefitGenerator.CreateAsync(settings);
        generator.OpenApiDocument.Paths.Should().ContainKey("/foo");
        generator.OpenApiDocument.Paths.Should().NotContainKey("/bar");
        generator.OpenApiDocument.Paths.Should().NotContainKey("/baz");
        var generatedCode = generator.Generate();

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("PipelineTest");
    }

    [Test]
    public async Task Generate_ProducesValidCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest"
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("PipelineTest");
    }

    [Test]
    public async Task GenerateMultipleFiles_ProducesOutput()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest",
            GenerateMultipleFiles = true,
            GenerateContracts = true,
            GenerateClients = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var result = sut.GenerateMultipleFiles();

        result.Files.Should().NotBeEmpty();
    }

    [Test]
    public async Task CreateAsync_WithSwagger20_LoadsAndProcessesDocument()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(SwaggerSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest",
            IncludeTags = ["Foo", "Bar"]
        };

        var sut = await RefitGenerator.CreateAsync(settings);

        sut.Should().NotBeNull();
        sut.OpenApiDocument.Paths.Should().ContainKey("/foo");
        sut.OpenApiDocument.Paths.Should().ContainKey("/bar");
        sut.OpenApiDocument.Paths.Should().NotContainKey("/baz");
        var generatedCode = sut.Generate();
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("PipelineTest");
    }

    [Test]
    public async Task Generate_WithSwagger20_ProducesValidCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(SwaggerSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest"
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("PipelineTest");
    }
}
