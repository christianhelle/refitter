using System.Text;

using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

/// <summary>
/// Generator class for creating XML documentation.
/// </summary>
public class XmlDocumentationGenerator
{
    /// <summary>
    /// The global code generation settings.
    /// </summary>
    private readonly RefitGeneratorSettings _settings;

    /// <summary>
    /// The whitespace to use for a single level of indentation.
    /// </summary>
    private const string Separator = "    ";

    /// <summary>
    /// Instantiates a new instance of the <see cref="XmlDocumentationGenerator"/> class.
    /// </summary>
    /// <param name="settings">The code generation settings to use.</param>
    internal XmlDocumentationGenerator(RefitGeneratorSettings settings)
    {
        this._settings = settings;
    }

    /// <summary>
    /// Appends XML docs for the given interface definition to the given code builder.
    /// </summary>
    /// <param name="group">The OpenAPI definition of the interface.</param>
    /// <param name="code">The builder to append the documentation to.</param>
    public void AppendInterfaceDocumentation(OpenApiOperation group, StringBuilder code)
    {
        if (!_settings.GenerateXmlDocCodeComments || string.IsNullOrWhiteSpace(group.Summary))
            return;

        this.AppendXmlCommentBlock("summary", group.Summary, code, indent: Separator);
    }

    /// <summary>
    /// Appends XML docs for the given method to the given code builder.
    /// </summary>
    /// <param name="method">The NSwag model of the method's OpenAPI definition.</param>
    /// <param name="code">The builder to append the documentation to.</param>
    public void AppendMethodDocumentation(CSharpOperationModel method, StringBuilder code)
    {
        if (!_settings.GenerateXmlDocCodeComments)
            return;

        if (!string.IsNullOrWhiteSpace(method.Summary))
            this.AppendXmlCommentBlock("summary", method.Summary, code);

        if (!string.IsNullOrWhiteSpace(method.Description))
        {
            this.AppendXmlCommentBlock("remarks", method.Description, code);
        }

        foreach (var parameter in method.Parameters)
        {
            if (parameter == null || string.IsNullOrWhiteSpace(parameter.Description))
                continue;

            this.AppendXmlCommentBlock("param", parameter.Description, code, new Dictionary<string, string>
                { ["name"] = parameter.VariableName });
        }

        if (method.HasResult && !string.IsNullOrWhiteSpace(method.ResultDescription))
        {
            this.AppendXmlCommentBlock("returns", method.ResultDescription, code);
        }

        this.AppendXmlCommentBlock("throws", BuildErrorDescription(method.Responses), code, new Dictionary<string, string>
            { ["cref"] = "ApiException" });
    }

    /// <summary>
    /// Append a single XML element to the given code builder.
    /// If the content includes line breaks, it is placed on a new line and the existing breaks are preserved.
    /// Otherwise, the element and its content are placed on the same line.
    /// </summary>
    /// <param name="tagName">The name of the tag to write.</param>
    /// <param name="content">The content to place within the tag.</param>
    /// <param name="code">The builder to append the tag to.</param>
    /// <param name="attributes">An optional dictionary of attributes to add to the tag.</param>
    /// <param name="indent">The whitespace to add before new lines.</param>
    private void AppendXmlCommentBlock(
        string tagName,
        string content,
        StringBuilder code,
        Dictionary<string, string>? attributes = null,
        string indent = $"{Separator}{Separator}")
    {
        code.Append($"{indent}/// <{tagName}");
        if (attributes != null)
            foreach (var attribute in attributes)
                code.Append($" {attribute.Key}=\"{attribute.Value}\"");

        code.Append(">");

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines.Length > 1)
        {
            code.AppendLine();
            foreach (var line in content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                code.AppendLine($"{indent}/// {line.Trim()}");

            code.AppendLine($"{indent}/// </{tagName}>");
        }
        else
        {
            code.AppendLine($"{content}</{tagName}>");
        }


    }

    /// <summary>
    /// Generates a human readable description for the given endpoint responses. This includes available documentation
    /// for response codes below 200 or above 299.
    /// </summary>
    /// <param name="responses">The responses to document.</param>
    /// <returns>A string detailing the response codes and their description (if available).</returns>
    private static string BuildErrorDescription(IReadOnlyCollection<CSharpResponseModel> responses)
    {
        var errorDescription = new StringBuilder("Thrown when the request returns a non-success status code");
        var errorResponses = responses.Where(response => !HttpUtilities.IsSuccessStatusCode(response.StatusCode)).ToList();
        if (!errorResponses.Any())
            return errorDescription.Append(".").ToString();

        errorDescription.Append(":");
        foreach (var response in errorResponses)
        {
            errorDescription.AppendLine().Append(response.StatusCode);
            if (!string.IsNullOrWhiteSpace(response.ExceptionDescription))
                errorDescription.Append(": ").Append(response.ExceptionDescription);
        }

        return errorDescription.ToString();
    }
}