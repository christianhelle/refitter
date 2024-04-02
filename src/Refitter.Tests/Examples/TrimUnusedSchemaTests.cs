using FluentAssertions;

using Refitter.Core;
using Refitter.Tests.Build;

using Xunit;

namespace Refitter.Tests.Examples;

public class TrimUnusedSchemaTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
paths:
  /v1/Warehouses:
    post:
      tags:
        - Warehouses
      operationId: CreateWarehouse
      parameters:
      - name: 'token'
        in: 'query'
        description: 'Some Token'
        required: false
        type: 'string'
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Warehouse'
      responses:
        '201':
          description: Created
          headers:
            X-Rate-Limit:
              type: 'integer'
              format: 'int32'
              description: 'calls per hour allowed by the user'
          content:
            application/json:
              schema:
                type: 'array'
                items:
                  $ref: '#/components/schemas/WarehouseResponse'
        '400':
          description: Bad Request
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
        '500':
          description: Server Error
components:
  schemas:
    Metadata:
      type: object
      properties:
        createdAt:
          type: string
          format: date-time
        createdBy:
          type: string
          nullable: true
        lastModifiedAt:
          type: string
          format: date-time
        lastModifiedBy:
          type: string
          nullable: true
      additionalProperties: false
    SomeComponent:
      required:
        - $type
      type: object
      allOf:
        - $ref: '#/components/schemas/Component'
      properties:
        $type:
          type: string
        typeId:
          type: integer
          format: int64
      additionalProperties: false
      discriminator:
        propertyName: $type
    SomeComponentState:
      enum:
        - Active
        - Inactive
        - Blocked
        - Deleted
      type: string
    SomeComponentType:
      type: object
      allOf:
        - $ref: '#/components/schemas/Component'
      properties:
        state:
          $ref: '#/components/schemas/SomeComponentState'
        isBaseRole:
          type: boolean
        name:
          type: string
          nullable: true
        numberingId:
          type: string
          nullable: true
      additionalProperties: false
    Component:
      type: object
      properties:
        id:
          type: integer
          format: int64
        metadata:
          $ref: '#/components/schemas/Metadata'
      additionalProperties: false
    LoadingAddress:
      type: object
      allOf:
        - $ref: '#/components/schemas/SomeComponent'
      properties:
        info:
          type: string
          nullable: true
      additionalProperties: false
    Warehouse:
      type: object
      allOf:
        - $ref: '#/components/schemas/SomeComponent'
      properties:
        info:
          type: string
          nullable: true
      additionalProperties: false
    WarehouseResponse:
      type: object
      allOf:
        - $ref: '#/components/schemas/SomeComponent'
      properties:
        info:
          type: string
          nullable: true
      additionalProperties: false
    UserComponent:
      type: object
      allOf:
        - $ref: '#/components/schemas/SomeComponent'
      properties:
        info:
          type: string
          nullable: true
      additionalProperties: false
    UserComponent2:
      type: object
      allOf:
        - $ref: '#/components/schemas/UserComponent'
      properties:
        info2:
          type: string
          nullable: true
      additionalProperties: false
    ProblemDetails:
      required:
        - $type
      type: object
      properties:
        $type:
          type: string
        type:
          type: string
          nullable: true
        title:
          type: string
          nullable: true
        status:
          type: integer
          format: int32
          nullable: true
        detail:
          type: string
          nullable: true
        instance:
          type: string
          nullable: true
      additionalProperties: { }
      discriminator:
        propertyName: $type
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Removes_Unreferenced_Schema()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("class Warehouse");
        generateCode.Should().Contain("class WarehouseResponse");
        generateCode.Should().Contain("class SomeComponent");
        generateCode.Should().Contain("class Component");
        generateCode.Should().Contain("class ProblemDetails");

        // Schema is manipulated to return ARRAY of WarehouseResponse
        // previously (<= 0.9.9.0) this wasn't accomodated.
        //
        // For whatever reason NSwag OpenApi-Schema doesn't contain Schema for "Items"
        // Instead it's inside "Item"
        generateCode.Should().Contain("Task<ICollection<WarehouseResponse>> CreateWarehouse([Query] string token, [Body] Warehouse ");

        generateCode.Should().NotContain("class UserComponent");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            TrimUnusedSchema = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}