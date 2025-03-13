using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;
using BotCore.PageRouter.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace BotCore.PageRouter
{
    public static class IHostExtensions
    {
        public static IHostBuilder RegisterPagesInRouter<TUser, TContext, TKey>(this IHostBuilder hostBuilder, params Assembly[] assemblies)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull => RegisterPagesInRouter<TUser, TContext, TKey>(hostBuilder, assemblies.AsEnumerable());

        public static IHostBuilder RegisterPagesInRouter<TUser, TContext, TKey>(this IHostBuilder hostBuilder, params Type[] pages)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull => RegisterPagesInRouter<TUser, TContext, TKey>(hostBuilder, pages.AsEnumerable());

        public static IHostBuilder RegisterPagesInRouter<TUser, TContext, TKey>(this IHostBuilder hostBuilder, IEnumerable<Assembly> assemblies)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull => RegisterPagesInRouter<TUser, TContext, TKey>(hostBuilder, assemblies.SelectMany(x => x.GetTypes()));

        public static IHostBuilder RegisterPagesInRouter<TUser, TContext, TKey>(this IHostBuilder hostBuilder, IEnumerable<Type> pages)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull
        {
            return RegisterPagesInRouter<TUser, TContext, TKey>(hostBuilder, pages.Select<Type, (TKey, string, Type)?>(x =>
            {
                var attr = x.GetCustomAttribute<PageAttribute<TKey>>();
                if (attr == null) return null;
                return (attr.Key, attr.CompilerName, x);
            }).Where(x => x != null).Cast<(TKey, string, Type)>());
        }

        public static IHostBuilder RegisterPagesInRouter<TUser, TContext, TKey>(this IHostBuilder hostBuilder, IEnumerable<(TKey keyPage, string compilerName, Type typePage)> pages)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                if (!context.Properties.TryGetValue(typeof(PageFactoryBuilder<TUser, TContext, TKey>), out var factory)
                    || factory is not PageFactoryBuilder<TUser, TContext, TKey> factoryCast)
                {
                    factoryCast = new PageFactoryBuilder<TUser, TContext, TKey>();
                    services.AddSingleton(typeof(IPageFactoryBuilder<TUser, TContext, TKey>), factoryCast);
                }

                foreach (var (keyPage, compilerName, typePage) in pages)
                {
                    services.AddSingleton(typePage);
                    factoryCast.AddPage(keyPage, compilerName, typePage);
                }
            });
        }
    }
}
