using BotCore.Interfaces;
using BotCore.Models;
using BotCore.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BotCore.Tg
{
    public partial class TgClient<TUser, TDB> : BackgroundService, IClientBot<TUser, UpdateContext<TUser>>
        where TUser : IUser, IUserTgExtension
        where TDB : IDB
    {
        public readonly TelegramBotClient BotClient;

        private readonly ILogger? _logger;
        private readonly TgClientOptions _options;
        private readonly DBClientHelper<TUser, TDB, Chat, SingletonObjectProvider<TDB>> _database;

        public TgClient(
            IOptions<TgClientOptions> options,
            DBClientHelper<TUser, TDB, Chat, SingletonObjectProvider<TDB>> database,
            ILogger<TgClient<TUser, TDB>> logger)
        {
            _options = options.Value;
            _logger = logger;
            _database = database;
            BotClient = new TelegramBotClient(_options.Token);
        }

        private partial Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task task = BotClient.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, _options.ReceiverOptions, cancellationToken: stoppingToken);
            string botName = (await BotClient.GetMyName(cancellationToken: stoppingToken)).Name;
            _logger?.LogInformation("Бот {botName} запущен", botName);
            await task;
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger?.LogError(exception, "Внутренняя ошибка работы клиента Tg");
            return Task.CompletedTask;
        }

        public ButtonSearch? GetIndexButton(UpdateModel update, ButtonsSend buttonsSend)
        {
            if (update.OriginalMessage is not Update updateTg) return null;
            for (int i = 0; i < buttonsSend.Buttons.Count; i++)
            {
                for (int j = 0; j < buttonsSend.Buttons[i].Count; j++)
                {
                    if (buttonsSend.Buttons[i][j].Text == updateTg.Message?.Text || buttonsSend.Buttons[i][j].Text == updateTg.InlineQuery?.Query || buttonsSend.Buttons[i][j].Text.GetHashCode().ToString() == updateTg.CallbackQuery?.Data) // TODO оптимизировать за счёт кешировани buttonsSend.Buttons[i][j].Text.GetHashCode().ToString()
                    {
                        return new ButtonSearch(i, j, buttonsSend.Buttons[i][j]);
                    }
                }
            }
            return null;
        }
    }
}
