using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IPageFactoryBuilder<TUser, TContext, TKey>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
        where TKey : notnull
    {
        public IPageFactory<TUser, TContext, TKey> CreateFactory(IServiceProvider serviceProvider);
    }
}