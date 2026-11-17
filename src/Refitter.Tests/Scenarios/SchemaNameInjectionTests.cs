using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

// Regression tests for GHSA-h924-jfhp-9vjr. Schema property names, discriminator property names and
// discriminator mapping keys are rendered into C# string literals by NJsonSchema's class template.
// Each payload closes the literal and puts an #error directive on its own line, so the generated code
// only compiles if the payload stays inside the string literal.
[TestClass]
public class SchemaNameInjectionTests
{
    private const string PropertyNameSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets","responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Pet"}}}}}}}},
         "components":{"schemas":{"Pet":{"type":"object","properties":{
           "name":{"type":"string"},
           "a\")]\n#error INJECTED-PROPERTY\n//":{"type":"string"}}}}}}
        """;

    private const string DiscriminatorSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets","responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Pet"}}}}}}}},
         "components":{"schemas":{
           "Pet":{"type":"object","discriminator":{"propertyName":"petType\")]\n#error INJECTED-DISCRIMINATOR\n//"},"properties":{"name":{"type":"string"}}},
           "Dog":{"allOf":[{"$ref":"#/components/schemas/Pet"},{"type":"object","properties":{"bark":{"type":"boolean"}}}]}}}}
        """;

    private const string MappingKeySpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets","responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Pet"}}}}}}}},
         "components":{"schemas":{
           "Pet":{"type":"object","discriminator":{"propertyName":"petType","mapping":{"Dog\")]\n#error INJECTED-MAPPING\n//":"#/components/schemas/Dog"}},"properties":{"name":{"type":"string"}}},
           "Dog":{"allOf":[{"$ref":"#/components/schemas/Pet"},{"type":"object","properties":{"bark":{"type":"boolean"}}}]}}}}
        """;

    [TestMethod]
    public async Task Property_Name_Is_Escaped()
    {
        string generatedCode = await GenerateCode(PropertyNameSpec);
        generatedCode.Should().Contain("JsonPropertyName(\"a\\\")]");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Property_Name_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(PropertyNameSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Discriminator_Property_Name_Is_Escaped(bool usePolymorphicSerialization)
    {
        string generatedCode = await GenerateCode(DiscriminatorSpec, s => s.UsePolymorphicSerialization = usePolymorphicSerialization);
        generatedCode.Should().Contain("\"petType\\\")]");
        generatedCode.Should().NotContain("\"petType\")]");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Discriminator_Property_Name_Injection_Is_Inert_And_Compiles(bool usePolymorphicSerialization)
    {
        string generatedCode = await GenerateCode(DiscriminatorSpec, s => s.UsePolymorphicSerialization = usePolymorphicSerialization);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Discriminator_Mapping_Key_Is_Escaped(bool usePolymorphicSerialization)
    {
        string generatedCode = await GenerateCode(MappingKeySpec, s => s.UsePolymorphicSerialization = usePolymorphicSerialization);
        generatedCode.Should().Contain("\"Dog\\\")]");
        generatedCode.Should().NotContain("\"Dog\")]");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Discriminator_Mapping_Key_Injection_Is_Inert_And_Compiles(bool usePolymorphicSerialization)
    {
        string generatedCode = await GenerateCode(MappingKeySpec, s => s.UsePolymorphicSerialization = usePolymorphicSerialization);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string spec, Action<RefitGeneratorSettings>? configure = null)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        RefitGeneratorSettings settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);
        RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
