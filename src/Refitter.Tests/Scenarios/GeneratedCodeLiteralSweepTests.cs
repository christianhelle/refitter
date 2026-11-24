using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A defence-in-depth sweep over the spec-derived string positions that reach a generated C# literal, carrying
/// the same payload in one document. Each position already has its own regression test; this one asserts that
/// every position stays escaped when they are all present at once and the options that add more literal sinks
/// are combined, so a new sink, or a raw interpolation one of the per-advisory tests does not cover, cannot
/// slip through.
/// </summary>
/// <remarks>
/// Covers GHSA-2p8r-7mx6-gh86 (operation key), GHSA-3fhm-p725-h3g3 (path), GHSA-58x9-vjvp-6mx8 (header
/// parameter name), GHSA-p32v-8v8j-j534 (content-type key), GHSA-7hgj-q592-v892 (network-path route),
/// GHSA-r2hq-86ch-hf3x (response status), and GHSA-h924-jfhp-9vjr (schema property name, discriminator
/// property name, discriminator mapping key, and the default, enum and parameter-default values).
/// </remarks>
[TestClass]
public class GeneratedCodeLiteralSweepTests
{
    // Escapes to "\")]  then a line break, then "#error INJECTED" on its own line, then "//"
    private const string Payload = "\\\")]\\n#error INJECTED\\n//";

    // Refs stay plain so the payload only lands in positions that reach generated code. The path key carries a
    // Unicode line separator instead of a quote, because an unsafe path is refused before generation.
    private const string Template = """"
        {"openapi":"3.0.0",
         "info":{"title":"sweepPAYLOADX","version":"1.0.0"},
         "security":[{"schemePAYLOADX":[]}],
         "paths":{
          "/p\u2028x":{"post":{"operationId":"GetPAYLOADX","tags":["tPAYLOADX"],
            "requestBody":{"content":{"application/jsonPAYLOADX":{"schema":{"type":"string"}}}},
             "parameters":[
               {"name":"hPAYLOADX","in":"header","schema":{"type":"string"}},
               {"name":"qPAYLOADX","in":"query","schema":{"type":"string","default":"aPAYLOADX"}}],
             "responses":{
               "200PAYLOADX":{"description":"okPAYLOADX",
                 "headers":{"X-RPAYLOADX":{"schema":{"type":"string"}}},
                 "content":{"application/jsonPAYLOADX":{"schema":{"$ref":"#/components/schemas/Dto"}}}}}}}},
         "components":{
           "securitySchemes":{"schemePAYLOADX":{"type":"apiKey","in":"header","name":"X-KPAYLOADX"}},
           "schemas":{
             "Dto":{"type":"object","properties":{
               "sPAYLOADX":{"type":"string","default":"aPAYLOADX"},
               "e":{"$ref":"#/components/schemas/Enum"}}},
             "Enum":{"type":"string","enum":["vPAYLOADX"]},
             "Poly":{"type":"object",
               "discriminator":{"propertyName":"kPAYLOADX","mapping":{"mPAYLOADX":"#/components/schemas/Sub"}},
               "properties":{"n":{"type":"string"}}},
             "Sub":{"allOf":[{"$ref":"#/components/schemas/Poly"},{"type":"object","properties":{"b":{"type":"boolean"}}}]}
           }
         }}
        """";

    private static string SweepSpec() => Template.Replace("PAYLOADX", Payload);

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [TestCategory("Integration")]
    public async Task Sweep_Compiles_When_Literal_Sinks_Are_Combined(bool polymorphic, bool dynamicQuerystring)
    {
        string generatedCode = await GenerateCode(SweepSpec(), settings =>
        {
            settings.UsePolymorphicSerialization = polymorphic;
            settings.UseDynamicQuerystringParameters = dynamicQuerystring;
            settings.OptionalParameters = true;
            settings.GenerateStatusCodeComments = true;
        });

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(nameof(PropertyNamingPolicy.PascalCase))]
    [DataRow(nameof(PropertyNamingPolicy.PreserveOriginal))]
    [TestCategory("Integration")]
    public async Task Sweep_Compiles_For_Each_Property_Naming_Policy(string propertyNamingPolicy)
    {
        string generatedCode = await GenerateCode(SweepSpec(), settings =>
            settings.PropertyNamingPolicy = Enum.Parse<PropertyNamingPolicy>(propertyNamingPolicy));

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Sweep_Compiles_For_Native_Records()
    {
        string generatedCode = await GenerateCode(SweepSpec(), settings => settings.ImmutableRecords = true);

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Sweep_Compiles_With_JsonStringEnumMemberName()
    {
        // JsonLibraryVersion 9.0 adds [JsonStringEnumMemberName(@"...")] beside [EnumMember(Value = @"...")],
        // so the enum value reaches one more verbatim string literal
        string generatedCode = await GenerateCode(SweepSpec(), settings =>
            settings.CodeGeneratorSettings = new CodeGeneratorSettings { JsonLibraryVersion = 9.0m });

        BuildHelper.BuildCSharpForTargetFramework("net11.0", generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string spec, Action<RefitGeneratorSettings> configure)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        RefitGeneratorSettings settings = new()
        {
            OpenApiPath = swaggerFile,
            AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter,
        };
        configure(settings);
        RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
