using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class DefaultResponseObjectTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: '1.0.0'
  title: 'Default Response API'
  description: 'An API that uses default responses and range responses'
servers:
  - url: 'https://api.example.com/v1'
paths:
  /users:
    get:
      tags:
        - 'Users'
      summary: 'Get users list using default response'
      description: 'Returns a list of users using default response type'
      operationId: 'getUsersWithDefault'
      responses:
        default:
          description: 'List of users with unknown status code'
          content:
            application/json:
              schema:
                type: 'array'
                items:
                  $ref: '#/components/schemas/User'
  /users/{id}:
    get:
      tags:
        - 'Users'
      summary: 'Get user by ID using default response'
      description: 'Returns a user by ID using default response type'
      operationId: 'getUserByIdWithDefault'
      parameters:
        - name: 'id'
          in: 'path'
          description: 'User ID'
          required: true
          schema:
            type: 'string'
      responses:
        default:
          description: 'User data with unknown status code'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/User'
  /categories:
    get:
      tags:
        - 'Categories'
      summary: 'Get categories using 2XX range response'
      description: 'Returns a list of categories using 2XX range response'
      operationId: 'getCategoriesWithRange'
      responses:
        2XX:
          description: 'List of categories with 2XX status code'
          content:
            application/json:
              schema:
                type: 'array'
                items:
                  $ref: '#/components/schemas/Category'
  /categories/{id}:
    get:
      tags:
        - 'Categories'
      summary: 'Get category by ID using 2XX range response'
      description: 'Returns a category by ID using 2XX range response'
      operationId: 'getCategoryByIdWithRange'
      parameters:
        - name: 'id'
          in: 'path'
          description: 'Category ID'
          required: true
          schema:
            type: 'string'
      responses:
        2XX:
          description: 'Category data with 2XX status code'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Category'
components:
  schemas:
    User:
      type: 'object'
      properties:
        id:
          type: 'string'
        name:
          type: 'string'
        email:
          type: 'string'
          format: 'email'
    Category:
      type: 'object'
      properties:
        id:
          type: 'string'
        name:
          type: 'string'
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Should_Generate_Default_Response_Return_Types()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<User> GetUserByIdWithDefault(string id);");
        generatedCode.Should().Contain("Task<ICollection<User>> GetUsersWithDefault();");
    }

    [Fact]
    public async Task Should_Generate_Range_Response_Return_Types()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Category> GetCategoryByIdWithRange(string id);");
        generatedCode.Should().Contain("Task<ICollection<Category>> GetCategoriesWithRange();");
    }

    [Fact]
    public async Task Should_Generate_User_Contract()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public partial class User");
        generatedCode.Should().Contain("public string Id { get; set; }");
        generatedCode.Should().Contain("public string Name { get; set; }");
        generatedCode.Should().Contain("public string Email { get; set; }");
    }

    [Fact]
    public async Task Should_Generate_Category_Contract()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public partial class Category");
        generatedCode.Should().Contain("public string Id { get; set; }");
        generatedCode.Should().Contain("public string Name { get; set; }");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseCancellationTokens = false
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
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
