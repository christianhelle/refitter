using System.Text;
using NSwag;

namespace Refitter.Core;

internal class ByTagInterfacePartitioning : IInterfacePartitioning
{
    private readonly RefitGeneratorSettings _settings;
    private readonly string _ungroupedTitle;

    public ByTagInterfacePartitioning(RefitGeneratorSettings settings, OpenApiDocument document)
    {
        _settings = settings;
        _ungroupedTitle = settings.Naming.UseOpenApiTitle
            ? IdentifierUtils.Sanitize(document.Info?.Title ?? "ApiClient")
            : settings.Naming.InterfaceName;
        _ungroupedTitle = _ungroupedTitle.CapitalizeFirstCharacter();
    }

    public string GetGroupKey(OpenApiOperationInfo operation)
    {
        var tag = operation.Operation.Tags.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(tag)
            ? tag.SanitizeControllerTag()
            : _ungroupedTitle;
    }

    public string GetInterfaceName(string groupKey, string title, string baseOperationName) =>
        $"I{groupKey.CapitalizeFirstCharacter()}";

    public string GetInterfaceNameSuffix() => "Api";

    public string GetMethodName(OpenApiOperationInfo operation, string interfaceName, string baseOperationName)
    {
        var methodName = baseOperationName.CapitalizeFirstCharacter();
        if (_settings.OperationNameTemplate?.Contains("{operationName}") ?? false)
        {
            methodName = _settings.OperationNameTemplate!.Replace("{operationName}", methodName);
        }

        return methodName;
    }

    public string GetDynamicQuerystringParameterType(string interfaceName, string methodName) =>
        methodName + "QueryParams";

    public bool IsSingleInterface => false;

    public void AppendInterfaceDocumentation(
        OpenApiDocument document,
        XmlDocumentationGenerator docGenerator,
        string groupKey,
        OpenApiOperationInfo representativeOperation,
        StringBuilder code) =>
        docGenerator.AppendInterfaceDocumentationByTag(document, groupKey, code);
}
