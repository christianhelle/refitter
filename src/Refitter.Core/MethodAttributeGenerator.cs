using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class MethodAttributeGenerator : IMethodAttributeGenerator
{
    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;

    public MethodAttributeGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document)
    {
        this.settings = settings;
        this.document = document;
    }

    public string[] Generate(OpenApiOperation operation, CSharpOperationModel operationModel)
    {
        var attributes = new List<string>();

        if (operation.IsDeprecated)
        {
            attributes.Add("[System.Obsolete]");
        }

        if (operationModel.Consumes.Contains("multipart/form-data"))
        {
            attributes.Add("[Multipart]");
        }

        var headers = new List<string>();

        if (settings.AddAcceptHeaders && document.SchemaType is >= NJsonSchema.SchemaType.OpenApi3)
        {
            var uniqueContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var response in operation.Responses.Values)
            {
                if (response.Content == null)
                    continue;

                foreach (var contentType in response.Content.Keys)
                {
                    uniqueContentTypes.Add(contentType);
                }
            }

            if (uniqueContentTypes.Any())
            {
                headers.Add($"\"Accept: {string.Join(", ", uniqueContentTypes)}\"");
            }
        }

        if (settings.AddContentTypeHeaders && document.SchemaType is >= NJsonSchema.SchemaType.OpenApi3)
        {
            var uniqueContentTypes = operation.RequestBody?.Content.Keys ?? Array.Empty<string>();
            var contentType =
                uniqueContentTypes.FirstOrDefault(c => c.Equals("application/json", StringComparison.OrdinalIgnoreCase)) ??
                uniqueContentTypes.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(contentType) && !operationModel.Consumes.Contains("multipart/form-data"))
            {
                headers.Add($"\"Content-Type: {contentType}\"");
            }
        }

        if (settings.AuthenticationHeaderStyle == AuthenticationHeaderStyle.Method)
        {
            foreach (var securitySchemeName in operationModel.Security.SelectMany(x => x.Keys))
            {
                if ((settings.SecurityScheme != null && securitySchemeName != settings.SecurityScheme) ||
                    !document.SecurityDefinitions.TryGetValue(securitySchemeName, out var securityScheme))
                {
                    continue;
                }

                if (securityScheme is { Type: OpenApiSecuritySchemeType.Http, Scheme: var scheme }
                    && string.Equals(scheme, "bearer", StringComparison.OrdinalIgnoreCase))
                {
                    headers.Add("\"Authorization: Bearer\"");
                }
            }
        }

        if (headers.Any())
        {
            attributes.Add($"[Headers({string.Join(", ", headers)})]");
        }

        return attributes.ToArray();
    }
}
