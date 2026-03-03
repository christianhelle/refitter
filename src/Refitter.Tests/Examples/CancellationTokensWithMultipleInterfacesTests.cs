using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class CancellationTokensWithMultipleInterfacesTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /users/{id}:
    get:
      tags:
      - 'Users'
      operationId: 'GetUserById'
      description: 'Get user by ID'
      parameters:
        - in: 'path'
          name: 'id'
          description: 'User ID'
          required: true
          schema:
            type: 'integer'
      responses:
        '200':
          description: 'successful operation'
  /users:
    get:
      tags:
      - 'Users'
      operationId: 'GetAllUsers'
      description: 'Get all users'
      responses:
        '200':
          description: 'successful operation'
    post:
      tags:
      - 'Users'
      operationId: 'CreateUser'
      description: 'Create a new user'
      responses:
        '201':
          description: 'created'
  /posts/{id}:
    get:
      tags:
      - 'Posts'
      operationId: 'GetPostById'
      description: 'Get post by ID'
      parameters:
        - in: 'path'
          name: 'id'
          description: 'Post ID'
          required: true
          schema:
            type: 'integer'
      responses:
        '200':
          description: 'successful operation'
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
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_CancellationToken()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("CancellationToken cancellationToken = default");
    }

    [Test]
    public async Task Generated_Code_Contains_Multiple_Interfaces()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IGetUserByIdEndpoint");
        generatedCode.Should().Contain("partial interface IGetAllUsersEndpoint");
        generatedCode.Should().Contain("partial interface ICreateUserEndpoint");
        generatedCode.Should().Contain("partial interface IGetPostByIdEndpoint");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                UseCancellationTokens = true,
                MultipleInterfaces = MultipleInterfaces.ByEndpoint
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
            {
                File.Delete(swaggerFile);
            }

            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
