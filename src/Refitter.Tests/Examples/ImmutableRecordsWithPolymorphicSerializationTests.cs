using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ImmutableRecordsWithPolymorphicSerializationTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'Animal API'
  version: '1.0'
paths:
  /animals:
    get:
      operationId: 'GetAnimals'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Animal'
    post:
      operationId: 'CreateAnimal'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Animal'
      responses:
        '201':
          description: 'Created'
components:
  schemas:
    Animal:
      type: object
      discriminator:
        propertyName: type
      required:
        - type
        - name
      properties:
        type:
          type: string
        name:
          type: string
    Dog:
      allOf:
        - $ref: '#/components/schemas/Animal'
        - type: object
          properties:
            breed:
              type: string
    Cat:
      allOf:
        - $ref: '#/components/schemas/Animal'
        - type: object
          properties:
            color:
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
    public async Task Generated_Code_Contains_Record_Keyword()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("record ");
    }

    [Test]
    public async Task Generated_Code_Contains_JsonPolymorphic_Attribute()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Match(x =>
            x.Contains("[JsonPolymorphic") ||
            x.Contains("[JsonDerivedType"));
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                ImmutableRecords = true,
                UsePolymorphicSerialization = true
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
