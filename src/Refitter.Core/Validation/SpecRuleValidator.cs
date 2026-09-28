using System.Text.RegularExpressions;
using Refitter.Core.Validation.Model;

namespace Refitter.Core.Validation;

/// <summary>
/// Applies the default validation rules of Microsoft.OpenApi (MIT license) to a <see cref="SpecDocument"/>,
/// walking it in the same order and reporting each problem at the same JSON pointer.
/// </summary>
internal sealed class SpecRuleValidator
{
    private static readonly Regex ComponentKey = new("^[a-zA-Z0-9\\.\\-_]+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
    private static readonly Regex StatusCode = new("^[1-5](?>[0-9]{2}|[xX]{2})$", RegexOptions.None, TimeSpan.FromMilliseconds(100));

    private readonly SpecComponents? registered;
    private readonly Stack<string> path = new();
    private readonly Stack<SpecSchema> schemaLoop = new();
    private readonly Stack<SpecPathItem> pathItemLoop = new();
    private readonly List<ValidationIssue> errors = [];
    private readonly List<ValidationIssue> warnings = [];

    private SpecRuleValidator(SpecComponents? registered)
    {
        this.registered = registered;
    }

    /// <summary>
    /// Adds the rule violations of the document to the diagnostics: errors first, then warnings.
    /// </summary>
    public static void Validate(SpecDocument? document, ValidationDiagnostics diagnostics)
    {
        if (document == null)
            return;

        var validator = new SpecRuleValidator(document.Components);
        validator.Walk(document);

        foreach (var error in validator.errors)
        {
            diagnostics.Errors.Add(error);
        }

        foreach (var warning in validator.warnings)
        {
            diagnostics.Warnings.Add(warning);
        }
    }

    private string PathString => "#/" + string.Join("/", path.Reverse());

    private void Enter(string segment) =>
        path.Push(string.IsNullOrEmpty(segment) ? string.Empty : segment.Replace("~", "~0").Replace("/", "~1"));

    private void Exit() => path.Pop();

    private void Error(string message) => errors.Add(new ValidationIssue(PathString, message));

    private void ErrorAt(string segment, string message)
    {
        Enter(segment);
        Error(message);
        Exit();
    }

    private void Within(string segment, Action walk)
    {
        Enter(segment);
        walk();
        Exit();
    }

    private void WithinEach<T>(string segment, IDictionary<string, T>? items, Action<T> walk)
    {
        if (items == null || items.Count == 0)
            return;

        Within(segment, () =>
        {
            foreach (var item in items)
            {
                Within(item.Key, () => walk(item.Value));
            }
        });
    }

    private static string FieldIsRequired(string field, string element) =>
        $"The field '{field}' in '{element}' object is REQUIRED.";

    private void Walk(SpecDocument document)
    {
        if (document.Info == null)
            ErrorAt("info", FieldIsRequired("info", "document"));

        if (document.Info != null)
            Within("info", () => Walk(document.Info));

        if (document.Servers != null)
        {
            Within("servers", () =>
            {
                for (var i = 0; i < document.Servers.Count; i++)
                {
                    var server = document.Servers[i];
                    Within(i.ToString(System.Globalization.CultureInfo.InvariantCulture), () => Walk(server));
                }
            });
        }

        if (document.Paths != null)
            Within("paths", () => Walk(document.Paths));

        WithinEach("webhooks", document.Webhooks, Walk);

        if (document.Components != null)
            Within("components", () => Walk(document.Components));

        if (document.ExternalDocs != null)
            Within("externalDocs", () => Walk(document.ExternalDocs));

        if (document.Tags != null)
        {
            Within("tags", () =>
            {
                for (var i = 0; i < document.Tags.Count; i++)
                {
                    var tag = document.Tags[i];
                    Within(i.ToString(System.Globalization.CultureInfo.InvariantCulture), () => Walk(tag));
                }
            });
        }
    }

    private void Walk(SpecInfo info)
    {
        if (info.Title == null)
            ErrorAt("title", FieldIsRequired("title", "info"));

        if (info.Version == null)
            ErrorAt("version", FieldIsRequired("version", "info"));

        if (info.Contact != null)
        {
            Within("contact", () =>
            {
                var email = info.Contact.Email;
                if (email != null && !IsEmailAddress(email))
                    ErrorAt("email", $"The string '{email}' MUST be in the format of an email address.");
            });
        }

        if (info.License != null)
        {
            Within("license", () =>
            {
                if (info.License.Name == null)
                    ErrorAt("name", FieldIsRequired("name", "license"));
            });
        }
    }

    private static bool IsEmailAddress(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var parts = input.Split('@');
        return parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]);
    }

    private void Walk(SpecServer server)
    {
        if (server.Url == null)
            ErrorAt("url", FieldIsRequired("url", "server"));

        if (server.Variables != null)
        {
            Within("variables", () =>
            {
                foreach (var variable in server.Variables)
                {
                    Within(variable.Key, () =>
                    {
                        if (string.IsNullOrEmpty(variable.Value!.Default))
                            ErrorAt("default", FieldIsRequired("default", variable.Key));
                    });
                }
            });
        }
    }

    private void Walk(SpecExternalDocs externalDocs)
    {
        if (externalDocs.Url == null)
            ErrorAt("url", FieldIsRequired("url", "External Documentation"));
    }

    private void Walk(SpecTag tag)
    {
        if (tag.Name == null)
            ErrorAt("name", FieldIsRequired("name", "tag"));

        // Microsoft.OpenApi checks the external docs of a tag without entering them
        if (tag.ExternalDocs != null)
            Walk(tag.ExternalDocs);
    }

    private void Walk(SpecPaths paths)
    {
        foreach (var key in paths.Keys)
        {
            Enter(key);
            if (!key.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                Error($"The path item name '{key}' MUST begin with a slash.");

            Exit();
        }

        var signatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in paths.Keys)
        {
            var signature = GetPathSignature(key);
            if (!signatures.Add(signature))
                ErrorAt(key, $"The path signature '{signature}' MUST be unique.");
        }

        foreach (var pathItem in paths)
        {
            Within(pathItem.Key, () => Walk(pathItem.Value));
        }
    }

    private static string GetPathSignature(string path)
    {
        for (var start = path.IndexOf('{'); start > -1; start = path.IndexOf('{', start + 2))
        {
            var end = path.IndexOf('}', start);
            if (end < 0)
                return path;

            path = path.Substring(0, start + 1) + path.Substring(end);
        }

        return path;
    }

    private void Walk(SpecPathItem? pathItem)
    {
        if (pathItem is not { Reference: null } || pathItemLoop.Contains(pathItem))
            return;

        pathItemLoop.Push(pathItem);
        if (pathItem.Parameters != null)
            Within("parameters", () => Walk(pathItem.Parameters));

        if (pathItem.Operations != null)
        {
            foreach (var operation in pathItem.Operations)
            {
                Within(operation.Key, () => Walk(operation.Value));
            }
        }

        pathItemLoop.Pop();
    }

    private void Walk(SpecOperation operation)
    {
        if (operation.Parameters != null)
            Within("parameters", () => Walk(operation.Parameters));

        if (operation.RequestBody != null)
            Within("requestBody", () => Walk(operation.RequestBody));

        if (operation.Responses != null)
            Within("responses", () => Walk(operation.Responses));

        WithinEach("callbacks", operation.Callbacks, Walk);
    }

    private void Walk(List<SpecParameter> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            Within(i.ToString(System.Globalization.CultureInfo.InvariantCulture), () => Walk(parameter));
        }
    }

    private void Walk(SpecParameter? parameter)
    {
        if (parameter is not { Reference: null })
            return;

        if (parameter.Name == null)
            ErrorAt("name", FieldIsRequired("name", "parameter"));

        if (parameter.In == null)
            ErrorAt("in", FieldIsRequired("in", "parameter"));

        if (parameter.In == SpecParameterLocation.Path && !parameter.Required)
            ErrorAt("required", "\"required\" must be true when parameter location is \"path\"");

        var encodedName = string.IsNullOrEmpty(parameter.Name) ? string.Empty : parameter.Name!.Replace("~", "~0").Replace("/", "~1");
        if (parameter.In == SpecParameterLocation.Path
            && !PathString.Contains("{" + encodedName + "}")
            && !PathString.Contains("#/components"))
        {
            ErrorAt("in", $"Declared path parameter \"{parameter.Name}\" needs to be defined as a path parameter at either the path or operation level");
        }

        if (parameter.Schema != null)
            Within("schema", () => Walk(parameter.Schema));

        if (parameter.Content != null)
            Within("content", () => Walk(parameter.Content));
    }

    private void Walk(SpecRequestBody? requestBody)
    {
        if (requestBody is not { Reference: null })
            return;

        if (requestBody.Content != null)
            Within("content", () => Walk(requestBody.Content));
    }

    private void Walk(SpecResponses responses)
    {
        if (responses.Count == 0)
            Error("Responses must contain at least one response");

        foreach (var key in responses.Keys)
        {
            if (!"default".Equals(key, StringComparison.OrdinalIgnoreCase) && !StatusCode.IsMatch(key))
            {
                ErrorAt(key, "Responses key must be 'default', an HTTP status code, or one of the following strings representing a range of HTTP status codes: '1XX', '2XX', '3XX', '4XX', '5XX' (case insensitive)");
            }
        }

        foreach (var response in responses)
        {
            if (response.Value != null)
                Within(response.Key, () => Walk(response.Value));
        }
    }

    private void Walk(SpecResponse? response)
    {
        if (response is not { Reference: null })
            return;

        if (response.Description == null)
            ErrorAt("description", FieldIsRequired("description", "response"));

        if (response.Content != null)
            Within("content", () => Walk(response.Content));

        WithinEach("links", response.Links, Walk);
        WithinEach("headers", response.Headers, Walk);
    }

    private void Walk(Dictionary<string, SpecMediaType?> content)
    {
        foreach (var mediaType in content)
        {
            if (mediaType.Value != null)
                Within(mediaType.Key, () => Walk(mediaType.Value));
        }
    }

    private void Walk(SpecMediaType? mediaType)
    {
        if (mediaType is not { Reference: null })
            return;

        if (mediaType.Schema != null)
            Within("schema", () => Walk(mediaType.Schema));

        if (mediaType.Encoding != null)
        {
            Within("encoding", () =>
            {
                foreach (var encoding in mediaType.Encoding)
                {
                    if (encoding.Value?.Headers == null)
                        continue;

                    Within(encoding.Key, () =>
                    {
                        foreach (var header in encoding.Value.Headers)
                        {
                            if (header.Value != null)
                                Within(header.Key, () => Walk(header.Value));
                        }
                    });
                }
            });
        }
    }

    private void Walk(SpecHeader? header)
    {
        if (header is not { Reference: null })
            return;

        if (header.Content != null)
            Within("content", () => Walk(header.Content));

        if (header.Schema != null)
            Within("schema", () => Walk(header.Schema));
    }

    private void Walk(SpecLink? link)
    {
        if (link is not { Reference: null })
            return;

        if (link.Server != null)
            Within("server", () => Walk(link.Server));
    }

    private void Walk(SpecCallback? callback)
    {
        if (callback is not { Reference: null } || callback.PathItems == null)
            return;

        foreach (var pathItem in callback.PathItems)
        {
            if (pathItem.Value != null)
                Within(pathItem.Key, () => Walk(pathItem.Value));
        }
    }

    private void Walk(SpecComponents components)
    {
        ValidateKeys(components.Schemas?.Keys, "schemas");
        ValidateKeys(components.Responses?.Keys, "responses");
        ValidateKeys(components.Parameters?.Keys, "parameters");
        ValidateKeys(components.Examples?.Keys, "examples");
        ValidateKeys(components.RequestBodies?.Keys, "requestBodies");
        ValidateKeys(components.Headers?.Keys, "headers");
        ValidateKeys(components.SecuritySchemes?.Keys, "securitySchemes");
        ValidateKeys(components.Links?.Keys, "links");
        ValidateKeys(components.Callbacks?.Keys, "callbacks");

        WithinEach("schemas", components.Schemas, Walk);
        WithinEach("callbacks", components.Callbacks, Walk);
        WithinEach("pathItems", components.PathItems, Walk);
        WithinEach("parameters", components.Parameters, Walk);
        WithinEach("headers", components.Headers, Walk);
        WithinEach("links", components.Links, Walk);
        WithinEach("requestBodies", components.RequestBodies, Walk);
        WithinEach("responses", components.Responses, Walk);
        WithinEach("mediaTypes", components.MediaTypes, Walk);
    }

    private void ValidateKeys(IEnumerable<string>? keys, string component)
    {
        if (keys == null)
            return;

        foreach (var key in keys)
        {
            if (!ComponentKey.IsMatch(key))
                Error($"The key '{key}' in '{component}' of components MUST match the regular expression '{ComponentKey}'.");
        }
    }

    private void Walk(SpecSchema? schema)
    {
        if (schema == null)
            return;

        if (schema.Reference != null)
        {
            ValidateSchemaReference(schema.Reference);
            return;
        }

        if (schemaLoop.Contains(schema))
            return;

        schemaLoop.Push(schema);

        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties.Where(property => property.Value == null))
            {
                ErrorAt(property.Key, $"Schema  property {property.Key} is null.");
            }
        }

        var discriminator = schema.Discriminator?.PropertyName;
        if (schema.Discriminator != null && !ValidateChildSchemaAgainstDiscriminator(schema, discriminator))
        {
            ErrorAt("discriminator", $"Schema  must contain property specified in the discriminator {discriminator} in the required field list.");
        }

        if (schema.Items != null)
            Within("items", () => Walk(schema.Items));

        if (schema.Not != null)
            Within("not", () => Walk(schema.Not));

        WalkSchemas("allOf", schema.AllOf);
        WalkSchemas("anyOf", schema.AnyOf);
        WalkSchemas("oneOf", schema.OneOf);
        WithinEach("properties", schema.Properties, Walk);

        if (schema.AdditionalProperties != null)
            Within("additionalProperties", () => Walk(schema.AdditionalProperties));

        if (schema.Discriminator != null)
        {
            Within("discriminator", () => WithinEach<SpecSchema>("mapping", schema.Discriminator.Mapping, Walk));
        }

        if (schema.ExternalDocs != null)
            Within("externalDocs", () => Walk(schema.ExternalDocs));

        schemaLoop.Pop();
    }

    private void WalkSchemas(string segment, List<SpecSchema>? schemas)
    {
        if (schemas == null || schemas.Count == 0)
            return;

        Within(segment, () =>
        {
            for (var i = 0; i < schemas.Count; i++)
            {
                var schema = schemas[i];
                Within(i.ToString(System.Globalization.CultureInfo.InvariantCulture), () => Walk(schema));
            }
        });
    }

    private bool ValidateChildSchemaAgainstDiscriminator(SpecSchema schema, string? discriminator)
    {
        if (discriminator == null)
            return false;

        if (schema.Required != null && schema.Required.Contains(discriminator))
            return true;

        if (schema.OneOf is { Count: > 0 })
            return TraverseSchemaElements(discriminator, schema.OneOf);

        if (schema.AnyOf is { Count: > 0 })
            return TraverseSchemaElements(discriminator, schema.AnyOf);

        if (schema.AllOf is { Count: > 0 })
            return TraverseSchemaElements(discriminator, schema.AllOf);

        return false;
    }

    /// <summary>
    /// Microsoft.OpenApi only looks at the first child schema.
    /// </summary>
    private bool TraverseSchemaElements(string discriminator, List<SpecSchema> children)
    {
        var child = children[0];
        var resolved = SpecReferences.Resolve(child, c => c.Schemas, registered);
        if (resolved?.Properties != null && !resolved.Properties.ContainsKey(discriminator))
        {
            if (resolved.Required != null && !resolved.Required.Contains(discriminator))
                return ValidateChildSchemaAgainstDiscriminator(resolved, discriminator);
        }

        return true;
    }

    /// <summary>
    /// Warns about a local schema reference that does not resolve, as the reference rule of Microsoft.OpenApi does.
    /// </summary>
    private void ValidateSchemaReference(SpecReference reference)
    {
        if (reference.ExternalResource != null)
            return;

        string? warning;
        try
        {
            warning = SchemaReferences.ResolveRecursive(reference, registered) == null
                ? $"The schema reference '{SchemaReferences.ReferenceV3(reference)}' does not point to an existing schema."
                : null;
        }
        catch (InvalidOperationException exception)
        {
            warning = exception.Message;
        }

        if (warning != null)
        {
            Enter("$ref");
            warnings.Add(new ValidationIssue(PathString, warning));
            Exit();
        }
    }
}
