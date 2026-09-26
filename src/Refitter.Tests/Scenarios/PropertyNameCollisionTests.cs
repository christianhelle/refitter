using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Property names that collide after normalization or with generated members (#1268).
/// </summary>
public class PropertyNameCollisionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: PropNames, version: v1 }
        paths:
          /o:
            get:
              operationId: GetOrder
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/Order' } } } } }
        components:
          schemas:
            Order:
              type: object
              properties:
                order: { type: string }
                user_name: { type: string }
                userName: { type: string }
                AdditionalProperties: { type: string }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Unique_Member_Names_And_Keeps_Wire_Names()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().MatchRegex(@"JsonPropertyName\(""order""\)\]\s*public string Order2 \{");
        generatedCode.Should().MatchRegex(@"JsonPropertyName\(""user_name""\)\]\s*public string UserName \{");
        generatedCode.Should().MatchRegex(@"JsonPropertyName\(""userName""\)\]\s*public string UserName2 \{");
        generatedCode.Should().MatchRegex(@"JsonPropertyName\(""AdditionalProperties""\)\]\s*public string AdditionalProperties2 \{");
        generatedCode.Should().Contain("public IDictionary<string, object> AdditionalProperties");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Preserved_Property_Names_And_Immutable_Records()
    {
        var generatedCode = await GenerateCode(settings =>
        {
            settings.PropertyNamingPolicy = PropertyNamingPolicy.PreserveOriginal;
            settings.ImmutableRecords = true;
        });
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
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
