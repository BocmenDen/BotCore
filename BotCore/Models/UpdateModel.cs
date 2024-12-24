namespace BotCore.Models
{
    public record class UpdateModel : CollectionBotParameters
    {
        private IReadOnlyList<MediaSource>? _medias;
        private string? _message;
        private string? _command;

        public UpdateType UpdateType;
        public object? OriginalMessage;
        public string? Message
        {
            get => _message;
            set => _message = string.IsNullOrWhiteSpace(value) ? null : value;
        }
        public IReadOnlyList<MediaSource>? Medias
        {
            get => _medias;
            set => _medias = (value?.Any() ?? false) ? value : null;
        }
        public string? Command
        {
            get => _command;
            set => _command = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
    [Flags]
    public enum UpdateType
    {
        None = 0,
        Message = 1,
        Keyboard = 2,
        Inline = 4,
        Media = 8,
        Command = 16
    }
}
