namespace Refitter.Core;

internal interface IRefitInterfaceGenerator
{
    IEnumerable<GeneratedCode> GenerateCode();
}
