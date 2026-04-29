using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class MultipleInterfacesByTagWithDependencyInjectionTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'Multi-Tag API'
  version: '1.0'
paths:
  /pets:
    get:
      tags: ['Pets']
      operationId: 'GetPets'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Pet'
  /pets/{id}:
    get:
      tags: ['Pets']
      operationId: 'GetPetById'
      parameters:
        - in: path
          name: id
          required: true
          schema:
            type: string
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Pet'
  /owners:
    get:
      tags: ['Owners']
      operationId: 'GetOwners'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Owner'
components:
  schemas:
    Pet:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
    Owner:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_Multiple_Interfaces()
    {
        string generatedCode = await GenerateCode();
        var interfaceCount = System.Text.RegularExpressions.Regex.Matches(generatedCode, @"interface I\w+").Count;
        interfaceCount.Should().BeGreaterThan(1);
    }

    [Test]
    public async Task Generated_Code_Contains_DI_Registration()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Match(x =>
            x.Contains("ServiceCollectionExtensions") ||
            x.Contains("AddRefitClients") ||
            x.Contains("IServiceCollection"));
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                MultipleInterfaces = MultipleInterfaces.ByTag,
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    BaseUrl = "https://example.com"
                }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile)) File.Delete(swaggerFile);
            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
