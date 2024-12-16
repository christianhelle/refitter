using System.Reflection;
using Exceptionless;
using Exceptionless.Plugins;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Refitter.Core;
using Spectre.Console.Cli;

namespace Refitter;

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
                new Dictionary<string, string>
                {
                    { "settings", Serializer.Serialize(refitGeneratorSettings) }
                });
            telemetryClient.Flush();
        }
    }

    private static void LogFeatureUsage(CommandOptionAttribute attribute, PropertyInfo property)
    {
        var featureName = attribute.LongNames.FirstOrDefault() ?? property.Name;

        ExceptionlessClient
            .Default
            .CreateFeatureUsage(featureName)
            .Submit();

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

        string json = Serializer.Serialize(settings);
        var properties = Serializer.Deserialize<Dictionary<string, object>>(json)!;
        exception
            .ToExceptionless(
                new ContextData(
                    properties))
            .Submit();

        await ExceptionlessClient.Default.ProcessQueueAsync();

        telemetryClient.TrackException(
            exception,
            new Dictionary<string, string>
            {
                { "settings", json }
            });
    }
}
