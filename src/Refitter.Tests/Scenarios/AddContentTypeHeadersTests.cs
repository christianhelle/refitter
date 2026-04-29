using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class AddContentTypeHeadersTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Content Type Test API
  version: 1.0.0
paths:
  /api/users:
    post:
      operationId: CreateUser
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/User'
      responses:
        '201':
          description: User created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/User'
components:
  schemas:
    User:
      type: object
      required:
        - name
        - email
      properties:
        id:
          type: integer
          format: int32
        name:
          type: string
        email:
          type: string
          format: email
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode(true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_ContentType_Header_When_Enabled()
    {
        string generatedCode = await GenerateCode(true);
        generatedCode.Should().Contain("Content-Type: application/json");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_ContentType_Header_When_Disabled()
    {
        string generatedCode = await GenerateCode(false);
        generatedCode.Should().NotContain("Headers(\"Content-Type:");
    }

    [Test]
    public async Task Can_Build_Generated_Code_When_Disabled()
    {
        string generatedCode = await GenerateCode(false);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(bool addContentTypeHeaders)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                AddContentTypeHeaders = addContentTypeHeaders
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
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
