using BotCore.Attributes;
using BotCore.Interfaces;
using Telegram.Bot.Types;

namespace BotCore.Tg
{
    [Service<IDBClientParameterConverter<Chat>>(ServiceType.Singltone)]
    public class IDBTgParameterConverter : IDBClientParameterConverter<Chat>
    {
        public long ParameterConvert(Chat parameter) => parameter.Id;
    }
}
