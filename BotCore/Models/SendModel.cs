using System.Text;

namespace BotCore.Models
{
    public record class SendModel : CollectionBotParameters
    {
        private IReadOnlyList<MediaSource>? _medias;

        public string? Message;
        public ButtonsSend? Keyboard;
        public ButtonsSend? Inline;
        public IReadOnlyList<MediaSource>? Medias
        {
            get => _medias;
            set => _medias = (value?.Any() ?? false) ? value : null;
        }

        public static implicit operator SendModel(string text) => new() { Message = text };
        public static implicit operator SendModel(StringBuilder builder) => builder.ToString();

        public static SendModel operator +(SendModel sending, string text) => sending.Message += text;
    }
}
