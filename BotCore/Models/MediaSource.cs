namespace BotCore.Models
{
    public record class MediaSource : CollectionBotParameters
    {
        public string? Name;
        public string? Type;
        public string? Id;
        public string? MimeType;
        private readonly Func<Task<Stream>> _getStream;

        public MediaSource(Func<Task<Stream>> getStraem, CollectionBotParameters? parameters = null) : base(parameters)
            => _getStream = getStraem;

        public Task<Stream> GetStream() => _getStream();
    }
}
