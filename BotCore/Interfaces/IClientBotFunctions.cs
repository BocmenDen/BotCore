using BotCore.Models;

namespace BotCore.Interfaces
{
    public interface IClientBotFunctions
    {
        public ButtonSearch? GetIndexButton(UpdateModel update, ButtonsSend buttonsSend);
    }
}
