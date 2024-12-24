using BotCore.Interfaces;

namespace BotCore.SpamBroker
{
    public interface ISpam<TUser> where TUser : IUser
    {
        public StateSpam GetSpamState(IUpdateContext<TUser> context);
    }
}
