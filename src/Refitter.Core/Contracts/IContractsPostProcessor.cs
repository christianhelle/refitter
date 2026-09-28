
namespace Refitter.Core;

internal interface IContractsPostProcessor
{
    string Process(ApiDocument document, RefitGeneratorSettings settings, string contracts);
}
