using System.Text;

namespace Refitter.Core;

internal static class ApizrRegistrationGenerator
{
    private static readonly IApizrOptionsAdapter[] Adapters =
    [
        new HttpMessageHandlerAdapter(),
        new RetryHandlerAdapter(),
        new CacheProviderAdapter(),
        new MappingProviderAdapter(),
        new PriorityAdapter(),
        new MediationAdapter(),
        new FileTransferAdapter(),
    ];

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
        var methodName = GetMethodName(iocSettings, hasManyApis, title);

        var initialOptionsCode =
                $$"""
                optionsBuilder ??= _ => { }; // Default empty options if null
                            optionsBuilder += options => options
                """;

        var builder = new ApizrOptionsBuilder(initialOptionsCode, GetUsings(iocSettings));

        if (hasBaseUrl)
        {
            builder.WithBaseAddress(iocSettings!.BaseUrl!, "ApizrDuplicateStrategy.Ignore");
        }

        foreach (var adapter in Adapters.Where(adapter => adapter.CanApply(settings)))
        {
            adapter.Apply(builder, settings);
        }

        builder.AddPackage(ApizrPackages.Apizr);
        builder.AddUsing("using Apizr;");

        var optionsCode = builder.BuildOptionsCode();
        var usingsCodeBuilder = new StringBuilder(builder.GetUsings());
        var packageCodeBuilder = BuildPackageInstructions(
            settings,
            builder.GetPackages(),
            isDependencyInjectionExtension,
            methodName);

        // Code
        var code = new StringBuilder();
        code.AppendLine();
        code.AppendLine();

        var registration = new RegistrationContext(
            settings,
            interfaceNames,
            methodName,
            optionsCode,
            hasBaseUrl);

        if (isDependencyInjectionExtension)
        {
            if (hasBaseUrl)
            {
                usingsCodeBuilder.AppendLine(
                    "    using Apizr.Configuring;");
            }

            if (hasManyApis)
                AppendExtendedRegistry(code, usingsCodeBuilder, packageCodeBuilder, registration);
            else
                AppendExtendedManager(code, usingsCodeBuilder, packageCodeBuilder, registration);
        }
        else if (hasManyApis)
        {
            AppendStaticRegistry(code, usingsCodeBuilder, packageCodeBuilder, registration);
        }
        else
        {
            AppendStaticManager(code, usingsCodeBuilder, packageCodeBuilder, registration);
        }

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

    private static string GetMethodName(
        DependencyInjectionSettings? iocSettings,
        bool hasManyApis,
        string? title)
    {
        var methodName = iocSettings?.ExtensionMethodName;
        if (!string.IsNullOrWhiteSpace(methodName) && methodName != DependencyInjectionSettings.DefaultExtensionMethodName)
            return methodName!;

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

        var verb = iocSettings != null ? "Configure" : "Build";
        var suffix = hasManyApis ? "ApizrManagers" : "ApizrManager";
        return $"{verb}{formatedTitle}{suffix}";
    }

    private static string GetUsings(DependencyInjectionSettings? iocSettings) =>
        iocSettings?.TransientErrorHandler switch
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
            _ when iocSettings != null =>
                """
                using System;
                    using Microsoft.Extensions.DependencyInjection;
                """,
            _ =>
                """
                using System;
                """
        };

    private static StringBuilder BuildPackageInstructions(
        RefitGeneratorSettings settings,
        IEnumerable<ApizrPackages> apizrPackages,
        bool isDependencyInjectionExtension,
        string methodName)
    {
        var packageCodeBuilder = new StringBuilder();
        var packages = apizrPackages.OrderByDescending(p => p).ToList();
        if (packages.Count == 0)
            return packageCodeBuilder;

        if (!isDependencyInjectionExtension && RequiresDependencyInjection(settings.ApizrSettings!))
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

        if (settings.ApizrSettings!.WithFileTransfer)
        {
            packageCodeBuilder.AppendLine(
            $$"""
                    // - Add your file transfer manager while calling {{methodName}} method thanks to its options builder parameter
                """);
        }

        return packageCodeBuilder;
    }

    private static bool RequiresDependencyInjection(ApizrSettings apizrSettings) =>
        apizrSettings.WithMediation ||
        apizrSettings.WithCacheProvider == CacheProviderType.InMemory ||
        apizrSettings.WithCacheProvider == CacheProviderType.DistributedAsString ||
        apizrSettings.WithCacheProvider == CacheProviderType.DistributedAsByteArray;

    private static void AppendExtendedRegistry(
        StringBuilder code,
        StringBuilder usingsCodeBuilder,
        StringBuilder packageCodeBuilder,
        RegistrationContext context)
    {
        var (settings, interfaceNames, methodName, optionsCode, hasBaseUrl) = context;

        usingsCodeBuilder.AppendLine(
            "using Apizr.Extending.Configuring.Common;");

        code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{packageCodeBuilder}}
                    {{usingsCodeBuilder}}
                {{(settings.GenerateXmlDocCodeComments ? """
                
                    /// <summary>
                    /// Extension methods for configuring Apizr managers in the service collection.
                    /// </summary>
                """ : "")}}
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
                            {{optionsCode}}
                            
                            return services.AddApizr(
                                registry => registry
                """);
        AppendManagerRegistrations(code, interfaceNames);

        code.AppendLine();
        RemoveTrailingNewLine(code);
    }

    private static void AppendExtendedManager(
        StringBuilder code,
        StringBuilder usingsCodeBuilder,
        StringBuilder packageCodeBuilder,
        RegistrationContext context)
    {
        var (settings, interfaceNames, methodName, optionsCode, hasBaseUrl) = context;

        usingsCodeBuilder.AppendLine(
            "    using Apizr.Extending.Configuring.Manager;");

        code.AppendLine(
                $$"""
                #nullable enable
                namespace {{settings.Namespace}}
                {
                    {{packageCodeBuilder}}
                    {{usingsCodeBuilder}}
                {{(settings.GenerateXmlDocCodeComments ? """
                
                    /// <summary>
                    /// Extension methods for configuring Apizr managers in the service collection.
                    /// </summary>
                """ : "")}}
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
                            {{optionsCode}}
                                 
                            return services.AddApizrManagerFor<{{interfaceNames[0]}}>(optionsBuilder);
                """);

        RemoveTrailingNewLine(code);
    }

    private static void AppendStaticRegistry(
        StringBuilder code,
        StringBuilder usingsCodeBuilder,
        StringBuilder packageCodeBuilder,
        RegistrationContext context)
    {
        var (settings, interfaceNames, methodName, optionsCode, _) = context;

        usingsCodeBuilder.AppendLine(
            "    using Apizr.Configuring.Registry;");

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
                            {{optionsCode}}
                                  
                            return ApizrBuilder.Current.CreateRegistry(
                                registry => registry
                """);
        AppendManagerRegistrations(code, interfaceNames);

        RemoveTrailingNewLine(code);
    }

    private static void AppendStaticManager(
        StringBuilder code,
        StringBuilder usingsCodeBuilder,
        StringBuilder packageCodeBuilder,
        RegistrationContext context)
    {
        var (settings, interfaceNames, methodName, optionsCode, _) = context;

        usingsCodeBuilder.AppendLine(
            "    using Apizr.Configuring.Manager;");

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
                            {{optionsCode}}
                                  
                            return ApizrBuilder.Current.CreateManagerFor<{{interfaceNames[0]}}>(optionsBuilder);  
                """);

        RemoveTrailingNewLine(code);
    }

    private static void AppendManagerRegistrations(StringBuilder code, string[] interfaceNames)
    {
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
    }

    private static void RemoveTrailingNewLine(StringBuilder code)
    {
#pragma warning disable RS1035
        code.Remove(code.Length - Environment.NewLine.Length, Environment.NewLine.Length);
#pragma warning restore RS1035
    }

    private sealed record RegistrationContext(
        RefitGeneratorSettings Settings,
        string[] InterfaceNames,
        string MethodName,
        string OptionsCode,
        bool HasBaseUrl);
}
