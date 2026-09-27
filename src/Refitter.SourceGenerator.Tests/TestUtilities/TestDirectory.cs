namespace Refitter.SourceGenerators.Tests.TestUtilities;

/// <summary>
/// Provides a per test run temporary directory that is deleted when the test session ends
/// </summary>
public static class TestDirectory
{
    /// <summary>
    /// The root folder for all temporary files created during this test run
    /// </summary>
    public static readonly string Root = Path.Combine(
        Path.GetTempPath(),
        "refitter-sourcegenerator-tests",
        Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Creates a new uniquely named folder under <see cref="Root"/>
    /// </summary>
    /// <returns>The path to the created folder</returns>
    public static string CreateFolder()
    {
        var folder = Path.Combine(Root, Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }

    [After(HookType.TestSession)]
    public static void Cleanup()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
