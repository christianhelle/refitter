using System.Text;
using NSwag;

namespace Refitter.Core;

internal class SingleInterfacePartitioning(
    RefitGeneratorSettings settings) : IInterfacePartitioning
{

    public string GetGroupKey(OpenApiOperationInfo operation) => string.Empty;

    public string GetInterfaceName(string groupKey, string title, string baseOperationName) =>
        $"I{title.CapitalizeFirstCharacter()}".Sanitize();

    public string GetInterfaceNameSuffix() => string.Empty;

    public string GetMethodName(OpenApiOperationInfo operation, string interfaceName, string baseOperationName)
    {
        var methodName = baseOperationName;
        if (settings.OperationNameTemplate?.Contains("{operationName}") ?? false)
        {
            methodName = settings.OperationNameTemplate!.Replace("{operationName}", methodName);
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
