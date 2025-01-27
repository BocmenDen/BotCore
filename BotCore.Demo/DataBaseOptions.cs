namespace BotCore.Demo
{
    public class DataBaseOptions
    {
        public string? Path { get; set; }

        public string GetPathOrDefault()
            => string.IsNullOrWhiteSpace(Path) ? $"{System.IO.Path.GetRandomFileName()}.db" : Path;
    }
}
