namespace Refitter.Core
{
    /// <summary>
    /// Helper class to retrieve the version of the Refitter library.
    /// </summary>
    public static class VersionHelper
    {
        /// <summary>
        /// Get the Refitter version
        /// </summary>
        /// <returns>
        /// Returns the version of Refitter, appending "(local build)" if it's a local build.
        /// </returns>
        public static string GetVersion()
        {
            var version = typeof(GeneratorPipeline).Assembly.GetName().Version!.ToString();
            if (version == "1.0.0.0")
                version += " (local build)";
            return version;
        }
    }
}
