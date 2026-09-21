using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// End-to-end coverage for OpenAPI documents that pull schemas in from a sibling
/// file via a relative <c>$ref</c>, for both OpenAPI 3.0 and Swagger 2.0.
/// </summary>
public class MultiFileExternalReferenceTests
{
    private const string OpenApiV3Spec =
        @"openapi: '3.0.0'
info:
  version: '1.0.0'
  title: 'Multi File API'
paths:
  /users:
    get:
      tags:
        - 'Users'
      operationId: 'getUsers'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                $ref: './components.yml#/components/schemas/User'";

    private const string OpenApiV3Components =
        @"components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: integer
          format: int32
        name:
          type: string";

    private const string SwaggerV2Spec =
        @"swagger: '2.0'
info:
  version: '1.0.0'
  title: 'Multi File API'
paths:
  /users:
    get:
      tags:
        - 'Users'
      operationId: 'getUsers'
      produces:
        - 'application/json'
      responses:
        '200':
          description: 'Success'
          schema:
            $ref: './components.yml#/definitions/User'";

    private const string SwaggerV2Components =
        @"definitions:
  User:
    type: object
    properties:
      id:
        type: integer
        format: int32
      name:
        type: string";

    [Test]
    public async Task Can_Generate_Code_From_OpenApi_V3_External_Reference()
    {
        string generatedCode = await GenerateCode(OpenApiV3Spec, OpenApiV3Components);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Generate_Code_From_Swagger_V2_External_Reference()
    {
        string generatedCode = await GenerateCode(SwaggerV2Spec, SwaggerV2Components);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Generate_Externally_Referenced_Contract_For_OpenApi_V3()
    {
        string generatedCode = await GenerateCode(OpenApiV3Spec, OpenApiV3Components);

        generatedCode.Should().Contain("class User");
        generatedCode.Should().Contain("public int Id { get; set; }");
        generatedCode.Should().Contain("public string Name { get; set; }");
    }

    [Test]
    public async Task Should_Generate_Externally_Referenced_Contract_For_Swagger_V2()
    {
        string generatedCode = await GenerateCode(SwaggerV2Spec, SwaggerV2Components);

        generatedCode.Should().Contain("class User");
        // Swagger 2.0 has no notion of a non-nullable value type here, so NSwag emits int?
        generatedCode.Should().Contain("public int? Id { get; set; }");
        generatedCode.Should().Contain("public string Name { get; set; }");
    }

    [Test]
    public async Task Should_Generate_Interface_Returning_The_External_Type()
    {
        string generatedCode = await GenerateCode(OpenApiV3Spec, OpenApiV3Components);

        generatedCode.Should().Contain("[Get(\"/users\")]");
        generatedCode.Should().Contain("Task<User> GetUsers(");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_From_OpenApi_V3_External_Reference()
    {
        string generatedCode = await GenerateCode(OpenApiV3Spec, OpenApiV3Components);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_From_Swagger_V2_External_Reference()
    {
        string generatedCode = await GenerateCode(SwaggerV2Spec, SwaggerV2Components);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string spec, string components)
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        string specFile = Path.Combine(folder, "main.yml");
        await File.WriteAllTextAsync(specFile, spec);
        await File.WriteAllTextAsync(Path.Combine(folder, "components.yml"), components);

        RefitGeneratorSettings settings = new()
        {
            OpenApiPath = specFile,
            UseCancellationTokens = false
        };

        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
