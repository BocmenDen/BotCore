using BotCore.EfUserDb;
using BotCore.FilterRouter;
using BotCore.FilterRouter.Attributes;
using BotCore.FilterRouter.Extensions;
using BotCore.Interfaces;
using BotCore.Models;
using BotCore.OneBot;
using BotCore.Tg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CS8321  // Локальная функция объявлена, но не используется
#pragma warning disable IDE0051 // Удалите неиспользуемые закрытые члены

namespace BotCore.Demo
{
    class Program
    {
        [ResourceKey("keyboard")]
        readonly static ButtonsSend keyboard = new([["Мой GitHub", "Ссылка на этот проект"]]);

        [CommandFilter<User>("start")]
        static async Task StartMessageHandler(IUpdateContext<User> context)
        {
            SendModel sendModel = "Привет!";
            sendModel.Keyboard = keyboard;
            await context.Reply(sendModel);
        }

        [ButtonsFilter<User>("keyboard", 0, 0)]
        [FilterPriority(0)]
        static bool ListenerKeyboard(ILogger<Program> logger, IUpdateContext<User> context, ButtonSearch? buttonSearch)
        {
            logger.LogInformation("Пользователь {user}, нажал на кнопку {buttonText}", context.User, buttonSearch?.Button.Text);
            return false;
        }

        [ButtonsFilter<User>("keyboard", 0, 0)]
        [FilterPriority(1)]
        static async Task KeyboardHendlerMyGit(IUpdateContext<User> context)
        {
            await context.Reply("https://github.com/BocmenDen?tab=repositories");
        }

        [ButtonsFilter<User>("keyboard", 0, 1)]
        [FilterPriority(1)]
        static async Task KeyboardHendlerBotCoreProject(IUpdateContext<User> context)
        {
            await context.Reply("https://github.com/BocmenDen/BotCore");
        }

        static void Main()
        {
            IHost host = BotBuilder.CreateDefaultBuilder()
                .ConfigureAppConfiguration(app => app.AddUserSecrets(Assembly.GetExecutingAssembly()))
                .RegisterDBContextOptions(b => b.UseSqlite($"Data Source={Path.GetRandomFileName()}.db"))
                .RegisterServices(
                    Assembly.GetAssembly(typeof(Program)),
                    Assembly.GetAssembly(typeof(TgClient)),
                    Assembly.GetAssembly(typeof(CombineBots<,>)),
                    Assembly.GetAssembly(typeof(HandleFilterRouter<>))
                )
                .RegisterFiltersRouterAuto<User>()
                .Build();

            IServiceProvider service = host.Services;

            var tgClient = service.GetRequiredService<TgClient<UserTg, DataBase>>();
            var combineUser = service.GetRequiredService<CombineBots<User, DataBase>>();
            var spamFilter = host.Services.GetRequiredService<MessageSpam<User>>();
            var filterRouting = host.Services.GetRequiredService<HandleFilterRouter<User>>();
            tgClient.Update += combineUser.HandleNewUpdateContext;
            combineUser.NewUpdateContext += spamFilter.HandleCommand;
            spamFilter.Init(filterRouting.HandleNewUpdateContext);

            tgClient.Run().Wait();
        }
    }
}