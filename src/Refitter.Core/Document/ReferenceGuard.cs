namespace Refitter.Core;

/// <summary>
/// Validates <c>$ref</c> references inside an OpenAPI document by reading it with
/// <see cref="OpenApiDocumentReader"/>, which applies the reference rules as it reads.
/// </summary>
internal static class ReferenceGuard
{
    public static Task ValidateAsync(
        string openApiPath,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(openApiPath, null, allowRemoteReferences, cancellationToken);

    public static async Task ValidateAsync(
        string openApiPath,
        string? content,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The loader reports a missing document
        if (string.IsNullOrWhiteSpace(openApiPath) ||
            !PathUtilities.IsHttp(openApiPath) && content == null && !File.Exists(Path.GetFullPath(openApiPath)))
            return;

        await OpenApiDocumentReader.ReadAsync(openApiPath, content, allowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static List<string> ExtractReferences(
        string content,
        string source,
        long maxExpandedNodes = YamlAliasExpansionGuard.MaxExpandedNodes,
        int maxAmplification = YamlAliasExpansionGuard.MaxAmplification,
        long maxExpandedSize = YamlAliasExpansionGuard.MaxExpandedSize,
        bool readAsYaml = false) =>
        OpenApiDocumentReader.ExtractReferences(
            content, source, maxExpandedNodes, maxAmplification, maxExpandedSize, readAsYaml);

    internal static ReferenceResolutionException OutsideInputDirectory(
        string reference,
        string resolved,
        string rootDirectory) =>
        OpenApiDocumentReader.OutsideInputDirectory(reference, resolved, rootDirectory);

    internal static ReferenceResolutionException SymbolicLinkBlocked(string reference, string resolved) =>
        OpenApiDocumentReader.SymbolicLinkBlocked(reference, resolved);

    internal static bool ContainsLinkBelowRoot(string rootDirectory, string fullPath, string? rootDocumentPath = null) =>
        OpenApiDocumentReader.ContainsLinkBelowRoot(rootDirectory, fullPath, rootDocumentPath);

    internal static bool IsWithin(string rootDirectory, string candidate) =>
        OpenApiDocumentReader.IsWithin(rootDirectory, candidate);

    internal static ReferenceResolutionException RemoteBlocked(string reference, string source) =>
        OpenApiDocumentReader.RemoteBlocked(reference, source);
}
