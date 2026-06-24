namespace Dhblog.Database;

public static class TopicNormalizer
{
    public static string Normalize(string displayText) =>
        new string(displayText.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
}
