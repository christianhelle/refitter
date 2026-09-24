using System.Text.RegularExpressions;
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Apizr;

/// <summary>
/// Apizr registration code must not emit duplicate using directives for the transient error handler (#1264).
/// </summary>
public class ApizrTransientErrorHandlerUsingsTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Resilience, version: v1 }
        paths:
          /ping:
            get:
              operationId: Ping
              responses: { '204': { description: ok } }
        """;

    [Test]
    [Skip("https://github.com/christianhelle/refitter/issues/1264")]
    [Arguments(TransientErrorHandler.HttpResilience, "using Microsoft.Extensions.Http.Resilience;")]
    [Arguments(TransientErrorHandler.Polly, "using Polly.Extensions.Http;")]
    public async Task Emits_Transient_Error_Handler_Using_Once(TransientErrorHandler handler, string usingDirective)
    {
        var generatedCode = await GenerateCode(handler);
        Regex.Matches(generatedCode, Regex.Escape(usingDirective)).Count.Should().Be(1);
    }

    [Test]
    [Category("Integration")]
    [Arguments(TransientErrorHandler.HttpResilience)]
    [Arguments(TransientErrorHandler.Polly)]
    public async Task Can_Build_Generated_Code(TransientErrorHandler handler)
    {
        var generatedCode = await GenerateCode(handler);
        BuildHelper.BuildApizrCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(TransientErrorHandler handler)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://example.com",
                TransientErrorHandler = handler
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
