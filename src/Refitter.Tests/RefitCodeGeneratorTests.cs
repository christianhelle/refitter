using FluentAssertions;
using NSwag;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitCodeGeneratorTests
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
";

    [Category("Unit")]
    [Test]
    public async Task Generate_Produces_Valid_Code()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            OpenApiPath = swaggerFile
        };

        var sut = new RefitCodeGenerator();
        var result = sut.Generate(document, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("TestNamespace");
    }

    [Category("Unit")]
    [Test]
    public async Task Generate_WithFilteredDocument_ProducesValidCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var filtered = RefitDocumentFilter.FilterByTags(document, ["Bar"]);
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            OpenApiPath = swaggerFile
        };

        var sut = new RefitCodeGenerator();
        var result = sut.Generate(filtered, settings);

        result.Should().Contain("\"/bar\"");
        result.Should().NotContain("\"/foo\"");
    }

    [Category("Integration")]
    [Test]
    public async Task Generate_Compiles_Successfully()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            OpenApiPath = swaggerFile,
            GenerateContracts = true,
            GenerateClients = true
        };

        var sut = new RefitCodeGenerator();
        var result = sut.Generate(document, settings);

        BuildHelper.BuildCSharp(result).Should().BeTrue();
    }

    [Category("Unit")]
    [Test]
    public async Task GenerateMultipleFiles_Produces_Output()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            OpenApiPath = swaggerFile,
            GenerateMultipleFiles = true,
            GenerateContracts = true,
            GenerateClients = true
        };

        var sut = new RefitCodeGenerator();
        var result = sut.GenerateMultipleFiles(document, settings);

        result.Files.Should().NotBeEmpty();
        result.Files.Should().Contain(f => f.TypeName == "Contracts");
    }

    [Category("Unit")]
    [Test]
    public async Task Generate_WithSwagger20_Produces_Valid_Code()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(SwaggerSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            OpenApiPath = swaggerFile
        };

        var sut = new RefitCodeGenerator();
        var result = sut.Generate(document, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("TestNamespace");
    }

    [Category("Unit")]
    [Test]
    public async Task GenerateMultipleFiles_WithSwagger20_Produces_Output()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(SwaggerSpec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            OpenApiPath = swaggerFile,
            GenerateMultipleFiles = true,
            GenerateContracts = true,
            GenerateClients = true
        };

        var sut = new RefitCodeGenerator();
        var result = sut.GenerateMultipleFiles(document, settings);

        result.Files.Should().NotBeEmpty();
        result.Files.Should().Contain(f => f.TypeName == "Contracts");
    }
}
