using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IDBUserPageKey<in TUser, TKey>
        where TUser : IUser
        where TKey : notnull
    {
        public TKey? GetKeyPage(TUser user);
        public Task SetKeyPage(TUser user, TKey? key);
    }
}