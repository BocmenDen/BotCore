using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IBindNavigateFunction<TUser, TContext, TKey>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
        where TKey : notnull
    {
        public void BindNavigateFunction(Func<TContext, TKey, Task> navigateFunction);
    }
}
