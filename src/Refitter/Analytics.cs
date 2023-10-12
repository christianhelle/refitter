using System.Reflection;
using System.Text.Json;
using Exceptionless;
using Exceptionless.Plugins;

using Refitter.Core;

using Spectre.Console.Cli;

namespace Refitter;

public static class Analytics
{
    public static void Configure()
    {
        ExceptionlessClient.Default.Configuration.SetUserIdentity(
            SupportInformation.GetAnonymousIdentity(),
            SupportInformation.GetSupportKey());

        ExceptionlessClient.Default.Configuration.UseSessions();
        ExceptionlessClient.Default.Configuration.SetVersion(typeof(GenerateCommand).Assembly.GetName().Version!);
        ExceptionlessClient.Default.Startup("pRql7vmgecZ0Iph6MU5TJE5XsZeesdTe0yx7TN4f");
    }
    
    public static Task LogFeatureUsage(Settings settings)
    {
        if (settings.NoLogging)
            return Task.CompletedTask;

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
                .ForEach(
                    attribute =>
                        ExceptionlessClient.Default
                            .CreateFeatureUsage(attribute.LongNames.FirstOrDefault() ?? property.Name)
                            .Submit());
        }

        return ExceptionlessClient.Default.ProcessQueueAsync();
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
        
        return true;
    }

    public static Task LogError(Exception exception, Settings settings)
    {
        if (settings.NoLogging)
            return Task.CompletedTask;

        exception
            .ToExceptionless(
                new ContextData(
                    Serializer.Deserialize<Dictionary<string, object>>(
                        Serializer.Serialize(settings))!))
            .Submit();

        return ExceptionlessClient.Default.ProcessQueueAsync();
    }
}