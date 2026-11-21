namespace Refitter.Core;

/// <summary>
/// Rejects documents whose nesting is deep enough to overflow the stack of a recursive parser.
/// YamlDotNet recurses once per nesting level, and a stack overflow cannot be caught: it ends the process.
/// The measurement is a single non-recursive pass over the text, so it is safe to run before any parser.
/// </summary>
internal static class DocumentNestingGuard
{
    /// <summary>
    /// The deepest nesting accepted. Real OpenAPI documents rarely nest beyond a few dozen levels, while
    /// the parsers fail at several thousand, so this leaves a wide margin on both sides.
    /// </summary>
    internal const int MaxNestingDepth = 256;

    /// <summary>
    /// Throws a <see cref="ReferenceResolutionException"/> when <paramref name="content"/> nests deeper than
    /// <paramref name="maxDepth"/>. That type is treated as fatal by the document loader, so the rejection is
    /// never retried with another loading strategy.
    /// </summary>
    internal static void EnsureWithinLimit(string content, string source, int maxDepth = MaxNestingDepth)
    {
        if (MeasureMaxDepth(content, maxDepth) > maxDepth)
        {
            throw new ReferenceResolutionException(
                $"OpenAPI document \"{source}\" is nested deeper than the supported limit of {maxDepth} " +
                "levels and is rejected, because parsing it could exhaust the stack.");
        }
    }

    /// <summary>
    /// Measures the deepest nesting of flow collections (<c>[</c> and <c>{</c>) and of inline block
    /// indicators such as <c>- - - x</c>, ignoring text inside quoted strings. Counting stops early once
    /// <paramref name="stopAfter"/> is exceeded.
    /// </summary>
    /// <remarks>
    /// Block nesting by indentation is not measured. Each level needs more indentation than the one before,
    /// so reaching a dangerous depth takes a file of hundreds of megabytes, which the file size already limits.
    /// Inline indicators (<c>- - - x</c>) nest without indentation, so those are counted.
    ///
    /// A quote only starts a string where a value may start, because a plain YAML scalar can contain quote
    /// characters (<c>[a - 'b, [[[</c>), and treating one as a string would hide the nesting after it. A
    /// quote that is not recognized as a string start only makes the count higher, never lower. Quoted
    /// strings are also ended at a line break for the same reason.
    /// </remarks>
    internal static int MeasureMaxDepth(string content, int stopAfter = int.MaxValue)
    {
        var max = 0;
        var flowDepth = 0;
        var inDouble = false;
        var inSingle = false;
        var escaped = false;

        // The last significant character outside strings, and whether it closed a quoted string
        var previous = '\0';
        var previousClosedString = false;

        // A colon directly after a closing quote (JSON: "a":"b") or followed by whitespace (YAML: a: "b")
        // introduces a value; a colon glued to plain text (a:"b) does not
        var colonIntroducesValue = false;
        var colonFollowedBySpace = false;

        var atLineStart = true;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (c == '\n' || c == '\r')
            {
                inDouble = false;
                inSingle = false;
                escaped = false;
                atLineStart = true;
                if (flowDepth == 0)
                {
                    previous = '\0';
                    previousClosedString = false;
                }

                colonFollowedBySpace = previous == ':';
                continue;
            }

            if (inDouble)
            {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                {
                    inDouble = false;
                    previous = '"';
                    previousClosedString = true;
                }

                continue;
            }

            if (inSingle)
            {
                if (c == '\'' && i + 1 < content.Length && content[i + 1] == '\'')
                {
                    // A doubled quote is an escaped quote and does not end the string
                    i++;
                }
                else if (c == '\'')
                {
                    inSingle = false;
                    previous = '\'';
                    previousClosedString = true;
                }

                continue;
            }

            if (c == ' ' || c == '\t')
            {
                if (previous == ':')
                    colonFollowedBySpace = true;
                continue;
            }

            if (atLineStart)
            {
                atLineStart = false;
                if (flowDepth == 0)
                {
                    // Leading block indicators ("- ", "? ", ": ") nest without any indentation
                    var indicators = 0;
                    var j = i;
                    while (j < content.Length && IsBlockIndicator(content, j))
                    {
                        indicators++;
                        j++;
                        while (j < content.Length && (content[j] == ' ' || content[j] == '\t'))
                            j++;
                    }

                    if (indicators > 0)
                    {
                        if (indicators > max)
                            max = indicators;
                        if (max > stopAfter)
                            return max;

                        previous = '\0';
                        previousClosedString = false;
                        i = j - 1;
                        continue;
                    }
                }
            }

            switch (c)
            {
                case '[':
                case '{':
                    flowDepth++;
                    if (flowDepth > max)
                    {
                        max = flowDepth;
                        if (max > stopAfter)
                            return max;
                    }

                    previousClosedString = false;
                    break;
                case ']':
                case '}':
                    if (flowDepth > 0)
                        flowDepth--;
                    previousClosedString = false;
                    break;
                case '"':
                case '\'':
                    if (StartsValue(previous, colonIntroducesValue, colonFollowedBySpace))
                    {
                        if (c == '"')
                            inDouble = true;
                        else
                            inSingle = true;
                        continue;
                    }

                    previousClosedString = false;
                    break;
                case ':':
                    colonIntroducesValue = previousClosedString;
                    colonFollowedBySpace = false;
                    previousClosedString = false;
                    break;
                default:
                    previousClosedString = false;
                    break;
            }

            previous = c;
        }

        return max;
    }

    private static bool StartsValue(char previous, bool colonIntroducesValue, bool colonFollowedBySpace) =>
        previous is '\0' or '[' or '{' or ',' ||
        (previous == ':' && (colonIntroducesValue || colonFollowedBySpace));

    private static bool IsBlockIndicator(string content, int index)
    {
        var c = content[index];
        if (c is not ('-' or '?' or ':'))
            return false;

        // An indicator is followed by whitespace or the end of the line, which excludes "-1" and "-text"
        return index + 1 >= content.Length || content[index + 1] is ' ' or '\t' or '\n' or '\r';
    }
}
