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
            ? "public static IServiceCollection ConfigureRefitClients(this IServiceCollection services, Uri baseUrl)"
            : "public static IServiceCollection ConfigureRefitClients(this IServiceCollection services)";
        
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
            code.Append(
                $$"""
                              services
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
                code.AppendLine();
                code.Append(
                    $$"""
                                      .AddPolicyHandler(
                                          HttpPolicyExtensions
                                              .HandleTransientHttpError()
                                              .WaitAndRetryAsync(
                                                  Backoff.DecorrelatedJitterBackoffV2(
                                                      TimeSpan.FromSeconds({{iocSettings.FirstBackoffRetryInSeconds}}),
                                                      {{iocSettings.PollyMaxRetryCount}}))
                      """);
            }

            code.Append(");");
            code.AppendLine();
            code.AppendLine();
        }
        
        code.Remove(code.Length - 2, 2);
        code.AppendLine();
        code.AppendLine("            return services;");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();
        return code.ToString();
    }
}