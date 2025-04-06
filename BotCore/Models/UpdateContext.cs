using BotCore.Interfaces;

namespace BotCore.Models
{
    public record UpdateContext<TUser>(IClientBot<TUser, UpdateContext<TUser>> bot, TUser user, UpdateModel update, Func<SendModel, Task> reply) : IUpdateContext<TUser>
        where TUser : IUser
    {
        public readonly IClientBot<TUser, UpdateContext<TUser>> Bot = bot??throw new ArgumentNullException(nameof(bot));

        public IClientBotFunctions BotFunctions => Bot;

        public TUser User { get; private set; } = user;

        public UpdateModel Update { get; private set; } = update??throw new ArgumentNullException(nameof(update));

        public Task Reply(SendModel send) => reply(send);
    }
}
