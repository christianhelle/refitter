using System.Collections.Generic;

namespace Refitter.MSBuild;

/// <summary>
/// Abstraction for discovering installed .NET runtimes
/// </summary>
public interface IRuntimeResolver
{
    /// <summary>
    /// Gets the list of installed .NET runtimes
    /// </summary>
    List<string> GetInstalledRuntimes();
}
