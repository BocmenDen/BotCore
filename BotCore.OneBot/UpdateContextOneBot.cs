using BotCore.Interfaces;
using BotCore.Models;

namespace BotCore.OneBot
{
    public class UpdateContextOneBot<TUser>(IUpdateContext<IUser> originalContext, TUser user) : IUpdateContext<TUser>
        where TUser : IUser
    {
        public readonly IUpdateContext<IUser> OriginalContext = originalContext??throw new ArgumentNullException(nameof(originalContext));

        public IClientBotFunctions BotFunctions => OriginalContext.BotFunctions;

        public TUser User { get; private set; } = user;

        public UpdateModel Update => OriginalContext.Update;

        public Task Reply(SendModel send) => OriginalContext.Reply(send);
    }
}
