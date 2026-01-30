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
    /// The name of the XML documentation tag used for summaries.
    /// </summary>
    private const string SummaryTag = "summary";

    /// <summary>
    /// Instantiates a new instance of the <see cref="XmlDocumentationGenerator"/> class.
    /// </summary>
    /// <param name="settings">The code generation settings to use.</param>
    internal XmlDocumentationGenerator(RefitGeneratorSettings settings)
    {
        this._settings = settings;
    }

    /// <summary>
    /// Generates an interface description from the tags of the given OpenAPI document and appends it to the builder.
    /// </summary>
    /// <param name="document">The parent document of the controller.</param>
    /// <param name="tag">The controller tag that the endpoints were grouped by.</param>
    /// <param name="code">The builder to append the documentation to.</param>
    public void AppendInterfaceDocumentationByTag(OpenApiDocument document, string tag, StringBuilder code)
    {
        if (!_settings.GenerateXmlDocCodeComments)
        {
            return;
        }

        var controllerTag = document.Tags.FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
        var controllerDescription = controllerTag?.Description;
        if (!string.IsNullOrEmpty(controllerDescription))
        {
            this.AppendXmlCommentBlock(SummaryTag, EscapeSymbols(controllerDescription), code, indent: Separator);
        }
    }

    /// <summary>
    /// Generates an interface description from the summary of the given endpoint and appends it to the builder.
    /// </summary>
    /// <param name="endpoint">The OpenAPI definition of the endpoint.</param>
    /// <param name="code">The builder to append the documentation to.</param>
    public void AppendInterfaceDocumentationByEndpoint(OpenApiOperation endpoint, StringBuilder code)
    {
        if (!_settings.GenerateXmlDocCodeComments)
        {
            return;
        }

        var summary = endpoint.Summary;
        if (!string.IsNullOrEmpty(summary))
        {
            this.AppendXmlCommentBlock(SummaryTag, EscapeSymbols(summary), code, indent: Separator);
        }
    }

    /// <summary>
    /// Generates an interface description from the title of the given document and appends it to the builder.
    /// </summary>
    /// <param name="document">The OpenAPI definition of the document.</param>
    /// <param name="code">The builder to append the documentation to.</param>
    public void AppendSingleInterfaceDocumentation(OpenApiDocument document, StringBuilder code)
    {
        if (!_settings.GenerateXmlDocCodeComments)
        {
            return;
        }

        var title = document.Info?.Title;
        if (!string.IsNullOrEmpty(title))
        {
            this.AppendXmlCommentBlock(SummaryTag, EscapeSymbols(title), code, indent: Separator);
        }
    }

    /// <summary>
    /// Appends XML docs for the given method to the given code builder.
    /// </summary>
    /// <param name="method">The NSwag model of the method's OpenAPI definition.</param>
    /// <param name="hasApiResponse">Indicates whether the method returns an <c>ApiResponse</c>.</param>
    /// <param name="hasDynamicQuerystringParameter">Indicates whether the method gets a dynamic querystring parameter</param>
    /// <param name="hasApizrRequestOptionsParameter">Indicates whether the method gets an IApizrRequestOptions options final parameter</param>
    /// <param name="hasCancellationToken">Indicates whether the method gets a cancellation token parameter</param>
    /// <param name="code">The builder to append the documentation to.</param>
    public void AppendMethodDocumentation(
        CSharpOperationModel method,
        bool hasApiResponse,
        bool hasDynamicQuerystringParameter,
        bool hasApizrRequestOptionsParameter,
        bool hasCancellationToken,
        StringBuilder code)
    {
        if (!_settings.GenerateXmlDocCodeComments)
            return;

        if (!string.IsNullOrWhiteSpace(method.Summary))
            this.AppendXmlCommentBlock(SummaryTag, EscapeSymbols(method.Summary), code);

        if (!string.IsNullOrWhiteSpace(method.Description))
            this.AppendXmlCommentBlock("remarks", EscapeSymbols(method.Description), code);

        foreach (var parameter in method.Parameters)
        {
            if (parameter == null)
                continue;

            var description = parameter.HasDescription
                ? parameter.Description
                : $"{parameter.VariableName} parameter";

            this.AppendXmlCommentBlock(
                "param",
                description,
                code,
                new Dictionary<string, string>
                {
                    ["name"] = parameter.VariableName
                });
        }

        if (hasDynamicQuerystringParameter)
        {
            this.AppendXmlCommentBlock(
                "param",
                "The dynamic querystring parameter wrapping all others.",
                code,
                new Dictionary<string, string>
                {
                    ["name"] = "queryParams"
                });
        }

        if (hasApizrRequestOptionsParameter)
        {
            this.AppendXmlCommentBlock(
                "param",
                "The <see cref=\"IApizrRequestOptions\"/> instance to pass through the request.",
                code,
                new Dictionary<string, string>
                {
                    ["name"] = "options"
                });
        }

        if (hasCancellationToken)
        {
            this.AppendXmlCommentBlock(
                "param",
                "The cancellation token to cancel the request.",
                code,
                new Dictionary<string, string>
                {
                    ["name"] = "cancellationToken"
                });
        }

        if (hasApiResponse)
        {
            this.AppendXmlCommentBlock("returns", this.BuildApiResponseDescription(method.Responses), code);
        }
        else
        {
            if (method.HasResult)
            {
                // Document the result with a fallback description.
                var description = method.ResultDescription;
                if (string.IsNullOrWhiteSpace(description))
                    description = "A <see cref=\"Task\"/> representing the result of the request.";
                this.AppendXmlCommentBlock("returns", description, code);
            }
            else
            {
                // Document the returned task even when there is no result.
                this.AppendXmlCommentBlock(
                    "returns",
                    "A <see cref=\"Task\"/> that completes when the request is finished.",
                    code);
            }

            this.AppendXmlCommentBlock(
                "exception",
                this.BuildErrorDescription(method.Responses),
                code,
                new Dictionary<string, string>
                {
                    ["cref"] = "ApiException"
                });
        }
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

        var lines = content.Split(
            new[]
            {
                "\r\n", "\r", "\n"
            },
            StringSplitOptions.None);
        if (lines.Length > 1)
        {
            // When working with multiple lines, place the content on a separate line with normalized linebreaks.
            code.AppendLine();
            foreach (var line in content.Split(
                new[]
                {
                    "\r\n", "\r", "\n"
                },
                StringSplitOptions.None))
                code.AppendLine($"{indent}/// {line.Trim()}");

            code.AppendLine($"{indent}/// </{tagName}>");
        }
        else
        {
            // When the content only has a single line, place it on the same line as the tag.
            code.AppendLine($"{content}</{tagName}>");
        }
    }

    /// <summary>
    /// Generates a human readable error description for the given endpoint responses. This includes available
    /// documentation for response codes below 200 or above 299 if the
    /// <see cref="RefitGeneratorSettings.GenerateStatusCodeComments"/> setting is enabled.
    /// </summary>
    /// <param name="responses">The responses to document.</param>
    /// <returns>A string detailing the error codes and their description.</returns>
    private string BuildErrorDescription(IEnumerable<CSharpResponseModel> responses)
    {
        return this.BuildResponseDescription(
            "Thrown when the request returns a non-success status code",
            responses.Where(response => !HttpUtilities.IsSuccessStatusCode(response.StatusCode)));
    }

    /// <summary>
    /// Generates a human readable result description for the given endpoint responses. This includes all documented
    /// response codes if the <see cref="RefitGeneratorSettings.GenerateStatusCodeComments"/> setting is enabled.
    /// </summary>
    /// <param name="responses">The responses to document.</param>
    /// <returns>A string detailing the response codes and their description.</returns>
    private string BuildApiResponseDescription(IEnumerable<CSharpResponseModel> responses)
    {
        return this.BuildResponseDescription(
            "A <see cref=\"Task\"/> representing the <see cref=\"IApiResponse\"/> instance containing the result",
            responses);
    }

    /// <summary>
    /// Generates a description for the given responses.
    /// </summary>
    /// <param name="text">The text to prepend to the responses.</param>
    /// <param name="responses">The responses to document.</param>
    /// <returns>A string containing the given text and response descriptions.</returns>
    private string BuildResponseDescription(string text, IEnumerable<CSharpResponseModel> responses)
    {
        var description = new StringBuilder(text);
        var responseList = responses.ToList();

        if (!this._settings.GenerateStatusCodeComments || !responseList.Any())
            return description.Append(".").ToString();

        description.AppendLine(":")
            .AppendLine("<list type=\"table\">")
            .AppendLine("<listheader>")
            .AppendLine("<term>Status</term>")
            .AppendLine("<description>Description</description>")
            .AppendLine("</listheader>");

        foreach (var response in responseList)
        {
            description
                .AppendLine("<item>")
                .Append("<term>")
                .Append(response.StatusCode)
                .AppendLine("</term>");

            if (!string.IsNullOrWhiteSpace(response.ExceptionDescription))
            {
                description
                    .Append("<description>")
                    .Append(response.ExceptionDescription)
                    .AppendLine("</description>");
            }

            description.AppendLine("</item>");
        }

        description
            .Append("</list>");

        return description.ToString();
    }

    private string EscapeSymbols(string input)
    {
        return input
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
