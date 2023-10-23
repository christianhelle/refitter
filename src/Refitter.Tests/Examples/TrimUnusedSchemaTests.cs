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
  /v1/Logins/login:
    post:
      tags:
        - Logins
      operationId: PerformLogin
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/LoginRequest'
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/LoginResult'
        '422':
          description: Client Error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
components:
  schemas:
    LoginRequest:
      type: object
      properties:
        username:
          type: string
          nullable: true
        password:
          type: string
          nullable: true
      additionalProperties: false
    LoginResult:
      type: object
      properties:
        userId:
          type: string
          format: uuid
        accessToken:
          type: string
          nullable: true
        accessTokenExpiresAt:
          type: string
          format: date-time
        refreshToken:
          type: string
          nullable: true
        refreshTokenExpiresAt:
          type: string
          format: date-time
      additionalProperties: false
    ProblemDetails:
      type: object
      properties:
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
    RefreshLoginRequest:
      type: object
      properties:
        refreshToken:
          type: string
          nullable: true
      additionalProperties: false
    RefreshLoginResult:
      type: object
      properties:
        userId:
          type: string
          format: uuid
        accessToken:
          type: string
          nullable: true
        accessTokenExpiresAt:
          type: string
          format: date-time
        refreshToken:
          type: string
          nullable: true
        refreshTokenExpiresAt:
          type: string
          format: date-time
      additionalProperties: false
    User:
      type: object
      properties:
        id:
          type: string
          nullable: true
        username:
          type: string
          nullable: true
      additionalProperties: false
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
        generateCode.Should().Contain("class LoginRequest");
        generateCode.Should().Contain("class LoginResult");
        generateCode.Should().Contain("class ProblemDetails");

        generateCode.Should().Contain("Task<LoginResult> PerformLogin([Body] LoginRequest ");
        
        generateCode.Should().NotContain("class User");
        generateCode.Should().NotContain("class RefreshLoginResult");
        generateCode.Should().NotContain("class RefreshLoginRequest");
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