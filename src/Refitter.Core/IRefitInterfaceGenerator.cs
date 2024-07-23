namespace Refitter.Core;

internal interface IRefitInterfaceGenerator
{
    IEnumerable<RefitGeneratedCode> GenerateCode();
}
