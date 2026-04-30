using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class CoreGenerationRegressionTests
{
    private const string ApizrOptionalByEndpointSpec = """
        openapi: '3.0.0'
        info:
          title: Apizr Optional API
          version: 1.0.0
        paths:
          /items/{id}:
            get:
              operationId: getItem
              parameters:
                - name: id
                  in: path
                  required: true
                  schema:
                    type: string
                - name: filter
                  in: query
                  required: false
                  schema:
                    type: string
              responses:
                '200':
                  description: OK
        """;

    private const string EscapedLiteralsSpec = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Escaped Literals API",
            "version": "1.0.0"
          },
          "paths": {
            "/escaped/\"quoted\"/\\slash/\nline": {
              "get": {
                "operationId": "getEscaped",
                "parameters": [
                  {
                    "name": "X-\"Quote\"-\\Slash-\nLine",
                    "in": "header",
                    "required": true,
                    "schema": {
                      "type": "string"
                    }
                  }
                ],
                "responses": {
                  "202": {
                    "description": "Accepted",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/EscapedResponse"
                        }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "EscapedResponse": {
                "type": "object",
                "properties": {
                  "value": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
        """;

    private const string SuccessCodesSpec = """
        openapi: '3.0.0'
        info:
          title: Success Codes API
          version: 1.0.0
        paths:
          /accepted:
            get:
              operationId: getAccepted
              responses:
                '202':
                  description: Accepted
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/AcceptedResponse'
          /multi-status-file:
            get:
              operationId: getMultiStatusFile
              responses:
                '207':
                  description: Multi-status file
                  content:
                    application/octet-stream:
                      schema:
                        type: string
                        format: binary
          /no-content:
            delete:
              operationId: deleteNoContent
              responses:
                '204':
                  description: No content
        components:
          schemas:
            AcceptedResponse:
              type: object
              properties:
                id:
                  type: string
        """;

    private const string DeprecatedOnlySpec = """
        openapi: '3.0.0'
        info:
          title: Deprecated Only API
          version: 1.0.0
        paths:
          /old:
            get:
              operationId: getOld
              deprecated: true
              responses:
                '200':
                  description: OK
        """;

    private const string MultipartOptionalSpec = """
        openapi: '3.0.0'
        info:
          title: Multipart API
          version: 1.0.0
        paths:
          /upload:
            post:
              operationId: upload
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      required:
                        - requiredName
                      properties:
                        requiredName:
                          type: string
                        optional-name:
                          type: string
              responses:
                '200':
                  description: OK
        """;

    [Test]
    public async Task MultipleInterfaces_ByEndpoint_Apizr_Optional_Overload_Builds()
    {
        var generatedCode = await GenerateCode(
            ApizrOptionalByEndpointSpec,
            new RefitGeneratorSettings
            {
                MultipleInterfaces = MultipleInterfaces.ByEndpoint,
                OptionalParameters = true,
                ApizrSettings = new ApizrSettings { WithRequestOptions = true }
            });

        generatedCode.Should().Contain("Task Execute(string id, [Query] string? filter, [RequestOptions] IApizrRequestOptions options);");
        generatedCode.Should().Contain("Task Execute(string id, [RequestOptions] IApizrRequestOptions options);");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Escapes_Generated_Route_Header_And_BaseUrl_Literals()
    {
        var generatedCode = await GenerateJsonCode(
            EscapedLiteralsSpec,
            new RefitGeneratorSettings
            {
                GenerateOperationHeaders = true,
                GenerateXmlDocCodeComments = false,
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    BaseUrl = "https://example.com/\"quoted\"/\\slash\nline"
                },
                ApizrSettings = new ApizrSettings { WithRegistrationHelper = true }
            });

        generatedCode.Should().Contain("\\\"quoted\\\"");
        generatedCode.Should().Contain("\\\\slash");
        generatedCode.Should().Contain("\\nline");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Uses_All_Explicit_Numeric_2xx_Status_Codes()
    {
        var generatedCode = await GenerateCode(SuccessCodesSpec, new RefitGeneratorSettings());

        generatedCode.Should().Contain("Task<AcceptedResponse> GetAccepted()");
        generatedCode.Should().Contain("Task<HttpResponseMessage> GetMultiStatusFile()");
        generatedCode.Should().Contain("Task DeleteNoContent()");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Throws_Clear_Exception_When_No_Interfaces_Are_Generated()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(DeprecatedOnlySpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByEndpoint,
            GenerateDeprecatedOperations = false
        };
        var generator = await RefitGenerator.CreateAsync(settings);

        var act = () => generator.Generate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No Refit interfaces were generated*");
    }

    [Test]
    public async Task GenerateMultipleFiles_Respects_GenerateClients_False()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(SuccessCodesSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateMultipleFiles = true,
            GenerateClients = false,
            GenerateContracts = true,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://example.com"
            }
        };
        var generator = await RefitGenerator.CreateAsync(settings);

        var files = generator.GenerateMultipleFiles().Files;

        files.Should().NotContain(file => file.Content.Contains("interface", StringComparison.Ordinal));
        files.Should().NotContain(file => file.Content.Contains("AddRefitClient", StringComparison.Ordinal));
        files.Should().Contain(file => file.TypeName == TypenameConstants.Contracts);
    }

    [Test]
    public async Task Multipart_Form_Data_Respects_Optional_Required_Metadata()
    {
        var generatedCode = await GenerateCode(
            MultipartOptionalSpec,
            new RefitGeneratorSettings { OptionalParameters = true });

        generatedCode.Should().Contain("string requiredName");
        generatedCode.Should().Contain("string? optional_name = default");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string openApiSpec, RefitGeneratorSettings settings)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
        settings.OpenApiPath = swaggerFile;
        var generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }

    private static async Task<string> GenerateJsonCode(string openApiSpec, RefitGeneratorSettings settings)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(openApiSpec);
        settings.OpenApiPath = swaggerFile;
        var generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
