using BotCore.FilterRouter.Models;
using BotCore.Interfaces;

namespace BotCore.FilterRouter
{
    public interface IConditionalActionCollection<TUser> : IEnumerable<Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction>>
        where TUser : IUser
    {

    }
}
