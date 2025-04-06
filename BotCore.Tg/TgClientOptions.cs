using Telegram.Bot.Polling;

namespace BotCore.Tg
{
    public class TgClientOptions
    {
        public required string Token { get; set; }
        public ReceiverOptions? ReceiverOptions { get; set; }
    }
}
