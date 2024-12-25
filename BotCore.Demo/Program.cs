using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BotCore;
using BotCore.EfUserDb;
using BotCore.Interfaces;
using BotCore.Tg;
using BotCore.Demo;
using System.Reflection;
using BotCore.OneBot;

IHost host = BotBuilder.CreateDefaultBuilder()
    .ConfigureAppConfiguration(app => app.AddUserSecrets(Assembly.GetExecutingAssembly()))
    .RegisterDBContextOptions(b => b.UseSqlite($"Data Source={Path.GetRandomFileName()}.db"))
    .RegisterServices(
        Assembly.GetAssembly(typeof(Program)),
        Assembly.GetAssembly(typeof(TgClient)),
        Assembly.GetAssembly(typeof(CombineBots<,>))
    )
    .Build();

IServiceProvider service = host.Services;

var tgClient = service.GetRequiredService<TgClient<UserTg, DataBase>>();
var combineUser = service.GetRequiredService<CombineBots<User, DataBase>>();
var spamFilter = host.Services.GetRequiredService<MessageSpam<IUser>>();
tgClient.Update += combineUser.HandleNewUpdateContext;
combineUser.NewUpdateContext += spamFilter.HandleCommand;

spamFilter.Init(async (context) =>
{
    await context.Reply(context.Update.Message ?? "Вы отправили пустое сообщение");
});
tgClient.Run().Wait();