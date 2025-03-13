using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IPageLoading<in TUser>
        where TUser : IUser
    {
        public void PageLoading(TUser user);
    }
}