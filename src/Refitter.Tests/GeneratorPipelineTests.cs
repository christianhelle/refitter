using FluentAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class GeneratorPipelineTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/api/products": {
              "get": {
                "operationId": "getProducts",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "type": "array",
                          "items": {
                            "$ref": "#/components/schemas/Product"
                          }
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
              "Product": {
                "type": "object",
                "properties": {
                  "id": {
                    "type": "integer"
                  },
                  "name": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
        """;

    private const string ByTagOpenApiSpec = """
        openapi: '3.0.0'
        info:
          title: 'Test API'
          version: '1.0'
        paths:
          /api/manage/info:
            get:
              tags:
              - 'Manage'
              responses:
                '200':
                  description: 'ok'
          /api/account/info:
            get:
              tags:
              - 'Account'
              responses:
                '200':
                  description: 'ok'
          /api/user/info:
            get:
              tags:
              - 'User'
              responses:
                '200':
                  description: 'ok'
        """;

    [Test]
    public async Task Run_Returns_All_Generated_Artifacts()
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateMultipleFiles = true,
            GenerateJsonSerializerContext = true,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://api.example.com",
                HttpMessageHandlers = [],
                TransientErrorHandler = TransientErrorHandler.None,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };
        var document = await OpenApiDocument.FromJsonAsync(OpenApiSpec);

        var result = RunPipeline(document, settings);

        result.Contracts.Should().Contain("public partial class Product");
        result.Interfaces.Should().ContainSingle();
        result.Interfaces.Single().Content.Should().Contain("partial interface");
        result.SerializerContext.Should().Contain("JsonSerializable(typeof(Product))");
        result.DependencyInjectionCode.Should().Contain("AddRefitClient");
        result.InterfaceNames.Should().Equal(result.Interfaces.Select(code => code.TypeName));
    }

    [Test]
    public async Task Run_ByTag_Preserves_PreGeneration_OperationIds_For_Interface_Naming()
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateMultipleFiles = true,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };
        var document = await OpenApiYamlDocument.FromYamlAsync(ByTagOpenApiSpec);

        var result = RunPipeline(document, settings);
        var combinedInterfaces = string.Join("\n", result.Interfaces.Select(code => code.Content));

        result.InterfaceNames.Should().Contain(["IManageApi", "IAccountApi", "IUserApi"]);
        combinedInterfaces.Should().NotContainAny("Info2(", "Info3(", "InfoGet2(", "InfoGet3(", "InfoGET2(", "InfoGET3(");
    }

    private static GenerationResult RunPipeline(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var docGenerator = new XmlDocumentationGenerator(settings);
        var interfaceGenerator = new InterfaceGenerator(settings, document, generator, docGenerator);
        var pipeline = new GeneratorPipeline(
            interfaceGenerator,
            Array.Empty<IContractsPostProcessor>());
        return pipeline.Run(document, settings, generator);
    }
}
