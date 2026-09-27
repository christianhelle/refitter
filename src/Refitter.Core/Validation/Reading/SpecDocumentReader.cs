using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Refitter.Core.Validation.Model;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Reads an OpenAPI document into a <see cref="SpecDocument"/>, collecting the diagnostics Microsoft.OpenApi
/// (MIT license) reports while reading.
/// </summary>
internal static class SpecDocumentReader
{
    /// <summary>
    /// Reads JSON or YAML content. The format is decided by the first character that is not white space.
    /// </summary>
    /// <returns>The document, which is <c>null</c> when the content could not be read, and the diagnostics.</returns>
    public static (SpecDocument? Document, ValidationDiagnostics Diagnostics) Read(byte[] content, Uri? baseUrl = null)
    {
        var diagnostics = new ValidationDiagnostics();
        JsonNode jsonNode;
        if (IsJson(content))
        {
            try
            {
                jsonNode = JsonNode.Parse(content) ?? throw new InvalidOperationException("failed to parse input stream, input");
            }
            catch (JsonException exception)
            {
                diagnostics.Errors.Add(new ValidationIssue(
                    $"#line={exception.LineNumber}",
                    "Please provide the correct format, " + exception.Message));
                return (null, diagnostics);
            }
        }
        else
        {
            using var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            jsonNode = YamlToJsonConverter.Read(reader);
        }

        var context = new ParsingContext(diagnostics) { BaseUrl = baseUrl };
        try
        {
            return (context.Parse(jsonNode), diagnostics);
        }
        catch (SpecificationException exception)
        {
            diagnostics.Errors.Add(new ValidationIssue(exception.Pointer, exception.Message));
            return (null, diagnostics);
        }
    }

    /// <summary>
    /// Microsoft.OpenApi inspects the first byte that is not white space, so a byte order mark means YAML.
    /// </summary>
    private static bool IsJson(byte[] content)
    {
        var index = 0;
        var character = Next(content, ref index);
        while (char.IsWhiteSpace(character))
        {
            character = Next(content, ref index);
        }

        return character is '[' or '{';
    }

    private static char Next(byte[] content, ref int index) =>
        index < content.Length ? (char)content[index++] : (char)0xFFFF;
}
