using OneBot.Models;

namespace OneBot.Interfaces
{
    public interface IUpdateContext<out TUser> where TUser : IUser
    {
        public IClientBotFunctions BotFunctions { get; }
        public TUser User { get; }
        public UpdateModel Update { get; }
        public Task Reply(SendModel send);
    }
}
