using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.DependencyInjection;


public class DependencyInjectionGeneratorBranchTests
{
    [Test]
    [Arguments("\n")]
    [Arguments("\r\n")]
    public void Can_Generate_With_BaseUrl_And_XmlDocComments(string newLine)
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = new[]
                {
                    "AuthorizationMessageHandler"
                },
                TransientErrorHandler = TransientErrorHandler.None
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            newLine);

        code.Should().Contain("/// <summary>");
        code.Should().Contain("/// Configures the Refit clients for dependency injection.");
        code.Should().Contain("/// <param name=\"services\">The service collection to configure.</param>");
        code.Should().NotContain("/// <param name=\"baseUrl\">");
        code.Should().Contain(
            $$"""
              public static IServiceCollection ConfigureRefitClients(
                          this IServiceCollection services, 
                          Action<IHttpClientBuilder>? builder = default, 
                          RefitSettings? settings = default)
              """.ReplaceLineEndings(newLine));
        code.Should().Contain($".ConfigureHttpClient(c => c.BaseAddress = new Uri(\"{settings.DependencyInjectionSettings.BaseUrl}\"))");
        code.ContainExpectedDependencyInjectionLineEndings(newLine);
    }

    [Test]
    [Arguments("\n")]
    [Arguments("\r\n")]
    public void Can_Generate_Without_BaseUrl_And_With_XmlDocComments(string newLine)
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = null,
                HttpMessageHandlers = new[]
                {
                    "AuthorizationMessageHandler"
                },
                TransientErrorHandler = TransientErrorHandler.None
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            newLine);

        code.Should().Contain("/// <summary>");
        code.Should().Contain("/// Configures the Refit clients for dependency injection.");
        code.Should().Contain("/// <param name=\"services\">The service collection to configure.</param>");
        code.Should().Contain(
            $$"""
              /// <param name="baseUrl">The base URL for the API clients.</param>
                      /// <param name="builder">Optional action to configure the HTTP client builder.</param>
              """.ReplaceLineEndings(newLine));
        code.Should().Contain(
            """
            public static IServiceCollection ConfigureRefitClients(
                        this IServiceCollection services, 
                        Uri baseUrl, 
                        Action<IHttpClientBuilder>? builder = default, 
                        RefitSettings? settings = default)
            """.ReplaceLineEndings(newLine));
        code.Should().Contain(".ConfigureHttpClient(c => c.BaseAddress = baseUrl)");
        code.ContainExpectedDependencyInjectionLineEndings(newLine);
    }

    [Test]
    [Arguments("\n")]
    [Arguments("\r\n")]
    public void NormalizeLineEndings_Uses_Requested_Newline(string newLine)
    {
        const string input = "first line\r\nsecond line\nthird line\rfourth line";

        string normalized = DependencyInjectionGenerator.NormalizeLineEndings(input, newLine);

        normalized.Should().Be($"first line{newLine}second line{newLine}third line{newLine}fourth line");
        normalized.ContainExpectedDependencyInjectionLineEndings(newLine);
    }

    [Test]
    public void Can_Generate_With_Empty_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().NotContain("AddHttpMessageHandler");
        code.Should().Contain("AddRefitClient<IPetApi>(settings)");
    }

    [Test]
    public void Can_Generate_Polly_With_Empty_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().NotContain("AddHttpMessageHandler");
        code.Should().Contain("using Polly");
        code.Should().Contain("AddPolicyHandler");
        code.Should().Contain("Backoff.DecorrelatedJitterBackoffV2");
    }

    [Test]
    public void Can_Generate_HttpResilience_With_Empty_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.HttpResilience,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().NotContain("AddHttpMessageHandler");
        code.Should().Contain("using Microsoft.Extensions.Http.Resilience");
        code.Should().Contain("AddStandardResilienceHandler");
    }

    [Test]
    public void Can_Generate_TransientErrorHandler_None_With_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = new[]
                {
                    "AuthorizationMessageHandler",
                    "DiagnosticMessageHandler"
                },
                TransientErrorHandler = TransientErrorHandler.None
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().Contain("AddHttpMessageHandler<AuthorizationMessageHandler>()");
        code.Should().Contain("AddHttpMessageHandler<DiagnosticMessageHandler>()");
        code.Should().NotContain("using Polly");
        code.Should().NotContain("using Microsoft.Extensions.Http.Resilience");
        code.Should().NotContain("AddPolicyHandler");
        code.Should().NotContain("AddStandardResilienceHandler");
    }

    [Test]
    public void Can_Generate_Without_XmlDocComments()
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = false,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().NotContain("/// <summary>");
        code.Should().NotContain("/// Extension methods for configuring Refit clients");
    }

    [Test]
    public void Can_Generate_Polly_With_BaseUrl_And_XmlDocComments()
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = new[]
                {
                    "AuthorizationMessageHandler"
                },
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().Contain("/// <summary>");
        code.Should().Contain("/// Configures the Refit clients for dependency injection.");
        code.Should().Contain("using Polly");
        code.Should().Contain("AddPolicyHandler");
        code.Should().NotContain("/// <param name=\"baseUrl\">");
    }

    [Test]
    public void Can_Generate_HttpResilience_Without_BaseUrl_And_With_XmlDocComments()
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = null,
                HttpMessageHandlers = new[]
                {
                    "AuthorizationMessageHandler"
                },
                TransientErrorHandler = TransientErrorHandler.HttpResilience,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().Contain("/// <summary>");
        code.Should().Contain("/// <param name=\"baseUrl\">The base URL for the API clients.</param>");
        code.Should().Contain("Uri baseUrl");
        code.Should().Contain("using Microsoft.Extensions.Http.Resilience");
        code.Should().Contain("AddStandardResilienceHandler");
    }

    [Test]
    public void Does_Not_Generate_Windows_Authentication_By_Default()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().NotContain("ConfigurePrimaryHttpMessageHandler");
        code.Should().NotContain("HttpClientHandler { UseDefaultCredentials = true }");
    }

    [Test]
    public void Can_Generate_Windows_Authentication_For_TransientErrorHandler_None()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None,
                UseWindowsAuthentication = true
            }
        };

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[] { "IPetApi" });

        code.Should().Contain("using System.Net.Http");
        code.Should().Contain(".ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true })");
    }
}

internal static class DependencyInjectionGeneratorAssertions
{
    public static void ContainExpectedDependencyInjectionLineEndings(this string code, string expectedNewLine)
    {
        if (expectedNewLine == "\n")
        {
            code.Should().NotContain("\r\n");
            return;
        }

        code.Replace("\r\n", string.Empty).Should().NotContain("\n");
    }
}
