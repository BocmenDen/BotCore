using OneBot.Attributes;
using OneBot.Interfaces;
using Telegram.Bot.Types;

namespace OneBot.Tg
{
    [Service<IDBClientParametrConverter<Chat>>(ServiceType.Singltone)]
    public class IDBTgParametrConverter : IDBClientParametrConverter<Chat>
    {
        public long ParametrConvert(Chat parametr) => parametr.Id;
    }
}
