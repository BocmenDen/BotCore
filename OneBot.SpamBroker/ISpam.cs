using OneBot.Interfaces;

namespace OneBot.SpamBroker
{
    public interface ISpam<TUser> where TUser : IUser
    {
        public StateSpam GetSpamState(IUpdateContext<TUser> context);
    }
}
