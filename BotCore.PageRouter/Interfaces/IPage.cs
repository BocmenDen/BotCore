using BotCore.Interfaces;

namespace BotCore.PageRouter.Interfaces
{
    public interface IPage<in TUser, in TContext> : IInputLayer<TUser, TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public Task OnNavigate(TContext context);
    }
}