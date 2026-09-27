namespace Refitter.Core.Validation;

/// <summary>
/// The errors and warnings found while validating an OpenAPI document.
/// </summary>
public sealed class ValidationDiagnostics
{
    /// <summary>
    /// Gets the errors found in the document. Any error makes the document invalid.
    /// </summary>
    public IList<ValidationIssue> Errors { get; } = new List<ValidationIssue>();

    /// <summary>
    /// Gets the warnings found in the document.
    /// </summary>
    public IList<ValidationIssue> Warnings { get; } = new List<ValidationIssue>();

    /// <summary>
    /// Gets or sets the version of the OpenAPI specification the document was validated against.
    /// </summary>
    public OpenApiSpecificationVersion SpecificationVersion { get; set; }
}
