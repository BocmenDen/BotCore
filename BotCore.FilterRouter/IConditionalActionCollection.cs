using BotCore.FilterRouter.Models;
using BotCore.Interfaces;

namespace BotCore.FilterRouter
{
    public interface IConditionalActionCollection<TUser, TContext> : IEnumerable<Func<IServiceProvider, TContext, EvaluatedAction>>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {

    }
}
