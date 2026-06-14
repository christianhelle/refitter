namespace Refitter.Core;

internal class HttpMessageHandlerAdapter : IApizrOptionsAdapter
{
    public bool CanApply(RefitGeneratorSettings settings)
    {
        return settings.DependencyInjectionSettings?.HttpMessageHandlers.Length > 0;
    }

    public void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings)
    {
        foreach (string handler in settings.DependencyInjectionSettings!.HttpMessageHandlers)
        {
            builder.WithDelegatingHandler(handler);
        }
    }
}
