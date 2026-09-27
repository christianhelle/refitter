namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Checks the values Microsoft.OpenApi (MIT license) parses while reading, failing where it fails.
/// </summary>
internal static class RuntimeExpressions
{
    /// <summary>
    /// Checks a runtime expression, such as a callback key or a link parameter.
    /// </summary>
    public static void Validate(string expression)
    {
        CheckArgumentNullOrEmpty(expression, "expression");
        if (!expression.StartsWith("$", StringComparison.OrdinalIgnoreCase))
        {
            // A template such as {$request.body#/url} holds expressions in braces
            foreach (System.Text.RegularExpressions.Match match in ContainedExpression.Matches(expression))
            {
                Validate(match.Groups["exp"].Captures[0].Value);
            }

            return;
        }

        if (expression.Equals("$url", StringComparison.Ordinal)
            || expression.Equals("$method", StringComparison.Ordinal)
            || expression.Equals("$statusCode", StringComparison.Ordinal))
        {
            return;
        }

        if (expression.StartsWith("$request.", StringComparison.Ordinal))
        {
            ValidateSource(expression.Substring("$request.".Length));
            return;
        }

        if (expression.StartsWith("$response.", StringComparison.Ordinal))
        {
            ValidateSource(expression.Substring("$response.".Length));
            return;
        }

        throw new SpecificationException($"The runtime expression '{expression}' has invalid format.");
    }

    private static readonly System.Text.RegularExpressions.Regex ContainedExpression =
        new("{(?<exp>\\$[^}]*)");

    private static void ValidateSource(string expression)
    {
        if (!string.IsNullOrWhiteSpace(expression))
        {
            var parts = expression.Split('.');
            if (parts.Length == 2)
            {
                if (expression.StartsWith("header.", StringComparison.Ordinal))
                {
                    CheckArgumentNullOrEmpty(parts[1], "token");
                    return;
                }

                if (expression.StartsWith("query.", StringComparison.Ordinal)
                    || expression.StartsWith("path.", StringComparison.Ordinal))
                {
                    CheckArgumentNullOrEmpty(parts[1], "name");
                    return;
                }
            }

            if (expression.StartsWith("body", StringComparison.Ordinal))
                return;
        }

        throw new SpecificationException($"The source expression '{expression}' has invalid format.");
    }

    private static void CheckArgumentNullOrEmpty(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(parameterName, "Value cannot be null or empty: " + parameterName);
    }
}

/// <summary>
/// Checks schema types the way Microsoft.OpenApi (MIT license) maps them.
/// </summary>
internal static class SchemaTypes
{
    private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        "null", "boolean", "integer", "int", "decimal", "double", "number", "float", "string", "array", "object", "file",
    };

    /// <exception cref="SpecificationException">Thrown for a type Microsoft.OpenApi does not know.</exception>
    public static void ToJsonSchemaType(this string identifier)
    {
        if (!Known.Contains(identifier))
            throw new SpecificationException($"Invalid schema type identifier: {identifier}");
    }
}
