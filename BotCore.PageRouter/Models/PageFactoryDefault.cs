using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;

namespace BotCore.PageRouter.Models
{
    internal class PageFactoryDefault<TUser, TContext, TKey>
        (Dictionary<TKey, Func<IServiceProvider, TUser, IPage<TUser, TContext>>> pages) : IPageFactory<TUser, TContext, TKey>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, Func<IServiceProvider, TUser, IPage<TUser, TContext>>> _pages = pages;

        public IPage<TUser, TContext>? GetPage(IServiceProvider serviceProvider, TUser user, TKey keyPage)
        {
            return _pages.TryGetValue(keyPage, out var page) ? page(serviceProvider, user) : null;
        }
    }
}