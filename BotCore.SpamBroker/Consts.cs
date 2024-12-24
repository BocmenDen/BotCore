using Microsoft.Extensions.Logging;

namespace BotCore.SpamBroker
{
    public class Consts
    {
        public const string LogForbidden = "Пользователь [{user}] продолжает спамить";
        public const string LogForbiddenFirst = "Подозрение на спам от пользователя [{user}]";

        public readonly static EventId EventIdForbidden = new(1, nameof(StateSpam.Forbidden));
        public readonly static EventId EventIdForbiddenFirst = new(2, nameof(StateSpam.ForbiddenFirst));
    }
}
