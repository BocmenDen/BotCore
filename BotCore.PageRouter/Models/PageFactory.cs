using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotCore.PageRouter.Models
{
    internal class PageFactoryBuilder<TUser, TContext, TKey> : IPageFactoryBuilder<TUser, TContext, TKey>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
        where TKey : notnull
    {
        private readonly List<(TKey key, string compiler, Type typePage)> _pages = [];
        public void AddPage(TKey keyPage, string compiler, Type typePage) => _pages.Add((keyPage, compiler, typePage));

        public IPageFactory<TUser, TContext, TKey> CreateFactory(IServiceProvider serviceProvider)
        {
            ILogger logger = serviceProvider.GetRequiredService<ILogger<PageFactoryBuilder<TUser, TContext, TKey>>>();
            logger.LogInformation("Начало компиляции страниц");
            Dictionary<TKey, Func<IServiceProvider, TUser, IPage<TUser, TContext>>> pages = [];
            foreach (var (key, compiler, type) in _pages)
            {
                try
                {
                    pages.Add(key, PageFactoryCompiler.Build<TUser, TContext>(type, compiler, logger));
                    logger.LogDebug("Страница {page} успешно скомпилирована компилятором {compiler}", type.FullName, compiler);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка компиляции страницы {page} с ключом {key}", type.FullName, key);
                }
            }
            logger.LogInformation("Конец компиляции страниц");
            return new PageFactoryDefault<TUser, TContext, TKey>(pages);
        }
    }
}