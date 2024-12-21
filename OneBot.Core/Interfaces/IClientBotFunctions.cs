using OneBot.Models;

namespace OneBot.Interfaces
{
    public interface IClientBotFunctions
    {
        public ButtonSearch? GetIndexButton(UpdateModel update, ButtonsSend buttonsSend);
    }
}
