using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Exceptionless;
using Exceptionless.Plugins;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Refitter.Core;
using Spectre.Console.Cli;

namespace Refitter;

[ExcludeFromCodeCoverage]
public static class Analytics
{
    // API keys are stored in the source code because I'm interested in how forks of this project are used.
    private const string ExceptionlessApiKey = "pRql7vmgecZ0Iph6MU5TJE5XsZeesdTe0yx7TN4f";
    private const string ApplicationInsightsConnectionString = "InstrumentationKey=470c204f-b460-493a-9e31-d9b2f5e25abb;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=0836c3ac-e8ac-4e0c-ade8-3e0fadb9b40c";
    private static TelemetryClient telemetryClient = null!;

    public static void Configure()
    {
        ExceptionlessClient.Default.Configuration.SetUserIdentity(
            SupportInformation.GetAnonymousIdentity(),
            SupportInformation.GetSupportKey());

        ExceptionlessClient.Default.Configuration.UseSessions();
        ExceptionlessClient.Default.Configuration.SetVersion(typeof(GenerateCommand).Assembly.GetName().Version!);
        ExceptionlessClient.Default.Startup(ExceptionlessApiKey);

        var configuration = TelemetryConfiguration.CreateDefault();
        configuration.ConnectionString = ApplicationInsightsConnectionString;

        telemetryClient = new TelemetryClient(configuration);
        telemetryClient.Context.User.Id = SupportInformation.GetSupportKey();
        telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
        telemetryClient.Context.Operation.Id = Guid.NewGuid().ToString();
        telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
        telemetryClient.Context.Component.Version = typeof(Analytics).Assembly.GetName().Version!.ToString();
        telemetryClient.TelemetryConfiguration.TelemetryInitializers.Add(new SupportKeyInitializer());
    }

    public static void LogFeatureUsage(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        if (settings.NoLogging)
            return;

        foreach (var property in typeof(Settings).GetProperties())
        {
            if (!CanLogFeature(settings, property))
            {
                continue;
            }

            property.GetCustomAttributes(typeof(CommandOptionAttribute), true)
                .OfType<CommandOptionAttribute>()
                .Where(
                    attribute =>
                        !attribute.LongNames.Contains("namespace") &&
                        !attribute.LongNames.Contains("output") &&
                        !attribute.LongNames.Contains("no-logging"))
                .ToList()
                .ForEach(attribute => LogFeatureUsage(attribute, property));
        }

        if (settings.SettingsFilePath is not null)
        {
            telemetryClient.TrackEvent(
                "settings-file",
                GetTelemetryProperties(settings, refitGeneratorSettings));
            telemetryClient.Flush();
        }
    }

    private static void LogFeatureUsage(CommandOptionAttribute attribute, PropertyInfo property)
    {
        var featureName = attribute.LongNames.FirstOrDefault() ?? property.Name;

        telemetryClient.TrackEvent(featureName);
        telemetryClient.Flush();
    }

    private static bool CanLogFeature(Settings settings, PropertyInfo property)
    {
        var value = property.GetValue(settings);
        if (value is null or false)
            return false;

        if (property.PropertyType == typeof(string[]) && ((string[])value).Length == 0)
            return false;

        if (property.PropertyType == typeof(MultipleInterfaces) &&
            ((MultipleInterfaces)value) == MultipleInterfaces.Unset)
            return false;

        if (property.PropertyType == typeof(OperationNameGeneratorTypes) &&
            ((OperationNameGeneratorTypes)value) == OperationNameGeneratorTypes.Default)
            return false;

        return true;
    }

    public static async Task LogError(Exception exception, Settings settings)
    {
        if (settings.NoLogging)
            return;

        var properties = GetTelemetryProperties(settings)
            .ToDictionary(
                pair => pair.Key,
                pair => (object)pair.Value);
        exception
            .ToExceptionless(
                new ContextData(
                    properties))
            .Submit();

        await ExceptionlessClient.Default.ProcessQueueAsync();

        telemetryClient.TrackException(
            exception,
            GetTelemetryProperties(settings));
    }

    private static Dictionary<string, string> GetTelemetryProperties(
        Settings settings,
        RefitGeneratorSettings? refitGeneratorSettings = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["usedSettingsFile"] = (!string.IsNullOrWhiteSpace(settings.SettingsFilePath)).ToString(),
            ["hasOpenApiPath"] = (!string.IsNullOrWhiteSpace(settings.OpenApiPath)).ToString(),
            ["skipValidation"] = settings.SkipValidation.ToString(),
            ["multipleInterfaces"] = settings.MultipleInterfaces.ToString(),
            ["operationNameGenerator"] = settings.OperationNameGenerator.ToString(),
            ["useApizr"] = settings.UseApizr.ToString(),
            ["generateMultipleFiles"] = settings.GenerateMultipleFiles.ToString(),
            ["includeTagsCount"] = (settings.Tags?.Length ?? 0).ToString(),
            ["matchPathsCount"] = (settings.MatchPaths?.Length ?? 0).ToString(),
            ["additionalNamespacesCount"] = (settings.AdditionalNamespaces?.Length ?? 0).ToString(),
            ["ignoredOperationHeadersCount"] = (settings.IgnoredOperationHeaders?.Length ?? 0).ToString(),
            ["hasCustomTemplateDirectory"] = (!string.IsNullOrWhiteSpace(settings.CustomTemplateDirectory)).ToString(),
            ["hasContractsOutputPath"] = (!string.IsNullOrWhiteSpace(settings.ContractsOutputPath)).ToString(),
            ["hasContractsNamespace"] = (!string.IsNullOrWhiteSpace(settings.ContractsNamespace)).ToString(),
            ["hasOperationNameTemplate"] = (!string.IsNullOrWhiteSpace(settings.OperationNameTemplate)).ToString(),
            ["hasSecurityScheme"] = (!string.IsNullOrWhiteSpace(settings.SecurityScheme)).ToString()
        };

        if (refitGeneratorSettings is null)
        {
            return properties;
        }

        properties["openApiPathsCount"] = (refitGeneratorSettings.OpenApiPaths?.Length ?? 0).ToString();
        properties["generateClients"] = refitGeneratorSettings.GenerateClients.ToString();
        properties["generateContracts"] = refitGeneratorSettings.GenerateContracts.ToString();
        properties["generateMultipleFilesFromSettings"] = refitGeneratorSettings.GenerateMultipleFiles.ToString();
        properties["usePolymorphicSerialization"] = refitGeneratorSettings.UsePolymorphicSerialization.ToString();
        properties["generateJsonSerializerContext"] = refitGeneratorSettings.GenerateJsonSerializerContext.ToString();
        properties["hasDependencyInjectionSettings"] = (refitGeneratorSettings.DependencyInjectionSettings is not null).ToString();
        properties["hasApizrSettings"] = (refitGeneratorSettings.ApizrSettings is not null).ToString();
        properties["hasCustomTemplateDirectoryFromSettings"] = (!string.IsNullOrWhiteSpace(refitGeneratorSettings.CustomTemplateDirectory)).ToString();
        properties["hasOutputFolder"] = (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder)).ToString();
        properties["hasOutputFilename"] = (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFilename)).ToString();
        properties["hasContractsOutputFolder"] = (!string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder)).ToString();
        properties["includePathMatchesCount"] = refitGeneratorSettings.IncludePathMatches.Length.ToString();
        properties["includeTagsFromSettingsCount"] = refitGeneratorSettings.IncludeTags.Length.ToString();

        return properties;
    }
}
