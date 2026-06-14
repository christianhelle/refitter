using FluentAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class MethodSignatureGeneratorTests
{
    [Test]
    public async Task Generate_Returns_Route_Parameter()
    {
        var spec = """
            openapi: 3.0.0
            info:
              title: Test
              version: "1.0"
            paths:
              /test/{id}:
                get:
                  operationId: getTest
                  parameters:
                    - name: id
                      in: path
                      required: true
                      schema:
                        type: integer
                  responses:
                    '200':
                      description: Success
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodSignatureGenerator(settings);

        var operation = document.Paths["/test/{id}"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var (parametersString, _, _) = sut.Generate(operationModel, operation, string.Empty);

        parametersString.Should().Contain("int id");
    }

    [Test]
    public async Task Generate_Returns_Query_Parameter()
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
                  parameters:
                    - name: search
                      in: query
                      schema:
                        type: string
                  responses:
                    '200':
                      description: Success
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodSignatureGenerator(settings);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var (parametersString, _, _) = sut.Generate(operationModel, operation, string.Empty);

        parametersString.Should().Contain("string search");
    }

    [Test]
    public async Task Generate_Includes_Body_Parameter()
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
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodSignatureGenerator(settings);

        var operation = document.Paths["/test"]["post"];
        var operationModel = generator.CreateOperationModel(operation);
        var (parametersString, _, _) = sut.Generate(operationModel, operation, string.Empty);

        parametersString.Should().Contain("body");
    }

    [Test]
    public async Task Generate_With_CancellationToken_Includes_CancellationToken()
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
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings { UseCancellationTokens = true };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodSignatureGenerator(settings);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var (parametersString, _, _) = sut.Generate(operationModel, operation, string.Empty);

        parametersString.Should().Contain("CancellationToken");
    }

    [Test]
    public async Task Generate_With_DynamicQuerystring_Returns_Generated_Code()
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
                  parameters:
                    - name: page
                      in: query
                      schema:
                        type: integer
                    - name: limit
                      in: query
                      schema:
                        type: integer
                  responses:
                    '200':
                      description: Success
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodSignatureGenerator(settings);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var (_, _, dynamicQs) = sut.Generate(operationModel, operation, "TestQueryParams");

        dynamicQs.Should().NotBeNull();
    }

    [Test]
    public async Task Generate_With_Apizr_RequestOptions_Includes_Options_Parameter()
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
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRequestOptions = true }
        };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodSignatureGenerator(settings);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var (parametersString, _, _) = sut.Generate(operationModel, operation, string.Empty);

        parametersString.Should().Contain("IApizrRequestOptions");
    }

    [Test]
    public async Task Generate_Returns_Parameters_List_For_Apizr_Overload_Filtering()
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
                  parameters:
                    - name: search
                      in: query
                      required: false
                      schema:
                        type: string
                  responses:
                    '200':
                      description: Success
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings { OptionalParameters = true };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new MethodSignatureGenerator(settings);

        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        var (_, parameters, _) = sut.Generate(operationModel, operation, string.Empty);

        parameters.Should().NotBeEmpty();
    }
}
