using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Refitter.Core.Settings;

namespace Refitter.Core;

internal static class ApizrRegistrationGenerator
{
    public static string Generate(
        RefitGeneratorSettings settings,
        string[] interfaceNames,
        string? title = null)
    {
        if (!interfaceNames.Any() || !settings.ApizrSettings!.WithRegistrationHelper)
            return string.Empty;

        var hasManyApis = interfaceNames.Length > 1;
        var iocSettings = settings.DependencyInjectionSettings;
        var isDependencyInjectionExtension = iocSettings != null;
        var hasBaseUrl = !string.IsNullOrWhiteSpace(iocSettings?.BaseUrl);
        string? methodName = iocSettings?.ExtensionMethodName;
        if (string.IsNullOrWhiteSpace(methodName) || methodName == DependencyInjectionSettings.DefaultExtensionMethodName)
        {
            var formatedTitle = !string.IsNullOrWhiteSpace(title) ? title!
                .Replace(NamingSettings.DefaultInterfaceName, string.Empty)
                .Replace("Swagger", string.Empty)
                .Replace("OpenAPI", string.Empty)
                .ConvertKebabCaseToPascalCase()
                .ConvertSnakeCaseToPascalCase()
                .ConvertRouteToCamelCase()
                .ConvertSpacesToPascalCase()
                .ConvertColonsToPascalCase()
                .Sanitize()
                .CapitalizeFirstCharacter()
                : string.Empty;

            if(isDependencyInjectionExtension)
                methodName = hasManyApis ? $"Configure{formatedTitle}ApizrManagers" : $"Configure{formatedTitle}ApizrManager";
            else
                methodName = hasManyApis ? $"Build{formatedTitle}ApizrManagers" : $"Build{formatedTitle}ApizrManager";
        }

        var optionsCode =
                $$"""
                optionsBuilder ??= _ => { }; // Default empty options if null
                            optionsBuilder += options => options
                """;
        var optionsCodeBuilder = new StringBuilder(optionsCode);

        if (hasBaseUrl)
        {
            optionsCodeBuilder.AppendLine();
            optionsCodeBuilder.Append(
                $$"""               
                                .WithBaseAddress("{{iocSettings!.BaseUrl}}", ApizrDuplicateStrategy.Ignore)
                """);
        }

        var usings = iocSettings?.TransientErrorHandler switch
        {
            TransientErrorHandler.Polly =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Polly;
                    using Polly.Contrib.WaitAndRetry;
                    using Polly.Extensions.Http;
                """,
            TransientErrorHandler.HttpResilience =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Http.Resilience;
                """,
            _ when isDependencyInjectionExtension =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                """,
            _ =>
                """
                using System;
                """
        };

        var apizrPackages = new List<ApizrPackages>();
        var usingsCodeBuilder = new StringBuilder(usings);
        usingsCodeBuilder.AppendLine();

        if (iocSettings?.HttpMessageHandlers.Length > 0)
        {
            foreach (string httpMessageHandler in iocSettings.HttpMessageHandlers)
            {
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(
                $$"""               
                                .WithDelegatingHandler<{{httpMessageHandler}}>()
                """);
            }
        }

        if (iocSettings?.TransientErrorHandler == TransientErrorHandler.Polly)
        {
            var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);
            optionsCodeBuilder.AppendLine();
            optionsCodeBuilder.Append(
                $$"""
                                .ConfigureHttpClientBuilder(builder => builder
                                    .AddPolicyHandler(
                                        HttpPolicyExtensions
                                            .HandleTransientHttpError()
                                            .WaitAndRetryAsync(
                                                Backoff.DecorrelatedJitterBackoffV2(
                                                    TimeSpan.FromSeconds({{durationString}}),
                                                    {{iocSettings.MaxRetryCount}}))))
                """);
        }
        else if (iocSettings?.TransientErrorHandler == TransientErrorHandler.HttpResilience)
        {
            var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);
            optionsCodeBuilder.AppendLine();
            optionsCodeBuilder.Append(
                $$"""
                                .ConfigureHttpClientBuilder(builder => builder
                                    .AddStandardResilienceHandler(config =>
                                    {
                                        config.Retry = new HttpRetryStrategyOptions
                                        {
                                            UseJitter = true,
                                            MaxRetryAttempts = {{iocSettings.MaxRetryCount}},
                                            Delay = TimeSpan.FromSeconds({{durationString}})
                                        };
                                    }))
                """);
        }

        switch (settings.ApizrSettings.WithCacheProvider)
        {
            case CacheProviderType.Akavache:
                apizrPackages.Add(ApizrPackages.Apizr_Integrations_Akavache);
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Akavache;
                """);
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(
                $$"""               
                                .WithAkavacheCacheHandler()
                """);
                break;
            case CacheProviderType.MonkeyCache:
                apizrPackages.Add(ApizrPackages.Apizr_Integrations_MonkeyCache);
                usingsCodeBuilder.AppendLine(
                $$"""
                    using MonkeyCache;
                """);
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(
                $$"""               
                                .WithCacheHandler(new MonkeyCacheHandler(Barrel.Current))
                """);
                break;
            case CacheProviderType.InMemory when isDependencyInjectionExtension:
                apizrPackages.Add(ApizrPackages.Apizr_Extensions_Microsoft_Caching);
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(
                $$"""
                                .WithInMemoryCacheHandler()
                """);
                break;
            case CacheProviderType.DistributedAsString when isDependencyInjectionExtension:
                apizrPackages.Add(ApizrPackages.Apizr_Extensions_Microsoft_Caching);
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(
                $$"""
                                .WithDistributedCacheHandler<string>()
                """);
                break;
            case CacheProviderType.DistributedAsByteArray when isDependencyInjectionExtension:
                apizrPackages.Add(ApizrPackages.Apizr_Extensions_Microsoft_Caching);
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(
                $$"""
                                .WithDistributedCacheHandler<byte[]>()
                """);
                break;
        }

        switch (settings.ApizrSettings.WithMappingProvider)
        {
            case MappingProviderType.AutoMapper:
                apizrPackages.Add(ApizrPackages.Apizr_Integrations_AutoMapper);
                usingsCodeBuilder.AppendLine(
                $$"""
                    using AutoMapper;
                """);
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(isDependencyInjectionExtension ?
                $$"""
                                .WithAutoMapperMappingHandler()
                """ :
                $$"""               
                                .WithAutoMapperMappingHandler(new MapperConfiguration(config => { /* YOUR_MAPPINGS_HERE */ }))
                """);
                break;
            case MappingProviderType.Mapster:
                apizrPackages.Add(ApizrPackages.Apizr_Integrations_Mapster);
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Mapster;
                """);
                if(isDependencyInjectionExtension)
                    usingsCodeBuilder.AppendLine(
                $$"""
                    using MapsterMapper;
                """);
                optionsCodeBuilder.AppendLine();
                optionsCodeBuilder.Append(isDependencyInjectionExtension ?
                $$"""
                                .WithMapsterMappingHandler()
                """ :
                $$"""               
                                .WithMapsterMappingHandler(new Mapper())
                """);
                break;
        }

        if(settings.ApizrSettings.WithPriority)
        {
            apizrPackages.Add(ApizrPackages.Apizr_Integrations_Fusillade);
            optionsCodeBuilder.AppendLine();
            optionsCodeBuilder.Append(
            $$"""               
                                .WithPriority()
                """);
        }

        if (settings.ApizrSettings.WithOptionalMediation && isDependencyInjectionExtension)
        {
            apizrPackages.Add(ApizrPackages.Apizr_Integrations_Optional);
            usingsCodeBuilder.AppendLine(
                $$"""
                    using MediatR;
                """);
            optionsCodeBuilder.AppendLine();
            optionsCodeBuilder.Append(
                $$"""
                                .WithOptionalMediation()
                """);
        }
        else if (settings.ApizrSettings.WithMediation && isDependencyInjectionExtension)
        {
            apizrPackages.Add(ApizrPackages.Apizr_Integrations_MediatR);
            usingsCodeBuilder.AppendLine(
                $$"""
                    using MediatR;
                """);
            optionsCodeBuilder.AppendLine();
            optionsCodeBuilder.Append(
                $$"""
                                .WithMediation()
                """);
        }

        if (settings.ApizrSettings.WithFileTransfer)
        {
            if (isDependencyInjectionExtension)
            {
                if (settings.ApizrSettings.WithOptionalMediation)
                {
                    apizrPackages.Add(ApizrPackages.Apizr_Integrations_FileTransfer_Optional);
                    optionsCodeBuilder.AppendLine();
                    optionsCodeBuilder.Append(
                $$"""
                                .WithFileTransferOptionalMediation()
                """);
                }
                else if (settings.ApizrSettings.WithMediation)
                {
                    apizrPackages.Add(ApizrPackages.Apizr_Integrations_FileTransfer_MediatR);
                    optionsCodeBuilder.AppendLine();
                    optionsCodeBuilder.Append(
                $$"""
                                .WithFileTransferMediation()
                """);
                }
                else
                {
                    apizrPackages.Add(ApizrPackages.Apizr_Extensions_Microsoft_FileTransfer);
                }
            }
            else
            {
                apizrPackages.Add(ApizrPackages.Apizr_Integrations_FileTransfer);
            }
        }

        apizrPackages.Add(ApizrPackages.Apizr);
        usingsCodeBuilder.AppendLine(
                $$"""
                    using Apizr;
                """);

        if (optionsCodeBuilder.ToString().Contains(".With"))
            optionsCodeBuilder.Append(";");
        else
            optionsCodeBuilder.Clear();

        var packageCodeBuilder = new StringBuilder();
        var packages = apizrPackages.OrderByDescending(p => p).ToList();
        if (packages.Count > 0)
        {
            if (!isDependencyInjectionExtension && (settings.ApizrSettings.WithOptionalMediation ||
                                                    settings.ApizrSettings.WithMediation ||
                                                    settings.ApizrSettings.WithCacheProvider == CacheProviderType.InMemory ||
                                                    settings.ApizrSettings.WithCacheProvider == CacheProviderType.DistributedAsString ||
                                                    settings.ApizrSettings.WithCacheProvider == CacheProviderType.DistributedAsByteArray))
            {
                // ERROR: Asking for Apizr extended features without DependencyInjectionSettings set
                packageCodeBuilder.AppendLine(
                $$"""
                // /!\ ERROR =========
                    // Your configuration asked for some Apizr extended features but you did not configure the DependencyInjectionSettings.
                    // Please either set DependencyInjectionSettings property to get the extended features enabled 
                    // or disable Apizr extended features to get a static builder instead. 
                    // Then try to generate again.
                    // ===================
                    //
                    // Please make sure to complete the following steps resulting from your configuration:
                """);
            }
            else
            {
                packageCodeBuilder.AppendLine(
                $$"""
                // Please make sure to complete the following steps resulting from your configuration:
                """);
            }
            for (int i = 0; i < packages.Count; i++)
            {
                if (i == 0 || !packages[i - 1].HasFlag(packages[i]))
                {
                    packageCodeBuilder.AppendLine(
                $$"""
                    // - {{packages[i].ToDescription()}}
                """);
                }
            }

            if (settings.ApizrSettings.WithFileTransfer)
            {
                packageCodeBuilder.AppendLine(
                $$"""
                    // - Add your file transfer manager while calling {{methodName}} method thanks to its options builder parameter
                """);
            }
        }

        // Code
        var code = new StringBuilder();
        code.AppendLine();
        code.AppendLine();

        #region Extended

        if (isDependencyInjectionExtension)
        {
            if (hasBaseUrl)
            {
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Apizr.Configuring;
                """);
            }

            #region Registry

            if (hasManyApis)
            {
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Apizr.Extending.Configuring.Common;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{packageCodeBuilder}}
                    {{usingsCodeBuilder}}
                  
                    public static partial class IServiceCollectionExtensions
                    {
                """);

                if (settings.GenerateXmlDocCodeComments)
                {
                    code.AppendLine(
                $$"""
                        /// <summary>
                        /// Register all your Apizr managed apis with common shared options.
                        /// You may call WithConfiguration option to adjust settings to your need.
                        /// </summary>
                        /// <param name="optionsBuilder">Adjust common shared options</param>
                        /// <returns></returns>
                """); 
                }

                code.AppendLine(
                $$"""
                        public static IServiceCollection {{methodName}}(
                            this IServiceCollection services,
                """);

                code.AppendLine(hasBaseUrl ?
                $$"""
                            Action<IApizrExtendedCommonOptionsBuilder>? optionsBuilder = null)
                        {
                """ :
                $$"""
                            Action<IApizrExtendedCommonOptionsBuilder> optionsBuilder)
                        {
                """);

                code.AppendLine(
                $$"""
                            {{optionsCodeBuilder}}
                            
                            return services.AddApizr(
                                registry => registry
                """);
                for (int i = 0; i < interfaceNames.Length; i++)
                {
                    if(i > 0)
                        code.AppendLine();

                    code.Append(
                $"                  .AddManagerFor<{interfaceNames[i]}>()");
                }

                code.Append(",");
                code.AppendLine();
                code.AppendLine(
                $"""
                                optionsBuilder);
                """);

                code.AppendLine();
#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
            }

            #endregion

            #region Manager

            else
            {
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Apizr.Extending.Configuring.Manager;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{packageCodeBuilder}}
                    {{usingsCodeBuilder}}

                    public static partial class IServiceCollectionExtensions
                    {
                """);

                if (settings.GenerateXmlDocCodeComments)
                {
                    code.AppendLine(
                $$"""
                        /// <summary>
                        /// Register your Apizr managed api with common shared options.
                        /// You may call WithConfiguration option to adjust settings to your need.
                        /// </summary>
                        /// <param name="optionsBuilder">Adjust common shared options</param>
                        /// <returns></returns>
                """); 
                }

                code.AppendLine(
                $$"""
                        public static IServiceCollection {{methodName}}(
                            this IServiceCollection services,
                """);

                code.AppendLine(hasBaseUrl ? 
                $$"""
                            Action<IApizrExtendedManagerOptionsBuilder>? optionsBuilder = null)
                        {
                """ : 
                $$"""
                            Action<IApizrExtendedManagerOptionsBuilder> optionsBuilder)
                        {
                """);

                code.AppendLine(
                $$"""
                            {{optionsCodeBuilder}}
                                 
                            return services.AddApizrManagerFor<{{interfaceNames[0]}}>(optionsBuilder);
                """);

#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
            } 

            #endregion
        }

        #endregion

        #region Static

        else
        {
            #region Registry

            if (hasManyApis)
            {
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Apizr.Configuring.Registry;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{packageCodeBuilder}}
                    {{usingsCodeBuilder}}
                  
                    public static partial class ApizrRegistration
                    {
                """);

                if (settings.GenerateXmlDocCodeComments)
                {
                    code.AppendLine(
                $$"""
                        /// <summary>
                        /// Build a registry with your Apizr managed apis and common shared options.
                        /// You may call WithConfiguration option to adjust settings to your need.
                        /// </summary>
                        /// <param name="optionsBuilder">Adjust common shared options</param>
                        /// <returns></returns>
                """); 
                }

                code.AppendLine(
                $$"""
                        public static IApizrRegistry {{methodName}}(Action<IApizrCommonOptionsBuilder> optionsBuilder)
                        {
                            {{optionsCodeBuilder}}
                                  
                            return ApizrBuilder.Current.CreateRegistry(
                                registry => registry
                """);
                for (int i = 0; i < interfaceNames.Length; i++)
                {
                    if (i > 0)
                        code.AppendLine();

                    code.Append(
                $"                  .AddManagerFor<{interfaceNames[i]}>()");
                }

                code.Append(",");
                code.AppendLine();
                code.AppendLine(
                $"""
                                optionsBuilder);
                """);

#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
            }

            #endregion

            #region Manager

            else
            {
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Apizr.Configuring.Manager;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{packageCodeBuilder}}
                    {{usingsCodeBuilder}}
                      
                    public static partial class ApizrRegistration
                    {
                """);

                if (settings.GenerateXmlDocCodeComments)
                {
                    code.AppendLine(
                $$"""
                        /// <summary>
                        /// Build your Apizr managed api with common shared options.
                        /// You may call WithConfiguration option to adjust settings to your need.
                        /// </summary>
                        /// <param name="optionsBuilder">Adjust common shared options</param>
                        /// <returns></returns>
                """); 
                }

                code.AppendLine(
                $$"""
                        public static IApizrManager<{{interfaceNames[0]}}> {{methodName}}(Action<IApizrManagerOptionsBuilder> optionsBuilder)
                        {
                            {{optionsCodeBuilder}}
                                  
                            return ApizrBuilder.Current.CreateManagerFor<{{interfaceNames[0]}}>(optionsBuilder);  
                """);

#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
            } 

            #endregion
        }

        #endregion

        code.AppendLine();
        code.AppendLine(
                $$"""
                        }
                """);
        code.AppendLine(
                $$"""
                    }
                """);
        code.AppendLine(
                $$"""
                }
                """);
        code.AppendLine();
        return code.ToString();
    }
}