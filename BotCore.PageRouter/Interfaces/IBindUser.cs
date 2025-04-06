using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IBindUser<T> where T : IUser
    {
        public void BindUser(T user);
    }
}
