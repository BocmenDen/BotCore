using Telegram.Bot.Types;

namespace OneBot.Tg
{
    public interface IUserTgExtension
    {
        public Chat GetTgChat();
    }
}
