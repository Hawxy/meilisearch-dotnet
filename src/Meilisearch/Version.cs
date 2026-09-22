namespace Meilisearch
{
    /// <summary>
    /// Information regarding an API key for the Meilisearch server.
    /// </summary>
    public class Version
    {
        private static readonly string s_version = typeof(Version).Assembly.GetName().Version.ToString(3);
        private static readonly string s_qualifiedVersion = $"Meilisearch .NET (v{s_version})";

        /// <summary>
        /// Extracts version from Meilisearch.csproj.
        /// </summary>
        /// <returns>Returns a formatted version.</returns>
        public string GetQualifiedVersion()
        {
            return s_qualifiedVersion;
        }

        /// <summary>
        /// Extracts the "major.minor.build" version from Meilisearch.csproj.
        /// </summary>
        /// <returns>Returns a version from the GetType as String.</returns>
        public string GetVersion()
        {
            return s_version;
        }
    }
}
