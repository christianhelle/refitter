using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

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

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Generates_IDeleteFooEndpoint()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("partial interface IDeleteFooEndpoint");
    }

    [Fact]
    public async Task Generates_IDeleteBarEndpoint()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("partial interface IDeleteBarEndpoint");
    }

    [Fact]
    public async Task Generates_IServiceCollectionExtensions()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("static partial class IServiceCollectionExtensions");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateContracts = false,
            MultipleInterfaces = MultipleInterfaces.ByEndpoint,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://example.com/api/v1",
                UsePolly = true
            }
        };

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
