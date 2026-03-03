using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ContractsNamespaceTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Namespace Test API
  version: 1.0.0
paths:
  /api/products:
    get:
      operationId: GetProducts
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Product'
    post:
      operationId: CreateProduct
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateProductRequest'
      responses:
        '201':
          description: Product created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'
components:
  schemas:
    Product:
      type: object
      required:
        - id
        - name
        - price
      properties:
        id:
          type: integer
          format: int32
        name:
          type: string
        price:
          type: number
          format: double
        category:
          type: string
    CreateProductRequest:
      type: object
      required:
        - name
        - price
      properties:
        name:
          type: string
        price:
          type: number
          format: double
        category:
          type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode("TestApi.Contracts");
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode("TestApi.Contracts");
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_Contracts_Namespace()
    {
        string generatedCode = await GenerateCode("TestApi.Contracts");
        generatedCode.Should().Contain("namespace TestApi.Contracts");
    }

    [Test]
    public async Task Generated_Code_Contains_Interface_Namespace()
    {
        string generatedCode = await GenerateCode("TestApi.Contracts");
        generatedCode.Should().Contain("namespace TestApi");
    }

    [Test]
    public async Task Generated_Code_Uses_Same_Namespace_When_Not_Set()
    {
        string generatedCode = await GenerateCode(null);
        generatedCode.Should().NotBeNullOrWhiteSpace();

        // When ContractsNamespace is not set, everything uses the default namespace
        var namespaceCount = System.Text.RegularExpressions.Regex.Matches(generatedCode, "namespace TestApi").Count;
        namespaceCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Can_Build_Generated_Code_Without_Contracts_Namespace()
    {
        string generatedCode = await GenerateCode(null);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(string? contractsNamespace)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                Namespace = "TestApi",
                ContractsNamespace = contractsNamespace
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
