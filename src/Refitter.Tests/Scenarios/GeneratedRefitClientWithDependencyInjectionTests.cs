using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class GeneratedRefitClientWithDependencyInjectionTests
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
          description: 'OK'
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Pet'
components:
  schemas:
    Pet:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
";

    [Test]
    public async Task Generated_Code_Uses_AddRefitGeneratedClient_When_Enabled()
    {
        string generatedCode = await GenerateCode(true);
        generatedCode.Should().Contain("AddRefitGeneratedClient<IPetStoreAPI>(settings)");
        generatedCode.Should().NotContain("AddRefitClient<");
    }

    [Test]
    public async Task Generated_Code_Uses_AddRefitClient_By_Default()
    {
        string generatedCode = await GenerateCode(false);
        generatedCode.Should().Contain("AddRefitClient<IPetStoreAPI>(settings)");
        generatedCode.Should().NotContain("AddRefitGeneratedClient");
    }

    [Category("Integration")]
    [Test]
    public async Task Can_Build_Generated_Code_With_AddRefitGeneratedClient()
    {
        string generatedCode = await GenerateCode(true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(bool useGeneratedRefitClient)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    BaseUrl = "https://example.com",
                    UseGeneratedRefitClient = useGeneratedRefitClient
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
