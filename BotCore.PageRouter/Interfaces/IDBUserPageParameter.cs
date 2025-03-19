using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IDBUserPageParameter<TUser>
        where TUser : IUser
    {
        public object? GetParameter(TUser user);
        public Task SetParameter(TUser user, object? parameter);
    }
}
