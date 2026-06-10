using System.Text;
using NSwag;

namespace Refitter.Core;

internal class SingleInterfacePartitioning : IInterfacePartitioning
{
    private readonly RefitGeneratorSettings _settings;

    public SingleInterfacePartitioning(RefitGeneratorSettings settings)
    {
        _settings = settings;
    }

    public string GetGroupKey(OpenApiOperationInfo operation) => string.Empty;

    public string GetInterfaceName(string groupKey, string title, string baseOperationName) =>
        $"I{title.CapitalizeFirstCharacter()}".Sanitize();

    public string GetInterfaceNameSuffix() => string.Empty;

    public string GetMethodName(OpenApiOperationInfo operation, string interfaceName, string baseOperationName)
    {
        var methodName = baseOperationName;
        if (_settings.OperationNameTemplate?.Contains("{operationName}") ?? false)
        {
            methodName = _settings.OperationNameTemplate!.Replace("{operationName}", methodName);
        }

        return methodName;
    }

    public string GetDynamicQuerystringParameterType(string interfaceName, string methodName) =>
        methodName + "QueryParams";

    public bool IsSingleInterface => true;

    public void AppendInterfaceDocumentation(
        OpenApiDocument document,
        XmlDocumentationGenerator docGenerator,
        string groupKey,
        OpenApiOperationInfo representativeOperation,
        StringBuilder code) =>
        docGenerator.AppendSingleInterfaceDocumentation(document, code);
}
