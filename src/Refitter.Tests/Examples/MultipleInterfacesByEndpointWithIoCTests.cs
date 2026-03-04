using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class MultipleInterfacesByEndpointWithIoCTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /foo/{id}:
    delete:
      tags:
      - 'Foo'
      operationId: 'Delete foo'
      description: 'Delete the specified foo'
      parameters:
        - in: 'path'
          name: 'id'
          description: 'Foo ID'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
  /bar/{id}:
    delete:
      tags:
      - 'Bar'
      operationId: 'Delete bar'
      description: 'Delete the specified bar' 
      responses:
        '200':
          description: 'successful operation'
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_IDeleteFooEndpoint()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IDeleteFooEndpoint");
    }

    [Test]
    public async Task Generates_IDeleteBarEndpoint()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IDeleteBarEndpoint");
    }

    [Test]
    public async Task Generates_IServiceCollectionExtensions()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("static partial class IServiceCollectionExtensions");
    }

    [Test]
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
            OpenApiPath = swaggerFile,
            GenerateContracts = false,
            MultipleInterfaces = MultipleInterfaces.ByEndpoint,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://example.com/api/v1",
                TransientErrorHandler = TransientErrorHandler.Polly
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
