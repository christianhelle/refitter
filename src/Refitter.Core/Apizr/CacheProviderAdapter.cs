namespace Refitter.Core;

internal class CacheProviderAdapter : IApizrOptionsAdapter
{
    public bool CanApply(RefitGeneratorSettings settings)
    {
        return settings.ApizrSettings!.WithCacheProvider != CacheProviderType.None;
    }

    public void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings)
    {
        var isDependencyInjectionExtension = settings.DependencyInjectionSettings != null;

        switch (settings.ApizrSettings!.WithCacheProvider)
        {
            case CacheProviderType.Akavache:
                builder.AddPackage(ApizrPackages.Apizr_Integrations_Akavache);
                builder.AddUsing("using Akavache;");
                builder.AppendOptionsCode("\n                .WithAkavacheCacheHandler()");
                break;

            case CacheProviderType.MonkeyCache:
                builder.AddPackage(ApizrPackages.Apizr_Integrations_MonkeyCache);
                builder.AddUsing("using MonkeyCache;");
                builder.AppendOptionsCode("\n                .WithCacheHandler(new MonkeyCacheHandler(Barrel.Current))");
                break;

            case CacheProviderType.InMemory when isDependencyInjectionExtension:
                builder.AddPackage(ApizrPackages.Apizr_Extensions_Microsoft_Caching);
                builder.AppendOptionsCode("\n                .WithInMemoryCacheHandler()");
                break;

            case CacheProviderType.DistributedAsString when isDependencyInjectionExtension:
                builder.AddPackage(ApizrPackages.Apizr_Extensions_Microsoft_Caching);
                builder.AppendOptionsCode("\n                .WithDistributedCacheHandler<string>()");
                break;

            case CacheProviderType.DistributedAsByteArray when isDependencyInjectionExtension:
                builder.AddPackage(ApizrPackages.Apizr_Extensions_Microsoft_Caching);
                builder.AppendOptionsCode("\n                .WithDistributedCacheHandler<byte[]>()");
                break;
        }
    }
}
