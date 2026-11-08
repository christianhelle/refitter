using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Rewrites an OpenAPI JSON document into a form NSwag can read, before NSwag parses it. The document is parsed
/// at most once, with numbers kept exact, every rewrite runs on that one tree in a fixed order, and it is
/// serialized once if anything changed. A document no rewrite applies to is returned as the same instance.
/// </summary>
/// <remarks>
/// The rewrites, in order: clamp numeric bounds to the decimal range (#1273), convert object-valued enum
/// extensions to arrays (#1336), turn <c>itemSchema</c> into an array <c>schema</c> (#1348), and, for the root
/// document only, inline path items referenced from OpenAPI 3.1 <c>components/pathItems</c> (#1274).
/// </remarks>
internal static class OpenApiJsonNormalizer
{
    private static readonly JsonRewrite[] Rewrites =
    {
        new(NumericBoundsSanitizer.IsCandidate, NumericBoundsSanitizer.Rewrite, RootDocumentOnly: false),
        new(EnumExtensionsSanitizer.IsCandidate, EnumExtensionsSanitizer.Rewrite, RootDocumentOnly: false),
        new(ItemSchemaSanitizer.IsCandidate, ItemSchemaSanitizer.Rewrite, RootDocumentOnly: false),
        new(PathItemReferenceInliner.IsCandidate, PathItemReferenceInliner.Rewrite, RootDocumentOnly: true),
    };

    /// <summary>
    /// Normalizes the OpenAPI document being generated from.
    /// </summary>
    public static string NormalizeDocument(string json) => Normalize(json, isRootDocument: true);

    /// <summary>
    /// Normalizes a local file referenced from the OpenAPI document. Path items are only inlined in the
    /// document's own <c>paths</c>.
    /// </summary>
    public static string NormalizeReferencedFile(string json) => Normalize(json, isRootDocument: false);

    private static string Normalize(string json, bool isRootDocument)
    {
        var rewrites = Rewrites
            .Where(rewrite => (isRootDocument || !rewrite.RootDocumentOnly) && rewrite.IsCandidate(json))
            .ToArray();

        if (rewrites.Length == 0 || SanitizerJson.Parse(json) is not JContainer document)
            return json;

        var rewritten = false;
        foreach (var rewrite in rewrites)
            rewritten |= rewrite.Rewrite(document);

        return rewritten ? document.ToString(Formatting.None) : json;
    }

    private sealed record JsonRewrite(
        Func<string, bool> IsCandidate,
        Func<JContainer, bool> Rewrite,
        bool RootDocumentOnly);
}
