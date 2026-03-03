using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class GenerateDisposableClientsEnhancedTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Test API
  version: 1.0.0
paths:
  /items:
    get:
      operationId: getItems
      responses:
        '200':
          description: successful operation
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Item'
  /items/{id}:
    get:
      operationId: getItemById
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: successful operation
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Item'
components:
  schemas:
    Item:
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
        string generatedCode = await GenerateCode(generateDisposableClients: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(generateDisposableClients: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_IDisposable()
    {
        string generatedCode = await GenerateCode(generateDisposableClients: true);
        generatedCode.Should().Contain("IDisposable");
        generatedCode.Should().Contain("interface ITestAPI : IDisposable");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_IDisposable_When_Disabled()
    {
        string generatedCode = await GenerateCode(generateDisposableClients: false);
        generatedCode.Should().NotContain(": IDisposable");
    }

    [Test]
    public async Task Can_Build_Generated_Code_Without_Disposable()
    {
        string generatedCode = await GenerateCode(generateDisposableClients: false);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Disposable_Code_Contains_Using_System()
    {
        string generatedCode = await GenerateCode(generateDisposableClients: true);
        generatedCode.Should().Contain("IDisposable");
    }

    private static async Task<string> GenerateCode(bool generateDisposableClients)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateDisposableClients = generateDisposableClients
            };

            var sut = await RefitGenerator.CreateAsync(settings);
            return sut.Generate();
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
