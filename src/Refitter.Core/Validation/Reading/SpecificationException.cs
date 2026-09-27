namespace Refitter.Core.Validation.Reading;

/// <summary>
/// A problem found while reading an OpenAPI document, reported as a validation error at <see cref="Pointer"/>.
/// </summary>
/// <remarks>The validator reproduces Microsoft.OpenApi (MIT license), where this is <c>OpenApiException</c>.</remarks>
internal class SpecificationException(string message) : Exception(message)
{
    public string? Pointer { get; set; }
}

/// <summary>
/// A structural problem found while reading an OpenAPI document, such as a map where a list was expected.
/// </summary>
/// <remarks>The validator reproduces Microsoft.OpenApi (MIT license), where this is <c>OpenApiReaderException</c>.</remarks>
internal sealed class SpecificationReaderException : SpecificationException
{
    public SpecificationReaderException(string message)
        : base(message)
    {
    }

    public SpecificationReaderException(string message, ParsingContext context)
        : base(message)
    {
        Pointer = context.GetLocation();
    }
}
