using System.Text;
using NSwag;

namespace Refitter.Core;

internal class ByEndpointInterfacePartitioning(
    RefitGeneratorSettings settings) : IInterfacePartitioning
{

    public string GetGroupKey(OpenApiOperationInfo operation) => $"{operation.Path}#{operation.Verb}";

    public string GetInterfaceName(string groupKey, string title, string baseOperationName) =>
        "I" + baseOperationName.CapitalizeFirstCharacter();

    public string GetInterfaceNameSuffix() => "Endpoint";

    public string GetMethodName(OpenApiOperationInfo operation, string interfaceName, string baseOperationName)
    {
        var methodName = !string.IsNullOrWhiteSpace(settings.OperationNameTemplate)
            ? settings.OperationNameTemplate!.Replace("{operationName}", "Execute")
            : "Execute";

        return methodName;
    }

    public string GetDynamicQuerystringParameterType(string interfaceName, string methodName) =>
        interfaceName.Substring(1).Replace("Endpoint", "QueryParams");

    public bool IsSingleInterface => false;

    public void AppendInterfaceDocumentation(
        OpenApiDocument document,
        XmlDocumentationGenerator docGenerator,
        string groupKey,
        OpenApiOperationInfo representativeOperation,
        StringBuilder code) =>
        docGenerator.AppendInterfaceDocumentationByEndpoint(representativeOperation.Operation, code);
}
