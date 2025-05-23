﻿using BotCore.FilterRouter.Attributes;
using BotCore.Interfaces;
using BotCore.Models;
using BotCore.OneBot;
using BotCore.PageRouter;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CS8321  // Локальная функция объявлена, но не используется
#pragma warning disable IDE0051 // Удалите неиспользуемые закрытые члены

namespace BotCore.Demo
{
    public static class DemoFiltersRouter
    {
        [ResourceKey("keyboard")]
        readonly static ButtonsSend keyboard = new([["Мой GitHub", "Ссылка на этот проект"], ["DemoPage 1", "DemoPage 2"]]);

        [CommandFilter<User>(true, "start")]
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
            logger.LogInformation("Пользователь {user}, нажал на кнопку \"{buttonText}\"", context.User, buttonSearch?.Button.Text);
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

        [RegexFilter<User>(@"\b(?:https?://|www\.)\S+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, ReturnType.Match)]
        [FilterPriority(1)]
        static async Task LinkHendlerBotCoreProject(IUpdateContext<User> context, Match link)
        {
            try
            {
                using HttpClient httpClient = new();
                await context.Reply((await httpClient.GetStringAsync(link.Value))[..4096]); // TODO подобные ограничения нужно решать в самих реализациях клиентов или в виде вспомогательных прослоек
            }
            catch (Exception ex)
            {
                await context.Reply(ex.Message);
            }
        }

        [ButtonsFilter<User>("keyboard", 1)]
        [FilterPriority(1)]
        static async Task OpenPage(UpdateContextOneBot<User> context, ButtonSearch? buttonSearch, HandlePageRouter<User, UpdateContextOneBot<User>, string> routing)
        {
            if (buttonSearch!.Value.Column == 0)
                await routing.Navigate(context, "DemoPage1");
            if (buttonSearch!.Value.Column == 1)
                await routing.Navigate(context, "DemoPage2");
        }
    }
}