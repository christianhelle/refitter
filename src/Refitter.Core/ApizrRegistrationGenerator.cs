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

        var usingsBuilder = new StringBuilder(usings);
        usingsBuilder.AppendLine();

        code.AppendLine();
        code.AppendLine();

        #region Extended

        if (isDependencyInjectionExtension)
        {
            var hasBaseUrl = !string.IsNullOrWhiteSpace(iocSettings!.BaseUrl);
            if (hasBaseUrl)
            {
                usingsBuilder.AppendLine(
                $$"""
                    using Apizr.Configuring;
                """);
            }

            #region Registry

            if (hasManyApis)
            {
                usingsBuilder.AppendLine(
                $$"""
                    using Apizr.Extending.Configuring.Common;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usingsBuilder}}
                  
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

                code.AppendLine(
                $$"""         
                            optionsBuilder ??= _ => { }; // Default empty options if null
                            optionsBuilder += options => options
                """);

                if (hasBaseUrl)
                {
                    code.Append(
                $$"""               
                                .WithBaseAddress("{{iocSettings.BaseUrl}}", ApizrDuplicateStrategy.Ignore)
                """);
                }

                code.Append(";");
                code.AppendLine();
                code.AppendLine();
                code.Append(
                $"""
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
                usingsBuilder.AppendLine(
                $$"""
                    using Apizr.Extending.Configuring.Manager;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usingsBuilder}}

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
                            optionsBuilder ??= _ => { }; // Default empty options if null
                            optionsBuilder += options => options
                """);

                if (hasBaseUrl)
                {
                    code.Append(
                $$"""               
                                .WithBaseAddress("{{iocSettings.BaseUrl}}", ApizrDuplicateStrategy.Ignore)
                """);
                }

                code.Append(";");
                code.AppendLine();
                code.AppendLine();
                code.AppendLine(
                $$"""               
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
                usingsBuilder.AppendLine(
                $$"""
                    using Apizr.Configuring.Registry;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usingsBuilder}}
                  
                    public static partial class ApizrRegistration
                    {
                        public static IApizrRegistry {{methodName}}(Action<IApizrCommonOptionsBuilder> optionsBuilder)
                        {
                            optionsBuilder ??= _ => { }; // Default empty options if null
                            optionsBuilder += options => options
                """);
                if (true) // todo: add conditional logic
                {
                    code.Append(
                $$"""               
                                .WithBaseAddress("https://test.com", ApizrDuplicateStrategy.Ignore)
                """);
                }

                code.Append(";");
                code.AppendLine();
                code.AppendLine();
                code.Append(
                $"""
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
                usingsBuilder.AppendLine(
                $$"""
                    using Apizr.Configuring.Manager;
                """);

                code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{usingsBuilder}}
                      
                    public static partial class ApizrRegistration
                    {
                        public static IApizrManager<{{interfaceNames[0]}}> {{methodName}}(Action<IApizrManagerOptionsBuilder> optionsBuilder)
                        {
                            optionsBuilder ??= _ => { }; // Default empty options if null
                            optionsBuilder += options => options
                """);
                if (true) // todo: add conditional logic
                {
                    code.Append(
                $$"""               
                                .WithBaseAddress("https://test.com", ApizrDuplicateStrategy.Ignore)
                """);
                }

                code.Append(";");
                code.AppendLine();
                code.AppendLine();
                code.Append(
                $"""
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