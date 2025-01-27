using BotCore.FilterRouter.Attributes;
using BotCore.Interfaces;
using BotCore.Models;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CS8321  // Локальная функция объявлена, но не используется
#pragma warning disable IDE0051 // Удалите неиспользуемые закрытые члены

namespace BotCore.Demo
{
    public static class DemoFiltersRouter
    {
        [ResourceKey("keyboard")]
        readonly static ButtonsSend keyboard = new([["Мой GitHub", "Ссылка на этот проект"]]);

        [CommandFilter<User>("start")]
        [MessageTypeFilter<User>(UpdateType.Command)]
        static async Task StartMessageHandler(IUpdateContext<User> context)
        {
            SendModel sendModel = "Привет!";
            sendModel.Keyboard = keyboard;
            await context.Reply(sendModel);
        }

        [ButtonsFilter<User>("keyboard")]
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
    }
}
