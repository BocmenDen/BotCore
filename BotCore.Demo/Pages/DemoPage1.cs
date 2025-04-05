using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;
using Microsoft.Extensions.Logging;

namespace BotCore.Demo.Pages
{
    [PageCacheable<string>("DemoPage1")]
    public class DemoPage1<TUser, TContext>(ILogger<DemoPage1<TUser, TContext>> logger) : IPage<TUser, TContext>, IPageLoading<TUser>, IPageLoaded<TUser>, IBindNavigateFunction<TUser, TContext, string>, IPageOnExit<TUser, TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        private readonly ILogger _logger = logger;

        public void BindNavigateFunction(Func<TContext, string, Task> navigateFunction)
        {
        }

        public Task HandleNewUpdateContext(TContext context) => context.Reply(context.Update.Message!);

        public Task OnExit(TContext context)
        {
            return context.Reply("Вы покинули страницу повторюшки");
        }

        public async Task OnNavigate(TContext context)
        {
            await context.Reply("Вы перешли на страницу повторюшки");
        }

        public void PageLoaded(TUser user)
        {
            _logger.LogInformation("Страница успешно загружена");
        }

        public void PageLoading(TUser user)
        {
            _logger.LogInformation("Начало создания страницы");
        }
    }
}
