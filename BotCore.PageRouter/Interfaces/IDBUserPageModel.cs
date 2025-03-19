using BotCore.Interfaces;
using BotCore.PageRouter.Models;

namespace BotCore.PageRouter.Interfaces
{
    public interface IDBUserPageModel<in TUser>
        where TUser : IUser
    {
        public StorageModel<T> GetModel<T>(TUser user) where T: new();
    }
}
