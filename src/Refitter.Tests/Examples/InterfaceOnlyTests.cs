using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class InterfaceOnlyTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Test API
  version: 1.0.0
paths:
  /pets:
    get:
      operationId: getPets
      responses:
        '200':
          description: successful operation
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Pet'
    post:
      operationId: createPet
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Pet'
      responses:
        '201':
          description: created
  /pets/{id}:
    get:
      operationId: getPetById
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
    public async Task Generated_Code_Contains_Interface()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("interface ITestAPI");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_Contract_Classes()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("partial class Pet");
    }

    [Test]
    public async Task Generated_Code_Contains_Method_Signatures()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("GetPets");
        generatedCode.Should().Contain("CreatePet");
        generatedCode.Should().Contain("GetPetById");
    }

    [Test]
    public async Task Generated_Code_Contains_Refit_Attributes()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Get(\"/pets\")]");
        generatedCode.Should().Contain("[Post(\"/pets\")]");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateContracts = false,
                GenerateClients = true
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
