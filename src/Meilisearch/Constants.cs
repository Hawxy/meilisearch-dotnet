namespace Meilisearch
{
    /// <summary>
    /// Shared messages for the Meilisearch client.
    /// </summary>
    internal static class Constants
    {
        internal static string VersionErrorHintMessage(string message, string method)
        {
            return
                $"{message}\nHint: It might not be working because maybe you're not up to date with the Meilisearch version that ${method} call requires.";
        }
    }
}
