using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ContractOnlyWithMultipleInterfacesTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'API'
  version: '1.0'
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
  /products:
    get:
      operationId: 'GetProducts'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Product'
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
        email:
          type: string
    Product:
      type: object
      properties:
        id:
          type: integer
        title:
          type: string
        price:
          type: number
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
    public async Task Generated_Code_Does_Not_Contain_Interface()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("interface I");
        generatedCode.Should().NotContain("[Get(");
        generatedCode.Should().NotContain("[Post(");
    }

    [Test]
    public async Task Generated_Code_Contains_Contracts()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("class User");
        generatedCode.Should().Contain("class Product");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateClients = false,
                MultipleInterfaces = MultipleInterfaces.ByEndpoint
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
