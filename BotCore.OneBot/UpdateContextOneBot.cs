using BotCore.Interfaces;
using BotCore.Models;

namespace BotCore.OneBot
{
    internal class UpdateContextOneBot<TUser, TUserOld>(IUpdateContext<TUserOld> originalContext, TUser user) : IUpdateContext<TUser>
        where TUser : IUser
        where TUserOld : IUser
    {
        private readonly IUpdateContext<TUserOld> OriginalContext = originalContext??throw new ArgumentNullException(nameof(originalContext));

        public IClientBotFunctions BotFunctions => OriginalContext.BotFunctions;

        public TUser User { get; private set; } = user;

        public UpdateModel Update => OriginalContext.Update;

        public Task Reply(SendModel send) => OriginalContext.Reply(send);
    }
}
