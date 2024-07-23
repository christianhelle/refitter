using System.Globalization;
using System.Text;

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
        optionsCodeBuilder.AppendLine();

        if (hasBaseUrl)
        {
            optionsCodeBuilder.Append(
                $$"""               
                            .WithBaseAddress("{{iocSettings!.BaseUrl}}", ApizrDuplicateStrategy.Ignore)
                """);
        }

        if (hasBaseUrl)
        {
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
                    using Apizr; // Install-Package Apizr
                """,
            TransientErrorHandler.HttpResilience =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Http.Resilience;
                    using Apizr; // Install-Package Apizr
                """,
            _ when isDependencyInjectionExtension =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Apizr; // Install-Package Apizr
                """,
            _ =>
                """
                using System;
                    using Apizr; // Install-Package Apizr
                """
        };

        var usingsCodeBuilder = new StringBuilder(usings);
        usingsCodeBuilder.AppendLine();

        switch (settings.ApizrSettings.WithCacheProvider)
        {
            case CacheProviderType.Akavache:
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Akavache; // Install-Package Apizr.Integrations.Akavache
                """);
                optionsCodeBuilder.AppendLine(
                $$"""               
                            .WithAkavacheCacheHandler()
                """);
                break;
            case CacheProviderType.MonkeyCache:
                usingsCodeBuilder.AppendLine(
                $$"""
                    using MonkeyCache; // Install-Package Apizr.Integrations.MonkeyCache
                """);
                optionsCodeBuilder.AppendLine(
                $$"""               
                            .WithCacheHandler(new MonkeyCacheHandler(Barrel.Current)) // Write somewhere else: Barrel.ApplicationId = "YOUR_APPLICATION_NAME";
                """);
                break;
            case CacheProviderType.InMemory:
                usingsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                    // Install-Package Apizr.Extensions.Microsoft.Caching and Microsoft.Extensions.Caching.Memory
                """ :
                $$"""
                    // You have to set DependencyInjectionSettings to use in memory cache provider
                """);
                optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithInMemoryCacheHandler()
                """ :
                $$"""
                            // You have to set DependencyInjectionSettings to use in memory cache provider
                """);
                break;
            case CacheProviderType.Distributed:
                usingsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                    // Install-Package Apizr.Extensions.Microsoft.Caching and any Microsoft Distributed Caching compliant package
                """ :
                $$"""
                    // You have to set DependencyInjectionSettings to use distributed cache provider
                """);
                optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithDistributedCacheHandler<string>() // Replace string with byte[] if needed
                """ :
                $$"""
                            // You have to set DependencyInjectionSettings to use distributed cache provider
                """);
                break;
        }

        switch (settings.ApizrSettings.WithMappingProvider)
        {
            case MappingProviderType.AutoMapper:
                usingsCodeBuilder.AppendLine(
                $$"""
                    using AutoMapper; // Install-Package Apizr.Integrations.AutoMapper
                """);
                optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithAutoMapperMappingHandler() // Don't forget to register AutoMapper itself too
                """ :
                $$"""               
                            .WithAutoMapperMappingHandler(Your_MapperConfiguration) // Replace Your_MapperConfiguration with your own AutoMapper's MapperConfiguration instance
                """);
                break;
            case MappingProviderType.Mapster:
                usingsCodeBuilder.AppendLine(
                $$"""
                    using Mapster; // Install-Package Apizr.Integrations.Mapster
                """);
                if(isDependencyInjectionExtension)
                    usingsCodeBuilder.AppendLine(
                $$"""
                    using MapsterMapper;
                """);
                optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithMapsterMappingHandler() // Don't forget to register Mapster itself too
                """ :
                $$"""               
                            .WithMapsterMappingHandler(new Mapper())
                """);
                break;
        }

        if(settings.ApizrSettings.WithPriority)
        {
            usingsCodeBuilder.AppendLine(
                $$"""
                    // Install-Package Apizr.Integrations.Fusillade
                """);
            optionsCodeBuilder.AppendLine(
                $$"""               
                            .WithPriority()
                """);
        }

        if (settings.ApizrSettings.WithOptionalMediation)
        {
            usingsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                    using MediatR; // Install-Package Apizr.Integrations.Optional
                """ :
                $$"""
                    // You have to set DependencyInjectionSettings to use MediatR with optional result
                """);
            optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithOptionalMediation() // Don't forget to register MediatR itself too
                """ :
                $$"""
                            // You have to set DependencyInjectionSettings to use MediatR with optional result
                """);
        }
        else if (settings.ApizrSettings.WithMediation)
        {
            usingsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                    using MediatR; // Install-Package Apizr.Integrations.MediatR
                """ :
                $$"""
                    // You have to set DependencyInjectionSettings to use MediatR
                """);
            optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithMediation() // Don't forget to register MediatR itself too
                """ :
                $$"""
                            // You have to set DependencyInjectionSettings to use MediatR
                """);
        }

        if (settings.ApizrSettings.WithFileTransfer)
        {
            if (settings.ApizrSettings.WithOptionalMediation)
            {
                usingsCodeBuilder.AppendLine(isDependencyInjectionExtension ? 
                $$"""
                    // Install-Package Apizr.Integrations.FileTransfer.Optional
                """ : 
                $$"""
                    // You have to set DependencyInjectionSettings to use file transfer with optional mediation
                """);
                optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithFileTransferOptionalMediation() // Don't forget to register MediatR itself too
                """ :
                $$"""
                            // You have to set DependencyInjectionSettings to use file transfer with optional mediation
                """);
            }
            else if (settings.ApizrSettings.WithMediation)
            {

                usingsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                    // Install-Package Apizr.Integrations.FileTransfer.MediatR
                """ :
                $$"""
                    // You have to set DependencyInjectionSettings to use file transfer with mediation
                """);
                optionsCodeBuilder.AppendLine(isDependencyInjectionExtension ?
                $$"""
                            .WithFileTransferMediation() // Don't forget to register MediatR itself too
                """ :
                $$"""
                            // You have to set DependencyInjectionSettings to use file transfer with mediation
                """);
            }
            else if (isDependencyInjectionExtension)
            {
                usingsCodeBuilder.AppendLine(
                $$"""
                    // Install-Package Apizr.Extensions.Microsoft.FileTransfer
                """);
                usingsCodeBuilder.AppendLine(
                $$"""
                    // Please register your file transfer manager with options builder while calling {{methodName}}
                """);
            }
            else
            {
                usingsCodeBuilder.AppendLine(
                $$"""
                    // Install-Package Apizr.Integrations.FileTransfer
                """);
                usingsCodeBuilder.AppendLine(
                $$"""
                    // Please register your file transfer manager with options builder while calling {{methodName}}
                """);
            }
        }

        if(optionsCodeBuilder.ToString().Contains(".With"))
            optionsCodeBuilder.Append(";");
        else
            optionsCodeBuilder.Clear();

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
                    using Apizr.Extending.Configuring.Common; // Install-Package Apizr.Extensions.Microsoft.DependencyInjection
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usingsCodeBuilder}}
                  
                    public static partial class IServiceCollectionExtensions
                    {
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

                code.Append(
                $$"""
                            {{optionsCodeBuilder}}
                            
                            return services.AddApizr(
                                registry => registry
                """);
                foreach (var interfaceName in interfaceNames)
                {
                    code.AppendLine();
                    code.Append(
                $"                  .AddManagerFor<{interfaceName}>()");
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
                    using Apizr.Extending.Configuring.Manager; // Install-Package Apizr.Extensions.Microsoft.DependencyInjection
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usingsCodeBuilder}}

                    public static partial class IServiceCollectionExtensions
                    {
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

                code.AppendLine();
                code.Append(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usingsCodeBuilder}}
                  
                    public static partial class ApizrRegistration
                    {
                        public static IApizrRegistry {{methodName}}(Action<IApizrCommonOptionsBuilder> optionsBuilder)
                        {
                            {{optionsCodeBuilder}}
                            
                            return ApizrBuilder.Current.CreateRegistry(
                                registry => registry
                """);
                foreach (var interfaceName in interfaceNames)
                {
                    code.AppendLine();
                    code.Append(
                $"                  .AddManagerFor<{interfaceName}>()");
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
                    {{usingsCodeBuilder}}
                      
                    public static partial class ApizrRegistration
                    {
                        public static IApizrManager<{{interfaceNames[0]}}> {{methodName}}(Action<IApizrManagerOptionsBuilder> optionsBuilder)
                        {
                            {{optionsCodeBuilder}}
                            
                            return ApizrBuilder.Current.CreateManagerFor<{interfaceNames[0]}>(optionsBuilder);  
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