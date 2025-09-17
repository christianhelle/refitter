using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class FilterByTagsTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /foo/{id}:
    get:
      tags:
      - 'Foo'
      operationId: 'Get foo details'
      description: 'Get the details of the specified foo'
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
  /foo:
    get:
      tags:
      - 'Foo'
      operationId: 'Get all foos'
      description: 'Get all foos'      
      responses:
        '200':
          description: 'successful operation'
  /bar:
    get:
      tags:
      - 'Bar'
      operationId: 'Get all bars'
      description: 'Get all bars'      
      responses:
        '200':
          description: 'successful operation'
  /bar/{id}:
    get:
      tags:
      - 'Bar'
      operationId: 'Get bar details'
      description: 'Get the details of the specified bar'   
      responses:
        '200':
          description: 'successful operation'
  /baz:
    get:
      tags:
      - 'Baz'
      operationId: 'Get all bazs'
      description: 'Get all bazs'      
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
    public async Task Generates_BarAndBaz_Methods()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("\"/bar\"");
        generatedCode.Should().Contain("\"/bar/{id}\"");
        generatedCode.Should().Contain("\"/baz\"");
        generatedCode.Should().NotContain("/foo");
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
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            IncludeTags = new[] { "Bar", "Baz" }
        };

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
