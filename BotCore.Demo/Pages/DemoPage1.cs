using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;
using BotCore.PageRouter.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BotCore.Demo.Pages
{
    [PageCacheable<string>("DemoPage1")]
    public class DemoPage1<TUser, TContext>(ILogger<DemoPage1<TUser, TContext>> logger, IMemoryCache? memoryCache = null) : IPage<TUser, TContext>, IPageLoading<TUser>, IPageLoaded<TUser>, IBindNavigateFunction<TUser, TContext, string>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        private readonly ILogger _logger = logger;

        public void BindNavigateFunction(Func<TContext, string, Task> navigateFunction)
        {
        }

        public void BindNavigateParameter(object? parameter)
        {
        }

        public void BindStorageModel(StorageModel<object> model)
        {
        }

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
            _logger.LogInformation("Страница успешно загружена");
        }

        public void PageLoading(TUser user)
        {
            _logger.LogInformation("Начало создания страницы");
        }
    }
}
