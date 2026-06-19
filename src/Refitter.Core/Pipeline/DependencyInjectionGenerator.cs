using System.Globalization;
using System.Text;

namespace Refitter.Core;

internal static class DependencyInjectionGenerator
{
    public static string Generate(
        RefitGeneratorSettings settings,
        string[] interfaceNames)
    {
        var iocSettings = settings.DependencyInjectionSettings;
        if (iocSettings is null || !interfaceNames.Any())
            return string.Empty;

        var code = new StringBuilder();
        const string indent = "    ";
        var xmlDocComments = settings.GenerateXmlDocCodeComments;

        var methodDocs = xmlDocComments
            ? $"""
               /// <summary>
                       /// Configures the Refit clients for dependency injection.
                       /// </summary>
                       /// <param name="services">The service collection to configure.</param>
               {(string.IsNullOrEmpty(iocSettings.BaseUrl) ? "        /// <param name=\"baseUrl\">The base URL for the API clients.</param>\r\n        " : "")}/// <param name="builder">Optional action to configure the HTTP client builder.</param>
                       /// <param name="settings">Optional Refit settings to customize serialization and other behaviors.</param>
                       /// <returns>The configured service collection.</returns>
               {indent}{indent}
               """
            : "";

        var methodDeclaration = string.IsNullOrEmpty(iocSettings.BaseUrl)
            ? $"{methodDocs}public static IServiceCollection {iocSettings.ExtensionMethodName}(\r\n            this IServiceCollection services, \r\n            Uri baseUrl, \r\n            Action<IHttpClientBuilder>? builder = default, \r\n            RefitSettings? settings = default)"
            : $"{methodDocs}public static IServiceCollection {iocSettings.ExtensionMethodName}(\r\n            this IServiceCollection services, \r\n            Action<IHttpClientBuilder>? builder = default, \r\n            RefitSettings? settings = default)";

        var configureRefitClient = string.IsNullOrEmpty(iocSettings.BaseUrl)
            ? ".ConfigureHttpClient(c => c.BaseAddress = baseUrl)"
            : $".ConfigureHttpClient(c => c.BaseAddress = new Uri(\"{iocSettings.BaseUrl}\"))";

        var usings = iocSettings.TransientErrorHandler switch
        {
            TransientErrorHandler.Polly
                => """
                    using System;
                        using Microsoft.Extensions.DependencyInjection;
                        using Polly;
                        using Polly.Contrib.WaitAndRetry;
                        using Polly.Extensions.Http;
                        using Refit;
                    """,
            TransientErrorHandler.HttpResilience
                => """
                    using System;
                        using Microsoft.Extensions.DependencyInjection;
                        using Microsoft.Extensions.Http.Resilience;
                        using Refit;
                    """,
            _
                => """
                    using System;
                        using Microsoft.Extensions.DependencyInjection;
                        using Refit;
                    """
        };

        code.AppendLine();
        code.AppendLine();
        code.AppendLine(
            $$""""
              #nullable enable
              namespace {{settings.Namespace}}
              {
                  {{usings}}
              {{(xmlDocComments ? """

                  /// <summary>
                  /// Extension methods for configuring Refit clients in the service collection.
                  /// </summary>
              """ : "")}}
                  public static partial class IServiceCollectionExtensions
                  {
                      {{methodDeclaration}}
                      {
              """");
        foreach (var interfaceName in interfaceNames)
        {
            var clientBuilderName = $"clientBuilder{interfaceName}";
            code.Append(
                $$"""
                              var {{clientBuilderName}} = services
                                  .AddRefitClient<{{interfaceName}}>(settings)
                                  {{configureRefitClient}}
                  """);

            foreach (string httpMessageHandler in iocSettings.HttpMessageHandlers)
            {
                code.AppendLine();
                code.Append($"                .AddHttpMessageHandler<{httpMessageHandler}>()");
            }

            code.Append(";");
            code.AppendLine();

            if (iocSettings.TransientErrorHandler == TransientErrorHandler.Polly)
            {
                var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);
                code.AppendLine();
                code.AppendLine(
                    $$"""
                                  {{clientBuilderName}}
                                      .AddPolicyHandler(
                                          HttpPolicyExtensions
                                              .HandleTransientHttpError()
                                              .WaitAndRetryAsync(
                                                  Backoff.DecorrelatedJitterBackoffV2(
                                                      TimeSpan.FromSeconds({{durationString}}),
                                                      {{iocSettings.MaxRetryCount}})));
                      """);
            }
            else if (iocSettings.TransientErrorHandler == TransientErrorHandler.HttpResilience)
            {
                var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);
                code.AppendLine();
                code.AppendLine(
                    $$"""
                                  {{clientBuilderName}}
                                      .AddStandardResilienceHandler(config =>
                                      {
                                          config.Retry = new HttpRetryStrategyOptions
                                          {
                                              UseJitter = true,
                                              MaxRetryAttempts = {{iocSettings.MaxRetryCount}},
                                              Delay = TimeSpan.FromSeconds({{durationString}})
                                          };
                                      });
                      """);
            }

            code.AppendLine();
            code.AppendLine($"            builder?.Invoke({clientBuilderName});");
            code.AppendLine();
        }

#pragma warning disable RS1035
        code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
        code.AppendLine();
        code.AppendLine("            return services;");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();
        return code.ToString();
    }
}
