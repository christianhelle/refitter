namespace Refitter.Core;

/// <summary>
/// Represents a user-facing configuration warning message.
/// </summary>
/// <param name="Title">The short title of the warning.</param>
/// <param name="Description">The extended description of the warning.</param>
public record Warning(string Title, string Description);
