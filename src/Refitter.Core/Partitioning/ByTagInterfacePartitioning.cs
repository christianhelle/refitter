using System.Text;
using NSwag;

namespace Refitter.Core;

internal class ByTagInterfacePartitioning : IInterfacePartitioning
{
    private readonly RefitGeneratorSettings settings;
    private readonly string ungroupedTitle;

    public ByTagInterfacePartitioning(RefitGeneratorSettings settings, OpenApiDocument document)
    {
        this.settings = settings;
        ungroupedTitle = settings.Naming.UseOpenApiTitle
            ? (document.Info?.Title ?? "ApiClient").Sanitize()
            : settings.Naming.InterfaceName;
        ungroupedTitle = ungroupedTitle.CapitalizeFirstCharacter();
    }

    public string GetGroupKey(OpenApiOperationInfo operation)
    {
        var tag = operation.Operation.Tags.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(tag)
            ? tag.SanitizeControllerTag()
            : ungroupedTitle;
    }

    public string GetInterfaceName(string groupKey, string title, string baseOperationName) =>
        $"I{groupKey.CapitalizeFirstCharacter()}".Sanitize();

    public string GetInterfaceNameSuffix() => "Api";

    public string GetMethodName(OpenApiOperationInfo operation, string interfaceName, string baseOperationName)
    {
        var methodName = baseOperationName.CapitalizeFirstCharacter();
        if (settings.OperationNameTemplate?.Contains("{operationName}") ?? false)
        {
            methodName = settings.OperationNameTemplate!.Replace("{operationName}", methodName);
        }

        return methodName;
    }

    public string GetDynamicQuerystringParameterType(string interfaceName, string methodName)
    {
        var prefix = interfaceName;
        if (prefix.StartsWith("I"))
        {
            prefix = prefix.Substring(1);
        }

        var suffix = GetInterfaceNameSuffix();
        if (prefix.EndsWith(suffix) && prefix.Length > suffix.Length)
        {
            prefix = prefix.Substring(0, prefix.Length - suffix.Length);
        }

        return prefix + methodName + "QueryParams";
    }

    public bool IsSingleInterface => false;

    public void AppendInterfaceDocumentation(
        OpenApiDocument document,
        XmlDocumentationGenerator docGenerator,
        string groupKey,
        OpenApiOperationInfo representativeOperation,
        StringBuilder code) =>
        docGenerator.AppendInterfaceDocumentationByTag(document, groupKey, code);
}
