using System.Text;
using NJsonSchema;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Titles, summaries and descriptions are written into XML documentation comments, and XML 1.0 rejects most
/// control characters, unpaired surrogates, U+FFFE and U+FFFF. A document carrying one of them in such text
/// makes NSwag fail while rendering its documentation templates (#1428). This strips them from the
/// documentation text of the parsed document only; defaults, examples, enum values and extensions are data
/// and stay as they are.
/// </summary>
internal sealed class XmlInvalidCharacterMutator : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        if (document.Info != null)
        {
            Apply(document.Info.Title, text => document.Info.Title = text);
            Apply(document.Info.Description, text => document.Info.Description = text);
        }

        foreach (var tag in document.Tags?.Where(tag => tag != null) ?? [])
            Apply(tag.Description, text => tag.Description = text);

        foreach (var pathItem in (document.Paths?.Values ?? []).Where(pathItem => pathItem != null))
        {
            foreach (var parameter in pathItem.Parameters ?? [])
                StripSchema(parameter);

            foreach (var operation in pathItem.Values.Where(operation => operation != null))
                MutateOperation(operation);
        }

        foreach (var response in (document.Responses?.Values ?? []).Where(response => response != null))
            MutateResponse(response);

        foreach (var parameter in (document.Parameters?.Values ?? []).Where(parameter => parameter != null))
            StripSchema(parameter);

        SchemaWalker.TraverseDocumentSchemas(document, StripSchema);
    }

    private static void MutateOperation(OpenApiOperation operation)
    {
        Apply(operation.Summary, text => operation.Summary = text);
        Apply(operation.Description, text => operation.Description = text);

        foreach (var parameter in (operation.Parameters ?? []).Where(parameter => parameter != null))
            StripSchema(parameter);

        if (operation.RequestBody != null)
            Apply(operation.RequestBody.Description, text => operation.RequestBody.Description = text);

        foreach (var response in (operation.Responses?.Values ?? []).Where(response => response != null))
            MutateResponse(response);
    }

    private static void MutateResponse(OpenApiResponse response)
    {
        Apply(response.Description, text => response.Description = text);

        foreach (var header in (response.Headers?.Values ?? []).Where(header => header != null))
            StripSchema(header);
    }

    private static void StripSchema(JsonSchema schema)
    {
        Apply(schema.Title, text => schema.Title = text);
        Apply(schema.Description, text => schema.Description = text);
    }

    // Only assigns when something was stripped, so documents without such characters are not touched
    private static void Apply(string? text, Action<string> assign)
    {
        var stripped = Strip(text);
        if (stripped != null && !ReferenceEquals(stripped, text))
            assign(stripped);
    }

    private static string? Strip(string? text)
    {
        if (text == null)
            return null;

        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (!IsInvalid(text, i))
                builder.Append(text[i]);
        }

        return builder.Length == text.Length ? text : builder.ToString();
    }

    private static bool IsInvalid(string text, int index)
    {
        var c = text[index];
        if (c < ' ')
            return c is not ('\t' or '\n' or '\r');

        if (c is '￾' or '￿')
            return true;

        if (char.IsHighSurrogate(c))
            return index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]);

        return char.IsLowSurrogate(c) && (index == 0 || !char.IsHighSurrogate(text[index - 1]));
    }
}
