using FluentAssertions;
using NSwag;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitPipelineTests
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

        var sut = await RefitPipeline.CreateAsync(settings);

        sut.Should().NotBeNull();
        sut.OpenApiDocument.Paths.Should().ContainKey("/foo");
        sut.OpenApiDocument.Paths.Should().ContainKey("/bar");
        sut.OpenApiDocument.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task Create_WithFilteredDocument_ReturnsGeneratorWithFilteredDocument()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var settings = new RefitGeneratorSettings
        {
            Namespace = "PipelineTest",
            IncludePathMatches = ["^/foo"]
        };

        var sut = RefitPipeline.Create(document, settings);

        sut.Should().NotBeNull();
        sut.OpenApiDocument.Paths.Should().ContainKey("/foo");
        sut.OpenApiDocument.Paths.Should().NotContainKey("/bar");
        sut.OpenApiDocument.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task Create_DoesNotMutateOriginalDocument()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var originalCount = document.Paths.Count;
        var settings = new RefitGeneratorSettings
        {
            Namespace = "PipelineTest",
            IncludeTags = ["Foo"]
        };

        RefitPipeline.Create(document, settings);

        document.Paths.Count.Should().Be(originalCount);
    }

    [Test]
    public async Task Pipeline_Generate_ProducesValidCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "PipelineTest"
        };

        var sut = await RefitPipeline.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("PipelineTest");
    }

    [Test]
    public async Task Pipeline_GenerateMultipleFiles_ProducesOutput()
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

        var sut = await RefitPipeline.CreateAsync(settings);
        var result = sut.GenerateMultipleFiles();

        result.Files.Should().NotBeEmpty();
    }
}
