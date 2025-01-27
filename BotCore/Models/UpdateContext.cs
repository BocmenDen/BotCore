using BotCore.Base;
using BotCore.Interfaces;

namespace BotCore.Models
{
    public record class UpdateContext<TUser> : IUpdateContext<TUser> where TUser : IUser
    {
        public readonly ClientBot<TUser, UpdateContext<TUser>> Bot;

        public IClientBotFunctions BotFunctions => Bot;

        public TUser User { get; private set; }

        public UpdateModel Update { get; private set; }

        public Task Reply(SendModel send) => Bot.Send(User, send, Update);

        public UpdateContext(ClientBot<TUser, UpdateContext<TUser>> clientBot, TUser user, UpdateModel update)
        {
            Bot=clientBot??throw new ArgumentNullException(nameof(clientBot));
            User=user;
            Update=update??throw new ArgumentNullException(nameof(update));
        }
    }
}
