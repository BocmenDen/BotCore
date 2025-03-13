using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IPageFactory<in TUser, in TContext, in TKey>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
        where TKey : notnull
    {
        public IPage<TUser, TContext>? GetPage(IServiceProvider serviceProvider, TUser user, TKey keyPage);
    }
}