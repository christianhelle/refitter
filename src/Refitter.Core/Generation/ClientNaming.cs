namespace Refitter.Core;

/// <summary>
/// The names derived from the OpenAPI document title or the configured interface name.
/// Every generator that names something after the client reads it from here, so they agree.
/// </summary>
internal sealed class ClientNaming
{
    private ClientNaming(string title, string serializerContextTypeName)
    {
        Title = title;
        SerializerContextTypeName = serializerContextTypeName;
    }

    /// <summary>
    /// The sanitized OpenAPI title when <see cref="NamingSettings.UseOpenApiTitle"/> is enabled and the
    /// document has a title, otherwise <see cref="NamingSettings.InterfaceName"/>.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The type name of the generated JsonSerializerContext.
    /// </summary>
    public string SerializerContextTypeName { get; }

    public static ClientNaming Create(NamingSettings naming, string? documentTitle)
    {
        var useDocumentTitle = naming.UseOpenApiTitle && !string.IsNullOrWhiteSpace(documentTitle);
        var title = useDocumentTitle
            ? documentTitle!.Sanitize()
            : naming.InterfaceName;

        return new ClientNaming(title, GetSerializerContextTypeName(title, useDocumentTitle));
    }

    private static string GetSerializerContextTypeName(string title, bool isDocumentTitle)
    {
        var name = isDocumentTitle ? title.CapitalizeFirstCharacter() : WithoutInterfacePrefix(title);
        return $"{name}SerializerContext";
    }

    private static string WithoutInterfacePrefix(string interfaceName)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            return NamingSettings.DefaultInterfaceName;

        return interfaceName.Length > 1 && interfaceName[0] == 'I' && char.IsUpper(interfaceName[1])
            ? interfaceName.Substring(1)
            : interfaceName;
    }
}
