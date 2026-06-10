using System.Text;
using NSwag;

namespace Refitter.Core;

internal interface IInterfacePartitioning
{
    /// <summary>
    /// Gets the group key for an operation. Operations with the same key are placed in the same interface.
    /// </summary>
    string GetGroupKey(OpenApiOperationInfo operation);

    /// <summary>
    /// Gets the raw interface name for a group key. The InterfaceGenerator handles deduplication.
    /// </summary>
    string GetInterfaceName(string groupKey, string title, string baseOperationName);

    /// <summary>
    /// Gets the raw method name for an operation. The InterfaceGenerator handles deduplication.
    /// </summary>
    string GetMethodName(OpenApiOperationInfo operation, string interfaceName, string baseOperationName);

    /// <summary>
    /// Gets the dynamic querystring parameter type name for a method.
    /// </summary>
    string GetDynamicQuerystringParameterType(string interfaceName, string methodName);

    /// <summary>
    /// True if this partitioning generates exactly one interface.
    /// </summary>
    bool IsSingleInterface { get; }

    /// <summary>
    /// Appends interface-level documentation to the StringBuilder.
    /// </summary>
    void AppendInterfaceDocumentation(
        OpenApiDocument document,
        XmlDocumentationGenerator docGenerator,
        string groupKey,
        OpenApiOperationInfo representativeOperation,
        StringBuilder code);
}
