using System.Globalization;

namespace Refitter.Core.Validation;

/// <summary>
/// Exception thrown when an OpenAPI document declares a specification version that cannot be validated.
/// </summary>
public class UnsupportedSpecificationVersionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedSpecificationVersionException"/> class.
    /// </summary>
    /// <param name="specificationVersion">The specification version declared by the document.</param>
    /// <param name="innerException">The exception that caused this exception, if any.</param>
    public UnsupportedSpecificationVersionException(string specificationVersion, Exception? innerException = null)
        : base(
            string.Format(
                CultureInfo.InvariantCulture,
                "OpenAPI specification version '{0}' is not supported.",
                specificationVersion),
            innerException)
    {
        SpecificationVersion = specificationVersion;
    }

    /// <summary>
    /// Gets the specification version declared by the document.
    /// </summary>
    public string SpecificationVersion { get; }
}
