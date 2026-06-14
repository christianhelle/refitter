using NSwag;
using Refitter.Core;

namespace Refitter.Core;

internal interface IContractsPostProcessor
{
    string Process(OpenApiDocument document, RefitGeneratorSettings settings, string contracts);
}
