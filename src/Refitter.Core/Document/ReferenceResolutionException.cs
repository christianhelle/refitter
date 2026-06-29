namespace Refitter.Core;

/// <summary>
/// Thrown when an OpenAPI document contains a <c>$ref</c> that Refitter refuses to resolve, such as a
/// remote reference (when remote references are disabled) or a local reference that escapes the input
/// document's directory tree.
/// </summary>
public sealed class ReferenceResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceResolutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ReferenceResolutionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceResolutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReferenceResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
