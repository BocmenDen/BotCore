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

IHost host = BotBuilder.CreateDefaultBuilder()
    .ConfigureAppConfiguration(app => app.AddInMemoryCollection([new(TgClient.KeySettingTOKEN, "ВАШ_ТОКЕН")]))
    .RegisterDBContextOptions(b => b.UseSqlite("Data Source=database.db"))
    .RegisterServices(
        Assembly.GetAssembly(typeof(Program)),
        Assembly.GetAssembly(typeof(TgClient))
    )
    .Build();

IServiceProvider service = host.Services;

var tgClient = service.GetRequiredService<TgClient<User, DataBase>>();
var spamFilter = host.Services.GetRequiredService<MessageSpam<IUser>>();
tgClient.Update += spamFilter.HandleCommand;

spamFilter.Init(async (context) =>
{
    await context.Reply(context.Update.Message ?? "Вы отправили пустое сообщение");
});
tgClient.Run().Wait();