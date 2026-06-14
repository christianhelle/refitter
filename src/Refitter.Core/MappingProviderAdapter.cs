using Refitter.Core.Settings;

namespace Refitter.Core;

internal class MappingProviderAdapter : IApizrOptionsAdapter
{
    public bool CanApply(RefitGeneratorSettings settings)
    {
        return settings.ApizrSettings!.WithMappingProvider != MappingProviderType.None;
    }

    public void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings)
    {
        var isDependencyInjectionExtension = settings.DependencyInjectionSettings != null;

        switch (settings.ApizrSettings!.WithMappingProvider)
        {
            case MappingProviderType.AutoMapper:
                builder.AddPackage(ApizrPackages.Apizr_Integrations_AutoMapper);
                builder.AddUsing("using AutoMapper;");
                if (isDependencyInjectionExtension)
                    builder.AppendOptionsCode("\n                .WithAutoMapperMappingHandler()");
                else
                    builder.AppendOptionsCode("\n                .WithAutoMapperMappingHandler(new MapperConfiguration(config => { /* YOUR_MAPPINGS_HERE */ }))");
                break;

            case MappingProviderType.Mapster:
                builder.AddPackage(ApizrPackages.Apizr_Integrations_Mapster);
                builder.AddUsing("using Mapster;");
                if (isDependencyInjectionExtension)
                {
                    builder.AddUsing("using MapsterMapper;");
                    builder.AppendOptionsCode("\n                .WithMapsterMappingHandler()");
                }
                else
                {
                    builder.AppendOptionsCode("\n                .WithMapsterMappingHandler(new Mapper())");
                }
                break;
        }
    }
}
