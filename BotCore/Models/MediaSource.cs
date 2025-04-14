namespace BotCore.Models
{
    public record MediaSource : CollectionBotParameters
    {
        private static readonly HttpClient client = new();

        public string? Name;
        public string? Type;
        public string? Id;
        public string? MimeType;
        private readonly Func<Task<Stream>> _getStream;

        public string? Uri { get; private set; }
        public string? PathFile { get; private set; }

        public MediaSource(Func<Task<Stream>> getStream, CollectionBotParameters? parameters = null) : base(parameters)
            => _getStream = getStream;

        public Task<Stream> GetStream() => _getStream();

        public static MediaSource FromUri(string uri)
        {
            return new MediaSource(async () =>
            {
                HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            })
            {
                Type = Path.GetExtension(uri).Replace(".", string.Empty).Trim(),
                Name = Path.GetFileName(uri),
                Uri = uri
            };
        }

        public static MediaSource FromFile(string filePath)
        {
            filePath = Path.GetFullPath(filePath);
            return new MediaSource(() =>
            {
                Stream stream = File.OpenRead(filePath);
                return Task.FromResult<Stream>(stream);
            })
            {
                Type = Path.GetExtension(filePath).Replace(".", string.Empty).Trim(),
                Name = Path.GetFileName(filePath),
                PathFile = filePath
            };
        }

    }
}
