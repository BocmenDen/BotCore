﻿using BotCore.Base;
using BotCore.Interfaces;
using BotCore.Models;
using BotCore.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotCore.Tg
{
    public partial class TgClient<TUser, TDB> : ClientBot<TUser, UpdateContext<TUser>>
        where TUser : IUser, IUserTgExtension
        where TDB : IDB
    {
        private readonly ILogger<TgClient<TUser, TDB>>? _logger;
        public readonly TelegramBotClient BotClient;
        private readonly ReceiverOptions? _receiverOptions;
        private EventId _eventId;
        private readonly DBClientHelper<TUser, TDB, Chat, SingletonObjectProvider<TDB>> _database;

        public TgClient(IOptions<TgClientOptions> option,
                        DBClientHelper<TUser, TDB, Chat, SingletonObjectProvider<TDB>> database,
                        ILogger<TgClient<TUser, TDB>>? logger = null,
                        ReceiverOptions? receiverOptions = null)
        {
            string token = option.Value.Token ?? throw new Exception("Отсутствует токен подключения к Tg боту");
            Id = token.GetHashCode();
            BotClient = new TelegramBotClient(token);
            _receiverOptions = receiverOptions;
            _logger = logger;
            _eventId = new EventId(Id);
            _database=database;
        }

        protected override async Task ExecuteAsync(CancellationToken token = default)
        {
            Task task = BotClient.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, _receiverOptions, cancellationToken: token);
            string botName = (await BotClient.GetMyName(cancellationToken: token)).Name;
            _eventId = new EventId(Id, botName);
            _logger?.LogInformation(_eventId, "Бот {botName} запущен", botName);
            await task;
        }

        private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            return HandleUpdate(async () =>
            {
                var user = await LoadUser(update);
                if (user == null) return default;
                var media = GetMedias(update, out var flagType);
                var command = TgClient.ParseCommand(update?.Message?.Text ?? update?.Message?.Caption);

                return new UpdateContext<TUser>(this, user!,
                new UpdateModel()
                {
                    UpdateType = TgClient.GetReceptionType(update!) | flagType | (command == null ? UpdateType.None : UpdateType.Command),
                    OriginalMessage = update,
                    Message = update?.Message?.Text ?? update?.Message?.Caption,
                    Medias = media,
                    Command = command
                });
            });
        }

        public override async Task Send(TUser user, SendModel send, UpdateModel? updateModel = null)
        {
            if (send.Message == null)
            {
                _logger?.LogWarning(_eventId, "Поддерживается пока только отправка текстовых сообщений");
                return;
            }

            async Task sendMessageSaveId(Func<Task<Message>> action)
            {
                var message = await action();
                send[TgClient.MessagesToEdit] = message.Id;
                if (updateModel != null) updateModel[TgClient.MessagesToEdit] = message.Id;
            }

            var chat = user.GetTgChat();

            if (send.Medias != null)
            {
                foreach (var doc in send.Medias)
                {
                    using var file = await doc.GetFile();
                    Message? message;
                    if (doc.Name?.Contains(".mp4") ?? false)
                    {
                        message = await BotClient.SendVideo(chat, file, caption: send.Message!, replyMarkup: TgClient.GetReplyMarkup(send), parseMode: send.GetParseMode());
                        doc[TgClient.KeyMediaSourceFileId] = message.Video!.FileId;
                    }
                    else
                    {
                        message = await BotClient.SendDocument(chat, file, caption: send.Message!, replyMarkup: TgClient.GetReplyMarkup(send), parseMode: send.GetParseMode());
                        doc[TgClient.KeyMediaSourceFileId] = message.Document!.FileId;
                    }
                }
                return;
            }

            if (send.ContainsKey(TgClient.MessagesToEdit) && send.Inline == null && send.Keyboard == null)
                await EditMessage(user, (int)send[TgClient.MessagesToEdit], send, updateModel, sendMessageSaveId);
            else
                await sendMessageSaveId(() => BotClient.SendMessage(chat, send.Message!, replyMarkup: TgClient.GetReplyMarkup(send), parseMode: send.GetParseMode()));
        }

        private Task EditMessage(TUser user, int oldMessage, SendModel sendingClient, UpdateModel? _, Func<Func<Task<Message>>, Task> sendMessageSaveId)
        {
            return sendMessageSaveId(() => BotClient.EditMessageText(user.GetTgChat(), oldMessage, sendingClient.Message!, replyMarkup: sendingClient.Inline.CreateTgInline(), parseMode: sendingClient.GetParseMode()));
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger?.LogError(_eventId, exception, "Внутренняя ошибка работы клиента Tg");
            return Task.CompletedTask;
        }

        private async Task<TUser?> LoadUser(Update update)
        {
            var chatId = TgClient.GetChatId(update);
            if (chatId == null) return default;
            var (user, isCreate) = await _database.GetOrCreate(chatId);
            if (isCreate)
                _logger?.LogInformation(_eventId, "Добавлен новый пользователь [{userTg}]", user);
            return user;
        }

        public override ButtonSearch? GetIndexButton(UpdateModel update, ButtonsSend buttonsSend)
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

        private List<MediaSource>? GetMedias(Update update, out UpdateType receptionType)
        {
            List<MediaSource> mediaSources = [];
            receptionType = UpdateType.None;
            if (update.Message?.Document != null)
                AddMedia(mediaSources, update.Message.Document, update.Message.Document.FileName, update.Message.Document.MimeType);
            else if (update.Message?.Animation != null)
                AddMedia(mediaSources, update.Message.Animation, update.Message.Animation.FileName, update.Message.Animation.MimeType);
            else if (update.Message?.Video != null)
                AddMedia(mediaSources, update.Message.Video, update.Message.Video.FileName, update.Message.Video.MimeType);
            if (mediaSources.Count!=0)
            {
                receptionType = UpdateType.Media;
                return mediaSources;
            }
            return null;
        }

        private void AddMedia(List<MediaSource> medias, FileBase? fileBase, string? fileName, string? mimeType)
        {
            if (fileBase == null) return;
            medias.Add(new MediaSource(async () =>
            {
                string path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + fileName);
                var streamWriter = System.IO.File.Open(path, FileMode.OpenOrCreate);
                var file = await BotClient.GetFile(fileBase.FileId!);
                await BotClient.DownloadFile(file.FilePath!, streamWriter);
                streamWriter.Position = 0;
                return streamWriter;
            }, new() { { TgClient.KeyMediaSourceFileId, fileBase.FileId } })
            {
                Name = fileName,
                Type = Path.GetExtension(fileName),
                MimeType = mimeType,
                Id = fileBase.FileId
            });
        }

        public override void Dispose()
        {
            _database.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public static partial class TgClient
    {
        public const string MessagesToEdit = "tg_messagesToEdit";
        public const string KeyParseMode = "tg_parseMode";
        public const string KeyMediaSourceFileId = "tg_fileId";

        private static readonly Regex _parseCommand = GetParseCommandRegex();

        [GeneratedRegex(@"^/(\w+)(@\w+)?$", RegexOptions.Compiled)]
        internal static partial Regex GetParseCommandRegex();

        internal static string? ParseCommand(string? text)
        {
            if (text == null) return null;
            var match = _parseCommand.Match(text);
            if (match.Success)
                return match.Groups[1].Value;
            return null;
        }
        internal static UpdateType GetReceptionType(Update update)
        {
            UpdateType receptionType = update.Type switch
            {
                Telegram.Bot.Types.Enums.UpdateType.Message => UpdateType.Message,
                Telegram.Bot.Types.Enums.UpdateType.InlineQuery => UpdateType.Inline,
                Telegram.Bot.Types.Enums.UpdateType.CallbackQuery => UpdateType.Keyboard,
                _ => UpdateType.None
            };
            return receptionType;
        }
        internal static Chat? GetChatId(Update update)
        {
            if (update.Message != null) return update.Message.Chat;
            if (update.EditedMessage != null) return update.EditedMessage.Chat;
            if (update.ChannelPost != null) return update.ChannelPost.Chat;
            if (update.EditedChannelPost != null) return update.EditedChannelPost.Chat;
            if (update.CallbackQuery != null && update.CallbackQuery.Message != null) return update.CallbackQuery.Message.Chat;
            //if (update.InlineQuery != null && update.InlineQuery.From != null) return update.InlineQuery.From; // Для inline запросов это ID пользователя, не чата!
            //if (update.ChosenInlineResult != null && update.ChosenInlineResult.From != null) return update.ChosenInlineResult.From; // Для chosen inline results это ID пользователя, не чата!
            //if (update.ShippingQuery != null && update.ShippingQuery.From != null) return update.ShippingQuery.From; // ID пользователя
            //if (update.PreCheckoutQuery != null && update.PreCheckoutQuery.From != null) return update.PreCheckoutQuery.From.;
            return null; // ChatId не найден
        }
        internal static IReplyMarkup? GetReplyMarkup(SendModel sendingClient)
            => (IReplyMarkup?)sendingClient.Inline.CreateTgInline() ?? sendingClient.Keyboard.CreateTgReply();
    }
}