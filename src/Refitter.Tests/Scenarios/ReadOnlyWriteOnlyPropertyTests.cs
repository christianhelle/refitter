using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Required readOnly and writeOnly properties on a schema used for both request and response.
/// </summary>
[TestClass]
public class ReadOnlyWriteOnlyPropertyTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: RW, version: v1 }
        paths:
          /u:
            post:
              operationId: CreateUser
              requestBody:
                required: true
                content:
                  application/json:
                    schema: { $ref: '#/components/schemas/User' }
              responses:
                '201':
                  description: Created
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/User' }
        components:
          schemas:
            User:
              type: object
              required: [id, password]
              properties:
                id: { type: string, format: uuid, readOnly: true }
                password: { type: string, writeOnly: true }
                name: { type: string }
        """;

    [TestMethod]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task Uses_Same_Contract_For_Request_And_Response()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<User> CreateUser([Body] User body);");
        generatedCode.Should().Contain("public string Password { get; set; }");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code_With_Immutable_Records()
    {
        var generatedCode = await GenerateCode(settings => settings.ImmutableRecords = true);
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
