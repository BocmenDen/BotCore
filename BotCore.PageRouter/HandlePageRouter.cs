using BotCore.Attributes;
using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;
using BotCore.Services;
using Microsoft.Extensions.Logging;

namespace BotCore.PageRouter
{
    [Service(ServiceType.Singleton)]
    public class HandlePageRouter<TUser, TContext, TKey> : IInputLayer<TUser, TContext>, INextLayer<TUser, TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
        where TKey : notnull
    {
        private readonly ILogger _logger;
        private readonly IPageFactory<TUser, TContext, TKey> _pageCollection;
        public event Func<TContext, Task>? Update;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConditionalPooledObjectProvider<IDBUserPageKey<TUser, TKey>> _dbKeys;

        public HandlePageRouter(
                ILogger<HandlePageRouter<TUser, TContext, TKey>> logger,
                IServiceProvider serviceProvider,
                ConditionalPooledObjectProvider<IDBUserPageKey<TUser, TKey>> dbKeys,
                IPageFactoryBuilder<TUser, TContext, TKey>? pageCollectionBuilder = null,
                IPageFactory<TUser, TContext, TKey>? pageCollection = null
            )
        {
            _dbKeys = dbKeys;
            _logger = logger;
            _pageCollection = pageCollection ?? pageCollectionBuilder?.CreateFactory(serviceProvider) ?? throw new Exception("Не удалось найти данные о страницах");
            _logger.LogInformation("PageRouter инициализирован");
            _serviceProvider=serviceProvider;
        }

        public Task HandleNewUpdateContext(TContext context)
        {
            var keyPage = _dbKeys.TakeObject((db) => db.GetKeyPage(context.User));
            if (keyPage == null) return Update?.Invoke(context) ?? Task.CompletedTask;
            var page = _pageCollection.GetPage(_serviceProvider, context.User, keyPage);
            if (page != null)
                return page.HandleNewUpdateContext(context);
            return Update?.Invoke(context) ?? Task.CompletedTask;
        }

        public async Task Navigate(TContext context, TKey keyPage)
        {
            var oldKeyPage = _dbKeys.TakeObject((db) => db.GetKeyPage(context.User));
            if (oldKeyPage != null)
            {
                if (EqualityComparer<TKey>.Default.Equals(oldKeyPage, keyPage)) return;
                if (_pageCollection.GetPage(_serviceProvider, context.User, oldKeyPage) is IPageOnExit<TUser, TContext> pageExit)
                {
                    await pageExit.OnExit(context);
                }
            }
            _logger.LogInformation("Пользователь {user}, перешёл на страницу {keyPage}", context.User, keyPage);
            await _dbKeys.TakeObject((db) => db.SetKeyPage(context.User, keyPage));
            var page = _pageCollection.GetPage(_serviceProvider, context.User, keyPage);
            if (page == null) return;
            await page.OnNavigate(context);
        }
    }
}
