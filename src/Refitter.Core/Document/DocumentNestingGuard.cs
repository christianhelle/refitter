using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Refitter.Core;

/// <summary>
/// Rejects documents whose nesting is deep enough to overflow the stack of a recursive parser.
/// YamlDotNet's deserializer recurses once per nesting level, and a stack overflow cannot be caught: it ends
/// the process. The nesting is measured with parsers that do not recurse, so it is safe to run before any
/// recursive parser.
/// </summary>
/// <remarks>
/// YAML is measured from the event stream of <see cref="Parser"/>, which applies the full YAML lexical rules
/// (comments, multiline quotes, block scalars, compact sequences, anchors, tags) and reports every
/// collection that opens, however it is written. JSON is measured with the token reader of Newtonsoft.Json.
/// Both stop as soon as the limit is exceeded, so very deep input costs at most a limit's worth of work.
/// </remarks>
internal static class DocumentNestingGuard
{
    /// <summary>
    /// The deepest nesting accepted. Real OpenAPI documents rarely nest beyond a few dozen levels, while
    /// the parsers fail at several thousand, so this leaves a wide margin on both sides.
    /// </summary>
    internal const int MaxNestingDepth = 256;

    /// <summary>
    /// Throws a <see cref="ReferenceResolutionException"/> when <paramref name="content"/> nests deeper than
    /// <see cref="MaxNestingDepth"/>. That type is treated as fatal by the document loader, so the rejection
    /// is never retried with another loading strategy. The content is measured as JSON when it is valid
    /// JSON, and as YAML otherwise. Content that is not valid in either format is not rejected here: the
    /// parser that follows reports the syntax error.
    /// </summary>
    internal static void EnsureWithinLimit(string content, string source)
    {
        if (!TryEnsureJsonWithinLimit(content, source))
            EnsureYamlWithinLimit(content, source);
    }

    /// <summary>
    /// Measures <paramref name="content"/> as YAML, which also covers JSON written in flow style. Use this
    /// directly before handing content to the YAML deserializer.
    /// </summary>
    internal static void EnsureYamlWithinLimit(string content, string source)
    {
        var parser = new Parser(new StringReader(content));
        var depth = 0;
        try
        {
            while (parser.MoveNext())
            {
                switch (parser.Current)
                {
                    case MappingStart or SequenceStart:
                        depth++;
                        EnsureDepth(depth, source);
                        break;
                    case MappingEnd or SequenceEnd:
                        depth--;
                        break;
                }
            }
        }
        catch (YamlException)
        {
            // The deserializer reads the same events and stops at the same place, so what it parses is no
            // deeper than what was measured. It reports the syntax error itself
        }
    }

    /// <summary>
    /// Measures <paramref name="content"/> as JSON while reading it. Returns <c>false</c> when the content
    /// is not valid JSON, so the caller can measure it as YAML instead.
    /// </summary>
    private static bool TryEnsureJsonWithinLimit(string content, string source)
    {
        try
        {
            using var reader = CreateJsonReader(content);
            while (reader.Read())
                EnsureJsonDepth(reader, source);

            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a JSON reader without Newtonsoft's own depth limit, which would throw an error that callers
    /// take for "not JSON" and then hand the content to the recursive YAML reader. The reader is iterative,
    /// so callers check the depth themselves with <see cref="EnsureJsonDepth"/>.
    /// </summary>
    internal static JsonTextReader CreateJsonReader(string content) =>
        new(new StringReader(content))
        {
            DateParseHandling = DateParseHandling.None,
            MaxDepth = null
        };

    /// <summary>
    /// Throws when the token the <paramref name="reader"/> is positioned on opens a collection beyond the limit.
    /// </summary>
    internal static void EnsureJsonDepth(JsonReader reader, string source)
    {
        if (reader.TokenType is JsonToken.StartObject or JsonToken.StartArray)
            EnsureDepth(reader.Depth + 1, source);
    }

    private static void EnsureDepth(int depth, string source)
    {
        if (depth > MaxNestingDepth)
        {
            throw new ReferenceResolutionException(
                $"OpenAPI document \"{source}\" is nested deeper than the supported limit of {MaxNestingDepth} " +
                "levels and is rejected, because parsing it could exhaust the stack.");
        }
    }
}
