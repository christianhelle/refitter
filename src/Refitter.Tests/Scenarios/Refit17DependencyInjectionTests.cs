using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Refit 17 removed the reflection-based AddRefitClient, so DI registration must use AddRefitGeneratedClient.
/// The net10.0 and net11.0 build templates reference Refit 17.
/// </summary>
[TestClass]
public class Refit17DependencyInjectionTests
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
      parameters:
        - name: 'limit'
          in: 'query'
          schema:
            type: integer
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

    [TestMethod]
    [DataRow(TransientErrorHandler.None)]
    [DataRow(TransientErrorHandler.Polly)]
    [DataRow(TransientErrorHandler.HttpResilience)]
    public async Task Generated_Code_Uses_AddRefitGeneratedClient(TransientErrorHandler transientErrorHandler)
    {
        string generatedCode = await GenerateCode(transientErrorHandler, useGeneratedRefitClient: true);
        generatedCode.Should().Contain("AddRefitGeneratedClient<IPetStoreAPI>(settings)");
        generatedCode.Should().NotContain("AddRefitClient<");
    }

    [TestCategory("Integration")]
    [TestMethod]
    [DataRow("net10.0", TransientErrorHandler.None)]
    [DataRow("net10.0", TransientErrorHandler.Polly)]
    [DataRow("net10.0", TransientErrorHandler.HttpResilience)]
    [DataRow("net11.0", TransientErrorHandler.HttpResilience)]
    public async Task Can_Build_Generated_Code_Against_Refit_17(
        string targetFramework,
        TransientErrorHandler transientErrorHandler)
    {
        string generatedCode = await GenerateCode(transientErrorHandler, useGeneratedRefitClient: true);
        BuildHelper
            .BuildCSharpForTargetFramework(targetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [TestCategory("Integration")]
    [TestMethod]
    public async Task Reflection_Based_Registration_Does_Not_Build_Against_Refit_17()
    {
        string generatedCode = await GenerateCode(TransientErrorHandler.None, useGeneratedRefitClient: false);
        Action build = () => BuildHelper.BuildCSharpForTargetFramework("net10.0", generatedCode);
        build.Should().Throw<BuildFailedException>();
    }

    private static async Task<string> GenerateCode(
        TransientErrorHandler transientErrorHandler,
        bool useGeneratedRefitClient)
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
                    TransientErrorHandler = transientErrorHandler,
                    MaxRetryCount = 3,
                    FirstBackoffRetryInSeconds = 0.5,
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
