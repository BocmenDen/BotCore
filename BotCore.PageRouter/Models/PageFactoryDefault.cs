using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;
using static BotCore.PageRouter.PageFactoryCompiler;

namespace BotCore.PageRouter.Models
{
    internal class PageFactoryDefault<TUser, TContext, TKey>(Dictionary<TKey, CreatePage<TUser, TContext>> pages) : IPageFactory<TUser, TContext, TKey>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, CreatePage<TUser, TContext>> _pages = pages;

        public IPage<TUser, TContext>? GetPage(IServiceProvider serviceProvider, TUser user, TKey keyPage)
        {
            return _pages.TryGetValue(keyPage, out var page) ? page(serviceProvider, user) : null;
        }
    }
}