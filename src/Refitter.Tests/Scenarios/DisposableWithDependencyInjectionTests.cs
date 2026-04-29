using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class DisposableWithDependencyInjectionTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'Pet Store API'
  version: '1.0'
paths:
  /pets:
    get:
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
    post:
      operationId: 'CreatePet'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Pet'
      responses:
        '201':
          description: 'Created'
  /pets/{id}:
    get:
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
    delete:
      operationId: 'DeletePet'
      parameters:
        - in: path
          name: id
          required: true
          schema:
            type: string
      responses:
        '204':
          description: 'No Content'
components:
  schemas:
    Pet:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
        status:
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
    public async Task Generated_Code_Contains_IDisposable()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IDisposable");
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
                GenerateDisposableClients = true,
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
