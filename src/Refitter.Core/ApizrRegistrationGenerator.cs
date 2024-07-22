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
        if (!interfaceNames.Any())
            return string.Empty;

        var hasManyApis = interfaceNames.Length > 1;
        var iocSettings = settings.DependencyInjectionSettings;
        var isDependencyInjectionExtension = iocSettings != null;
        string? methodName = iocSettings?.ExtensionMethodName;
        if (string.IsNullOrWhiteSpace(methodName) || (settings.UseApizr && methodName == DependencyInjectionSettings.DefaultExtensionMethodName))
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

        var code = new StringBuilder();

        var usings = iocSettings?.TransientErrorHandler switch
        {
            TransientErrorHandler.Polly =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Polly;
                    using Polly.Contrib.WaitAndRetry;
                    using Polly.Extensions.Http;
                    using Apizr;
                """,
            TransientErrorHandler.HttpResilience =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Http.Resilience;
                    using Apizr;
                """,
            _ when isDependencyInjectionExtension => """
                 using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Apizr;
                 """,
            _ => """
                 using System;
                    using Apizr;
                 """
        };

        code.AppendLine();
        code.AppendLine();

        #region Extended

        if (isDependencyInjectionExtension)
        {
            #region Registry

            if (hasManyApis)
            {
                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usings}}
                    using Apizr.Extending.Configuring.Common;
                  
                    public static partial class IServiceCollectionExtensions
                    {
                        public static IServiceCollection {{methodName}}(
                            this IServiceCollection services,
                """);
                if (string.IsNullOrEmpty(iocSettings!.BaseUrl))
                {
                    code.AppendLine(
                $$"""
                            Action<IApizrExtendedCommonOptionsBuilder> optionsBuilder)
                        {
                """);
                }
                else
                {
                    code.AppendLine(
                $$"""
                            Action<IApizrExtendedCommonOptionsBuilder> optionsBuilder = null)
                        {
                            if(optionsBuilder == null)
                                optionsBuilder = options => options.WithBaseAddress({{iocSettings.BaseUrl}});
                            else
                                optionsBuilder += options => options.WithBaseAddress({{iocSettings.BaseUrl}}, ApizrDuplicateStrategy.Ignore);
                """);
                }

                code.AppendLine();
                code.AppendLine(
                $"""
                            services.AddApizr(
                                registry => registry
                """);
                for (var i = 0; i < interfaceNames.Length; i++)
                {
                    var lineBreak = i == interfaceNames.Length - 1 ? "," : string.Empty;
                    code.AppendLine(
                $"""
                                    .AddManagerFor<{interfaceNames[i]}>(){lineBreak}
                """);
                }

                code.AppendLine(
                $"""
                                optionsBuilder);
                """);

                code.AppendLine();
#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
                code.AppendLine();
                code.AppendLine(
                $"""
                            return services;
                """);
            }

            #endregion

            #region Manager

            else
            {
                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usings}}
                    using Apizr.Extending.Configuring.Manager;

                    public static partial class IServiceCollectionExtensions
                    {
                        public static IServiceCollection {{methodName}}(
                            this IServiceCollection services,
                """);
                if (string.IsNullOrEmpty(iocSettings!.BaseUrl))
                {
                    code.AppendLine(
                $$"""
                            Action<IApizrExtendedManagerOptionsBuilder> optionsBuilder)
                        {
                """);
                }
                else
                {
                    code.AppendLine(
                $$"""
                            Action<IApizrExtendedManagerOptionsBuilder> optionsBuilder = null)
                        {
                            if(optionsBuilder == null)
                                optionsBuilder = options => options.WithBaseAddress({{iocSettings.BaseUrl}});
                            else
                                optionsBuilder += options => options.WithBaseAddress({{iocSettings.BaseUrl}}, ApizrDuplicateStrategy.Ignore);
                """);
                }

                code.AppendLine();
                code.AppendLine(
                $$"""               
                            services.AddApizrManagerFor<{{interfaceNames[0]}}>(optionsBuilder);
                """);

                code.AppendLine();
#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
                code.AppendLine();
                code.AppendLine(
                $$"""
                            return services;
                """);
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
                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usings}}
                    using Apizr.Configuring.Registry;
                  
                    public static partial class ApizrRegistration
                    {
                        public static IApizrRegistry {{methodName}}(Action<IApizrCommonOptionsBuilder> optionsBuilder)
                        {
                            if(optionsBuilder == null)
                                optionsBuilder = options => options.WithBaseAddress("");
                            else
                                optionsBuilder += options => options.WithBaseAddress("");
                                
                            var apizrRegistry = ApizrBuilder.Current.CreateRegistry(
                                registry => registry
                """);
                for (var i = 0; i < interfaceNames.Length; i++)
                {
                    var lineBreak = i == interfaceNames.Length - 1 ? "," : string.Empty;
                    code.AppendLine(
                $"""
                                    .AddManagerFor<{interfaceNames[i]}>(){lineBreak}
                """);
                }

                code.AppendLine(
                $"""
                                optionsBuilder);
                """);

                code.AppendLine();
#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
                code.AppendLine();
                code.AppendLine(
                $"""
                            return apizrRegistry;
                """);
            }

            #endregion

            #region Manager

            else
            {
                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usings}}
                    Apizr.Configuring.Manager;
                      
                    public static partial class ApizrRegistration
                    {
                        public static IApizrManager<{{interfaceNames[0]}}> {{methodName}}(Action<IApizrManagerOptionsBuilder> optionsBuilder)
                        {
                            if(optionsBuilder == null)
                                optionsBuilder = options => options.WithBaseAddress("");
                            else
                                optionsBuilder += options => options.WithBaseAddress("");
                                
                            var apizrManager = ApizrBuilder.Current.CreateManagerFor<{{interfaceNames[0]}}>(optionsBuilder);
                """);

                code.AppendLine();
#pragma warning disable RS1035
                code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
                code.AppendLine();
                code.AppendLine(
                $"""
                            return apizrManager;
                """);
            } 

            #endregion
        } 

        #endregion

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