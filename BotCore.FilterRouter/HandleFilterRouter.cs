using BotCore.Attributes;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;

namespace BotCore.FilterRouter
{
    [Service(ServiceType.Singltone)]
    public class HandleFilterRouter<TUser>(IServiceProvider service, IConditionalActionCollection<TUser> conditionalActionCollection)
        where TUser : IUser
    {
        public event Action<IUpdateContext<TUser>>? UpdateContextUnrecognized;

        public async void HandleNewUpdateContext(IUpdateContext<TUser> context)
        {
            foreach (var conditionalAction in conditionalActionCollection)
            {
                EvaluatedAction evaluatedAction = conditionalAction(service, context);
                if (evaluatedAction.IsIgnore) continue;
                if (await evaluatedAction.Execute!()) return;
            }
            UpdateContextUnrecognized?.Invoke(context);
        }
    }
}
