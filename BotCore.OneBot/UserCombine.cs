namespace BotCore.OneBot
{
    public readonly struct UserLinkInfo(string sourceName, long sourceId)
    {
        public readonly string SourceName = sourceName??throw new ArgumentNullException(nameof(sourceName));
        public readonly long SourceId = sourceId;
    }
}
