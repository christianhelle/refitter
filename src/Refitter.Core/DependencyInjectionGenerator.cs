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

        var methodDeclaration = string.IsNullOrEmpty(iocSettings.BaseUrl)
            ? $"public static IServiceCollection {iocSettings.ExtensionMethodName}(this IServiceCollection services, Uri baseUrl, Action<IHttpClientBuilder>? builder = default)"
            : $"public static IServiceCollection {iocSettings.ExtensionMethodName}(this IServiceCollection services, Action<IHttpClientBuilder>? builder = default)";
        
        var configureRefitClient = string.IsNullOrEmpty(iocSettings.BaseUrl)
            ? ".ConfigureHttpClient(c => c.BaseAddress = baseUrl)"
            : $".ConfigureHttpClient(c => c.BaseAddress = new Uri(\"{iocSettings.BaseUrl}\"))";
        
        var usings = iocSettings.UsePolly
            ? """
              using System;
                  using Microsoft.Extensions.DependencyInjection;
                  using Polly;
                  using Polly.Contrib.WaitAndRetry;
                  using Polly.Extensions.Http;
              """
            : """
              using System;
                  using Microsoft.Extensions.DependencyInjection;
              """;

        code.AppendLine();
        code.AppendLine();
        code.AppendLine(
            $$""""
              #nullable enable
              namespace {{settings.Namespace}}
              {
                  {{usings}}

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
                                  .AddRefitClient<{{interfaceName}}>()
                                  {{configureRefitClient}}
                  """);

            foreach (string httpMessageHandler in iocSettings.HttpMessageHandlers)
            {
                code.AppendLine();
                code.Append($"                .AddHttpMessageHandler<{httpMessageHandler}>()");
            }

            if (iocSettings.UsePolly)
            {
                var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);
                code.AppendLine();
                code.Append(
                    $$"""
                                      .AddPolicyHandler(
                                          HttpPolicyExtensions
                                              .HandleTransientHttpError()
                                              .WaitAndRetryAsync(
                                                  Backoff.DecorrelatedJitterBackoffV2(
                                                      TimeSpan.FromSeconds({{durationString}}),
                                                      {{iocSettings.PollyMaxRetryCount}})))
                      """);
            }

            code.Append(";");
            code.AppendLine();
            code.Append($"            builder?.Invoke({clientBuilderName});");

            code.AppendLine();
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