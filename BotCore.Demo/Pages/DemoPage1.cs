using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;

namespace BotCore.Demo.Pages
{
    [Page<string>("DemoPage1")]
    public class DemoPage1<TUser, TContext> : IPage<TUser, TContext>, IPageLoading<TUser>, IPageLoaded<TUser>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public Task HandleNewUpdateContext(TContext context)
        {
            return context.Reply(context.Update.Message!);
        }

        public Task OnNavigate(TContext context)
        {
            return context.Reply("Вы перешли на страницу повторюшки");
        }

        public void PageLoaded(TUser user)
        {
        }

        public void PageLoading(TUser user)
        {
        }
    }
}
