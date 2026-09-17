using FluentAssertions;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class MethodAttributeGeneratorTests
{
    [Test]
    public async Task Generate_Includes_Obsolete_When_Deprecated()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                get:
                  operationId: getTest
                  deprecated: true
                  responses:
                    '200':
                      description: Success
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().Contain("[System.Obsolete]");
    }

    [Test]
    public async Task Generate_Includes_Multipart_For_MultipartFormData()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                post:
                  operationId: uploadFile
                  requestBody:
                    required: true
                    content:
                      multipart/form-data:
                        schema:
                          type: object
                          properties:
                            file:
                              type: string
                              format: binary
                  responses:
                    '200':
                      description: Success
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["post"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().Contain("[Multipart]");
    }

    [Test]
    public async Task Generate_Includes_Accept_Header_When_Enabled()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                get:
                  operationId: getTest
                  responses:
                    '200':
                      description: Success
                      content:
                        application/json:
                          schema:
                            type: string
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings { AddAcceptHeaders = true };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().Contain(a => a.Contains("Accept"));
    }

    [Test]
    public async Task Generate_Includes_ContentType_Header_When_Enabled()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                post:
                  operationId: createTest
                  requestBody:
                    required: true
                    content:
                      application/json:
                        schema:
                          type: object
                          properties:
                            name:
                              type: string
                  responses:
                    '201':
                      description: Created
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings { AddContentTypeHeaders = true };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["post"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().Contain(a => a.Contains("Content-Type"));
    }

    [Test]
    public async Task Generate_Omits_Accept_Header_When_Disabled()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                get:
                  operationId: getTest
                  responses:
                    '200':
                      description: Success
                      content:
                        application/json:
                          schema:
                            type: string
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings { AddAcceptHeaders = false };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().NotContain(a => a.Contains("Accept"));
    }

    [Test]
    public async Task Generate_Includes_Authorization_Header_For_Bearer_Auth()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                get:
                  operationId: getTest
                  security:
                    - bearerAuth: []
                  responses:
                    '200':
                      description: Success
            components:
              securitySchemes:
                bearerAuth:
                  type: http
                  scheme: bearer
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings
        {
            AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method
        };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().Contain(a => a.Contains("Authorization: Bearer"));
    }

    [Test]
    public async Task Generate_Omits_ContentType_For_Multipart()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                post:
                  operationId: uploadFile
                  requestBody:
                    required: true
                    content:
                      multipart/form-data:
                        schema:
                          type: object
                          properties:
                            file:
                              type: string
                              format: binary
                  responses:
                    '200':
                      description: Success
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings { AddContentTypeHeaders = true };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["post"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().NotContain(a => a.Contains("Content-Type"));
    }

    [Test]
    public async Task Generate_Returns_Empty_When_No_Attributes()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                get:
                  operationId: getTest
                  responses:
                    '200':
                      description: Success
                      content:
                        application/json:
                          schema:
                            type: string
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings
        {
            AddAcceptHeaders = false,
            AddContentTypeHeaders = false
        };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodAttributeGenerator(settings, document);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var attributes = sut.Generate(operation, operationModel);

        attributes.Should().BeEmpty();
    }

    [Test]
    public async Task Generate_Lists_All_Accept_Headers_For_Mixed_Streaming_Response()
    {
        string spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test:
                get:
                  operationId: getTest
                  responses:
                    '200':
                      description: Success
                      content:
                        application/json:
                          schema:
                            type: string
                        application/x-ndjson:
                          schema:
                            type: string
            """;

        OpenApiDocument document = await OpenApiYamlDocument.FromYamlAsync(spec);
        RefitGeneratorSettings settings = new RefitGeneratorSettings { AddAcceptHeaders = true };
        CustomCSharpClientGenerator generator = new CSharpClientGeneratorFactory(settings, document).Create();
        MethodAttributeGenerator sut = new MethodAttributeGenerator(settings, document);

        OpenApiOperation operation = document.Paths["/test"]["get"];
        CSharpOperationModel operationModel = generator.CreateOperationModel(operation);
        string[] attributes = sut.Generate(operation, operationModel);

        attributes.Should().ContainSingle(a => a.Contains("Accept"))
            .Which.Should().Contain("application/json")
            .And.Contain("application/x-ndjson");
    }
}
