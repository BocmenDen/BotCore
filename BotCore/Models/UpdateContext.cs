using BotCore.Base;
using BotCore.Interfaces;

namespace BotCore.Models
{
    public record UpdateContext<TUser>(ClientBot<TUser, UpdateContext<TUser>> Bot, TUser User, UpdateModel Update) : IUpdateContext<TUser>
        where TUser : IUser
    {
        public readonly ClientBot<TUser, UpdateContext<TUser>> Bot = Bot??throw new ArgumentNullException(nameof(Bot));

        public IClientBotFunctions BotFunctions => Bot;

        public TUser User { get; private set; } = User;

        public UpdateModel Update { get; private set; } = Update??throw new ArgumentNullException(nameof(Update));

        public Task Reply(SendModel send) => Bot.Send(User, send, Update);
    }
}
