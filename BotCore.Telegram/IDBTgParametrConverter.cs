using BotCore.Attributes;
using BotCore.Interfaces;
using Telegram.Bot.Types;

namespace BotCore.Tg
{
    [Service<IDBClientParametrConverter<Chat>>(ServiceType.Singltone)]
    public class IDBTgParametrConverter : IDBClientParametrConverter<Chat>
    {
        public long ParametrConvert(Chat parametr) => parametr.Id;
    }
}
