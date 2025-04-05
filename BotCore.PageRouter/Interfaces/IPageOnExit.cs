using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IPageOnExit<in TUser, in TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public Task OnExit(TContext context);
    }
}
