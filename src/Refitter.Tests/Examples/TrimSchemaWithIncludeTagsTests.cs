using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class TrimSchemaWithIncludeTagsTests
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
  /owners/{id}:
    get:
      tags: ['Owners']
      operationId: 'GetOwnerById'
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
        breed:
          type: string
    Owner:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
        email:
          type: string
    UnusedSchema:
      type: object
      properties:
        id:
          type: string
        data:
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
    public async Task Generated_Code_Contains_Included_Tag_Endpoints()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("GetPets");
        generatedCode.Should().Contain("GetPetById");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_Excluded_Tag_Endpoints()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("GetOwners");
        generatedCode.Should().NotContain("GetOwnerById");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_Unused_Schemas()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("class Owner");
        generatedCode.Should().NotContain("class UnusedSchema");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                TrimUnusedSchema = true,
                IncludeTags = ["Pets"]
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
