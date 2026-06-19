using System.Globalization;

namespace Refitter.Core;

internal class RetryHandlerAdapter : IApizrOptionsAdapter
{
    public bool CanApply(RefitGeneratorSettings settings)
    {
        return settings.DependencyInjectionSettings?.TransientErrorHandler is
            TransientErrorHandler.Polly or
            TransientErrorHandler.HttpResilience;
    }

    public void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings)
    {
        var iocSettings = settings.DependencyInjectionSettings!;
        var durationString = iocSettings.FirstBackoffRetryInSeconds.ToString(CultureInfo.InvariantCulture);

        builder.ConfigureHttpClientBuilder(httpBuilder =>
        {
            if (iocSettings.TransientErrorHandler == TransientErrorHandler.Polly)
            {
                builder.AddUsing("using Polly;");
                builder.AddUsing("using Polly.Contrib.WaitAndRetry;");
                builder.AddUsing("using Polly.Extensions.Http;");
                httpBuilder.Append(
                    $"                .ConfigureHttpClientBuilder(builder => builder{Environment.NewLine}" +
                    $"                    .AddPolicyHandler({Environment.NewLine}" +
                    $"                        HttpPolicyExtensions{Environment.NewLine}" +
                    $"                            .HandleTransientHttpError(){Environment.NewLine}" +
                    $"                            .WaitAndRetryAsync({Environment.NewLine}" +
                    $"                                Backoff.DecorrelatedJitterBackoffV2({Environment.NewLine}" +
                    $"                                    TimeSpan.FromSeconds({durationString}),{Environment.NewLine}" +
                    $"                                    {iocSettings.MaxRetryCount}))))");
            }
            else if (iocSettings.TransientErrorHandler == TransientErrorHandler.HttpResilience)
            {
                builder.AddUsing("using Microsoft.Extensions.Http.Resilience;");
                httpBuilder.Append(
                    $"                .ConfigureHttpClientBuilder(builder => builder{Environment.NewLine}" +
                    $"                    .AddStandardResilienceHandler(config =>{Environment.NewLine}" +
                    $"                    {{{Environment.NewLine}" +
                    $"                        config.Retry = new HttpRetryStrategyOptions{Environment.NewLine}" +
                    $"                        {{{Environment.NewLine}" +
                    $"                            UseJitter = true,{Environment.NewLine}" +
                    $"                            MaxRetryAttempts = {iocSettings.MaxRetryCount},{Environment.NewLine}" +
                    $"                            Delay = TimeSpan.FromSeconds({durationString}){Environment.NewLine}" +
                    $"                        }};{Environment.NewLine}" +
                    $"                    }}))");
            }
        });
    }
}
