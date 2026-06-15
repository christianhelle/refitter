using Refitter.Core.Settings;

namespace Refitter.Core;

internal class MediationAdapter : IApizrOptionsAdapter
{
    public bool CanApply(RefitGeneratorSettings settings)
    {
        return settings.ApizrSettings!.WithMediation &&
               settings.DependencyInjectionSettings != null;
    }

    public void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings)
    {
        builder.AddPackage(ApizrPackages.Apizr_Integrations_MediatR);
        builder.AddUsing("using MediatR;");
        builder.AppendOptionsCode("\n                .WithMediation()");
    }
}
