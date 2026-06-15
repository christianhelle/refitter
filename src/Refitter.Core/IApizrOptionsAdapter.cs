namespace Refitter.Core;

internal interface IApizrOptionsAdapter
{
    bool CanApply(RefitGeneratorSettings settings);
    void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings);
}
