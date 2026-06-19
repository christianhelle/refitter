namespace Refitter.Core;

/// <summary>
/// Configuration for how operations are partitioned into Refit interfaces.
/// </summary>
public interface IPartitioningConfiguration
{
    /// <summary>
    /// Gets the strategy for partitioning operations into multiple interfaces.
    /// </summary>
    MultipleInterfaces MultipleInterfaces { get; }

    /// <summary>
    /// Gets the tags used to filter which endpoints to include.
    /// </summary>
    string[] IncludeTags { get; }

    /// <summary>
    /// Gets the path regex patterns used to filter which endpoints to include.
    /// </summary>
    string[] IncludePathMatches { get; }

    /// <summary>
    /// Gets the template for generating operation names.
    /// </summary>
    string? OperationNameTemplate { get; }

    /// <summary>
    /// Gets a value indicating whether to generate deprecated operations.
    /// </summary>
    bool GenerateDeprecatedOperations { get; }
}
