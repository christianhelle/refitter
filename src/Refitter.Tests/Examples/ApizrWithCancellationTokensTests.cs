using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ApizrWithCancellationTokensTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /tasks/{id}:
    get:
      tags:
      - 'Tasks'
      operationId: 'GetTaskById'
      description: 'Get task by ID'
      parameters:
        - in: 'path'
          name: 'id'
          description: 'Task ID'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
    delete:
      tags:
      - 'Tasks'
      operationId: 'DeleteTask'
      description: 'Delete a task'
      parameters:
        - in: 'path'
          name: 'id'
          description: 'Task ID'
          required: true
          schema:
            type: 'string'
      responses:
        '204':
          description: 'no content'
  /tasks:
    get:
      tags:
      - 'Tasks'
      operationId: 'GetAllTasks'
      description: 'Get all tasks'
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
    public async Task Generated_Code_Contains_RequestOptions()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[RequestOptions] IApizrRequestOptions options");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_CancellationToken_When_Apizr_Used()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("CancellationToken cancellationToken");
    }

    [Test]
    public async Task Generated_Code_Uses_Apizr_Request_Options_Pattern()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IApizrRequestOptions options);");
        generatedCode.Should().Contain("[RequestOptions]");
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
                ApizrSettings = new ApizrSettings
                {
                    WithRequestOptions = true
                }
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
