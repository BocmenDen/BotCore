using BotCore.Attributes;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;

namespace BotCore.FilterRouter
{
    [Service(ServiceType.Singltone)]
    public class HandleFilterRouter<TUser, TContext>(IServiceProvider service, IConditionalActionCollection<TUser> conditionalActionCollection) : IInputLayer<TUser, TContext>, INextLayer<TUser, TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public event Func<TContext, Task>? Update;

        public async Task HandleNewUpdateContext(TContext context)
        {
            foreach (var conditionalAction in conditionalActionCollection)
            {
                EvaluatedAction evaluatedAction = conditionalAction(service, context);
                if (evaluatedAction.IsIgnore) continue;
                if (await evaluatedAction.Execute!()) return;
            }
            if (Update != null)
                await Update.Invoke(context);
        }
    }
}
