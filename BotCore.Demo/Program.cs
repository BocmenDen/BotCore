using BotCore.EfUserDb;
using BotCore.FilterRouter;
using BotCore.FilterRouter.Extensions;
using BotCore.Interfaces;
using BotCore.Models;
using BotCore.OneBot;
using BotCore.PageRouter;
using BotCore.Tg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace BotCore.Demo
{
    class Program
    {
        /// <summary>
        /// Создание и конфигурация хоста
        /// </summary>
        static IHostBuilder ConfigureServices() => BotBuilder.CreateDefaultBuilder()
                .ConfigureAppConfiguration(app => app.AddUserSecrets(Assembly.GetExecutingAssembly()))
                .RegisterServices(
                    Assembly.GetAssembly(typeof(Program)),
                    Assembly.GetAssembly(typeof(TgClient)),
                    Assembly.GetAssembly(typeof(CombineBots<,>)),
                    Assembly.GetAssembly(typeof(HandleFilterRouter<,>)),
                    Assembly.GetAssembly(typeof(HandlePageRouter<,,>))
                )
                .ConfigureServices((b, s) =>
                {
                    // Говорим откуда читать параметры
                    s.Configure<TgClientOptions>(b.Configuration.GetSection("TgClientOptions"));
                    s.Configure<DataBaseOptions>(b.Configuration.GetSection("DataBase"));
                    s.Configure<PooledObjectProviderOptions<DataBase>>(b.Configuration.GetSection("DataBase"));
                })
                .RegisterFiltersRouterAuto<User, UpdateContextOneBot<User>>() // Регистрация фильтров (См. BotCore.Demo.DemoFiltersRouter)
                .RegisterDBContextOptions((s, _, b) => b.UseSqlite($"Data Source={s.GetRequiredService<IOptions<DataBaseOptions>>().Value.GetPathOrDefault()}"));

        static void Main()
        {
            IHost host = ConfigureServices()
                        .RegisterClient<TgClient<UserTg, DataBase>>()
                        .RegisterPagesInRouter<User, UpdateContextOneBot<User>, string>(
                            Assembly.GetAssembly(typeof(Program))!
                        )
                        .ConfigureServices((s) => s.AddMemoryCache())
                        .Build();

            var combineUser = host.Services.GetRequiredService<CombineBots<DataBase, User>>();
            var spamFilter = host.Services.GetRequiredService<MessageSpam<User, UpdateContextOneBot<User>>>();
            var filterRouting = host.Services.GetRequiredService<HandleFilterRouter<User, UpdateContextOneBot<User>>>();
            var pageRouting = host.Services.GetRequiredService<HandlePageRouter<User, UpdateContextOneBot<User>, string>>();

            foreach (var client in host.Services.GetServices<IClientBot<IUser, IUpdateContext<IUser>>>())
                client.Update += combineUser.HandleNewUpdateContext;

            combineUser.Update += spamFilter.HandleNewUpdateContext;
            spamFilter.Update += filterRouting.HandleNewUpdateContext;
            filterRouting.Update += pageRouting.HandleNewUpdateContext;
            pageRouting.Update += (context) => context.Reply("Извините я Вас не понял");

            host.Run();
        }
    }
}