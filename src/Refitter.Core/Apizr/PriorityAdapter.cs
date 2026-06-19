namespace Refitter.Core;

internal class PriorityAdapter : IApizrOptionsAdapter
{
    public bool CanApply(RefitGeneratorSettings settings)
    {
        return settings.ApizrSettings!.WithPriority;
    }

    public void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings)
    {
        builder.AddPackage(ApizrPackages.Apizr_Integrations_Fusillade);
        builder.AppendOptionsCode("\n                .WithPriority()");
    }
}
