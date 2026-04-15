using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class TrimUnusedSchemaAliasWithLeadingDigitPropertyTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.4
        info:
          title: "Multi content test"
          version: "1"
        servers:
          - url: https://localhost:8000
        paths:
          /test:
            post:
              tags:
                - Test
              operationId: DoTest
              parameters:
                - name: inQueryParam
                  in: query
                  description: "inQueryParam test"
                  schema:
                    type: boolean
              requestBody:
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/TestRequest'
                  multipart/form-data:
                    schema:
                      $ref: '#/components/schemas/TestRequest'
                required: true
                x-go-name: Request
                x-bodyName: request
              responses:
                '201':
                  description: "A response"
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/TestResponse'
        components:
          schemas:
            TestRequest:
              title: "A test request"
              required:
                - type
              type: object
              properties:
                42_question:
                  $ref: '#/components/schemas/A42Question'
                request_param1:
                  type: string
                request_param2:
                  type: array
                  items:
                    $ref: '#/components/schemas/RequestParamArrayItem'
                request_param3:
                  $ref: '#/components/schemas/RequestParamArrayItem'
            A42Question:
              title: "A question about.. well, you know"
              type: object
              properties:
                param1:
                  type: string
                param2:
                  type: string
            TestResponse:
              title: "A test response"
              type: object
              properties:
                item:
                  $ref: '#/components/schemas/RequestParamArrayItem'
            RequestParamArrayItem:
              $ref: '#/components/schemas/RequestParamArrayItemInternal'
            RequestParamArrayItemInternal:
              required:
                - param1
              type: object
              properties:
                param1:
                  type: number
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                Namespace = "TestApi",
                TrimUnusedSchema = true,
                GenerateOperationHeaders = false,
                UseIsoDateFormat = true,
                UsePolymorphicSerialization = true
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
