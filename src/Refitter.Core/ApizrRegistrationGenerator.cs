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
        var hasBaseUrl = !string.IsNullOrWhiteSpace(iocSettings?.BaseUrl);
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
            _ when isDependencyInjectionExtension => 
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using Apizr;
                """,
            _ => 
                """
                using System;
                    using Apizr;
                """
        };

        var usingsCodeBuilder = new StringBuilder(usings);
        usingsCodeBuilder.AppendLine();

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

        optionsCodeBuilder.Append(";");

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
                    using Apizr.Extending.Configuring.Manager;
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