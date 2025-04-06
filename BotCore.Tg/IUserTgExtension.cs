using Telegram.Bot.Types;

namespace BotCore.Tg
{
    public interface IUserTgExtension
    {
        public Chat GetTgChat();
    }
}
