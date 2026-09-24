using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Callbacks, links and api key, bearer and OAuth2 security schemes.
/// </summary>
public class CallbacksLinksAndSecuritySchemeTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Callbacks, version: v1 }
        security: [ { apiKey: [] } ]
        paths:
          /subscribe:
            post:
              operationId: Subscribe
              security: [ { bearer: [] }, {} ]
              requestBody:
                content: { application/json: { schema: { type: object, properties: { callbackUrl: { type: string, format: uri } } } } }
              callbacks:
                onEvent:
                  '{$request.body#/callbackUrl}':
                    post:
                      requestBody: { content: { application/json: { schema: { $ref: '#/components/schemas/Event' } } } }
                      responses: { '200': { description: ok } }
              responses:
                '201':
                  description: ok
                  links:
                    GetSub: { operationId: GetSub, parameters: { id: '$response.body#/id' } }
                  content: { application/json: { schema: { type: object, properties: { id: { type: string } } } } }
          /subscribe/{id}:
            get:
              operationId: GetSub
              parameters: [ { name: id, in: path, required: true, schema: { type: string } } ]
              responses: { '200': { description: ok } }
        components:
          securitySchemes:
            apiKey: { type: apiKey, in: header, name: X-API-Key }
            bearer: { type: http, scheme: bearer }
            oauth:
              type: oauth2
              flows: { clientCredentials: { tokenUrl: 'https://x/token', scopes: { read: r } } }
          schemas:
            Event: { type: object, properties: { name: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Ignores_Callbacks()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Response> Subscribe([Body] Body body);");
        generatedCode.Should().Contain("Task GetSub(string id);");
        generatedCode.Should().NotContain("[Post(\"{$request.body#/callbackUrl}\")]");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_By_Tag()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByTag);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
