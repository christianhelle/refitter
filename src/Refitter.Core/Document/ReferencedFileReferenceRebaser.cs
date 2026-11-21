using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Rewrites the relative file <c>$ref</c> values of a referenced file into absolute paths. The reference
/// resolvers resolve every nested reference against the root document, while <see cref="ReferenceGuard"/>
/// validates it against the file that contains it. Making each reference absolute, relative to its own
/// file, removes the disagreement, so the file that is read is the file that was validated.
/// </summary>
internal static class ReferencedFileReferenceRebaser
{
    public static string Rebase(string json, string filePath)
    {
        if (json.IndexOf("ref", StringComparison.OrdinalIgnoreCase) < 0 ||
            SanitizerJson.Parse(json) is not JContainer document)
            return json;

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        var rewritten = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
        {
            if (!string.Equals(property.Name, "$ref", StringComparison.OrdinalIgnoreCase) ||
                property.Value is not JValue { Type: JTokenType.String } value)
                continue;

            var reference = ((string?)value.Value)?.Trim();
            if (string.IsNullOrEmpty(reference) ||
                reference!.StartsWith("#", StringComparison.Ordinal) ||
                PathUtilities.IsHttp(reference) ||
                PathUtilities.TryGetRemoteUri(reference, out _))
                continue;

            var separator = reference.IndexOf('#');
            var fileReference = separator < 0 ? reference : reference.Substring(0, separator);
            var fragment = separator < 0 ? string.Empty : reference.Substring(separator);
            if (string.IsNullOrWhiteSpace(fileReference))
                continue;

            value.Value = Path.GetFullPath(Path.Combine(directory, fileReference)) + fragment;
            rewritten = true;
        }

        return rewritten ? document.ToString(Formatting.None) : json;
    }
}
