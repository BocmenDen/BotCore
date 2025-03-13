using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IPageLoaded<in TUser>
        where TUser : IUser
    {
        public void PageLoaded(TUser user);
    }
}