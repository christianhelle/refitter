using System.Globalization;
using System.Text;

namespace Refitter.Core;

internal static class DependencyInjectionGenerator
{
    public static string Generate(
        RefitGeneratorSettings settings,
        string[] interfaceNames,
        string? newLine = null)
    {
        var iocSettings = settings.DependencyInjectionSettings;
        if (iocSettings is null || !interfaceNames.Any())
            return string.Empty;

        var code = new StringBuilder();
        const string indent = "    ";
        var xmlDocComments = settings.GenerateXmlDocCodeComments;

        newLine ??= Environment.NewLine;

        void AppendGeneratedLine(string value = "") => code.Append(value).Append(newLine);

        var baseUrlParam = string.IsNullOrEmpty(iocSettings.BaseUrl)
            ? $"        /// <param name=\"baseUrl\">The base URL for the API clients.</param>{newLine}        "
            : string.Empty;

        var methodDocs = xmlDocComments
            ? NormalizeLineEndings(
                $"""
                 /// <summary>
                         /// Configures the Refit clients for dependency injection.
                         /// </summary>
                         /// <param name="services">The service collection to configure.</param>
                 {baseUrlParam}/// <param name="builder">Optional action to configure the HTTP client builder.</param>
                         /// <param name="settings">Optional Refit settings to customize serialization and other behaviors.</param>
                         /// <returns>The configured service collection.</returns>
                 {indent}{indent}
                 """,
                newLine)
            : "";

        var methodDeclaration = string.IsNullOrEmpty(iocSettings.BaseUrl)
            ? $"{methodDocs}public static IServiceCollection {iocSettings.ExtensionMethodName}({newLine}            this IServiceCollection services, {newLine}            Uri baseUrl, {newLine}            Action<IHttpClientBuilder>? builder = default, {newLine}            RefitSettings? settings = default)"
            : $"{methodDocs}public static IServiceCollection {iocSettings.ExtensionMethodName}({newLine}            this IServiceCollection services, {newLine}            Action<IHttpClientBuilder>? builder = default, {newLine}            RefitSettings? settings = default)";

        var configureRefitClient = string.IsNullOrEmpty(iocSettings.BaseUrl)
            ? ".ConfigureHttpClient(c => c.BaseAddress = baseUrl)"
            : $".ConfigureHttpClient(c => c.BaseAddress = new Uri(\"{iocSettings.BaseUrl}\"))";

        var usings = NormalizeLineEndings(
            iocSettings.TransientErrorHandler switch
            {
                TransientErrorHandler.Polly
                    => """
                        using System;
                            using System.Net.Http;
                            using Microsoft.Extensions.DependencyInjection;
                            using Polly;
                            using Polly.Contrib.WaitAndRetry;
                            using Polly.Extensions.Http;
                            using Refit;
                        """,
                TransientErrorHandler.HttpResilience
                    => """
                        using System;
                            using System.Net.Http;
                            using Microsoft.Extensions.DependencyInjection;
                            using Microsoft.Extensions.Http.Resilience;
                            using Refit;
                        """,
                _
                    => """
                        using System;
                        using System.Net.Http;
                        using Microsoft.Extensions.DependencyInjection;
                        using Refit;
                        """
            },
            newLine);

        AppendGeneratedLine();
        AppendGeneratedLine();
        AppendGeneratedLine(
            NormalizeLineEndings(
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
                  """",
                newLine));
        foreach (var interfaceName in interfaceNames)
        {
            var clientBuilderName = $"clientBuilder{interfaceName}";
            code.Append(
                NormalizeLineEndings(
                    $$"""
                                  var {{clientBuilderName}} = services
                                      .AddRefitClient<{{interfaceName}}>(settings)
                                      {{(iocSettings.UseWindowsAuthentication ? ".ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true })" : "")}}
                                      {{configureRefitClient}}
                      """,
                    newLine));

            foreach (string httpMessageHandler in iocSettings.HttpMessageHandlers)
            {
                AppendGeneratedLine();
                code.Append($"                .AddHttpMessageHandler<{httpMessageHandler}>()");
            }

            code.Append(";");
            AppendGeneratedLine();

            if (iocSettings.TransientErrorHandler == TransientErrorHandler.Polly)
            {
                var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);
                AppendGeneratedLine();
                AppendGeneratedLine(
                    NormalizeLineEndings(
                        $$"""
                                      {{clientBuilderName}}
                                          .AddPolicyHandler(
                                              HttpPolicyExtensions
                                                  .HandleTransientHttpError()
                                                  .WaitAndRetryAsync(
                                                      Backoff.DecorrelatedJitterBackoffV2(
                                                          TimeSpan.FromSeconds({{durationString}}),
                                                          {{iocSettings.MaxRetryCount}})));
                          """,
                        newLine));
            }
            else if (iocSettings.TransientErrorHandler == TransientErrorHandler.HttpResilience)
            {
                var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);
                AppendGeneratedLine();
                AppendGeneratedLine(
                    NormalizeLineEndings(
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
                          """,
                        newLine));
            }

            AppendGeneratedLine();
            AppendGeneratedLine($"            builder?.Invoke({clientBuilderName});");
            AppendGeneratedLine();
        }

#pragma warning disable RS1035
        code.Remove(code.Length - newLine.Length, newLine.Length);
#pragma warning restore RS1035
        AppendGeneratedLine();
        AppendGeneratedLine("            return services;");
        AppendGeneratedLine("        }");
        AppendGeneratedLine("    }");
        AppendGeneratedLine("}");
        AppendGeneratedLine();
        return code.ToString();
    }

    internal static string NormalizeLineEndings(string value, string newLine)
    {
        var normalized = value.Replace("\r\n", "\n").Replace("\r", "\n");
        return newLine == "\n" ? normalized : normalized.Replace("\n", newLine);
    }
}
