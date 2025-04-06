using BotCore.Extensions;
using BotCore.Models;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotCore.Tg
{
    public static partial class TgClientExtensions
    {
        private static readonly Regex _parseCommand = GetParseCommandRegex();

        [GeneratedRegex(@"^/(\w+)(@\w+)?$", RegexOptions.Compiled)]
        internal static partial Regex GetParseCommandRegex();

        public static Telegram.Bot.Types.Enums.ParseMode TgGetParseMode(this SendModel sendingClient)
        {
            if (sendingClient.TryGetParameter(TgClient.KeyParseMode, out Telegram.Bot.Types.Enums.ParseMode parseMode))
                return parseMode;
            return Telegram.Bot.Types.Enums.ParseMode.None;
        }
        public static SendModel TgSetParseMode(this SendModel sendingClient, Telegram.Bot.Types.Enums.ParseMode parseMode)
        {
            sendingClient[TgClient.KeyParseMode] = parseMode;
            return sendingClient;
        }

        internal static (string? command, string? message) ParseCommand(this string? text)
        {
            if (text == null) return (null, null);
            var match = _parseCommand.Match(text);
            if (match.Success)
                return (match.Groups[1].Value, text.Remove(match.Groups[1].Index, match.Groups[1].Length).Trim());
            return (null, text);
        }
        internal static Chat? GetChatId(this Update update)
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
        internal static InlineKeyboardMarkup? CreateInline(this ButtonsSend? buttonsSend)
        {
            if (buttonsSend is null) return null;
            return new InlineKeyboardMarkup()
            {
                InlineKeyboard = buttonsSend.Buttons.Select(x => x.Select(b =>
                {
                    return new InlineKeyboardButton(b.Text)
                    {
                        CallbackData = b.Text.GetHashCode().ToString(),
                        Url = b.GetOrDefault<string>(nameof(InlineKeyboardButton.Url))
                    };
                }))
            };
        }
        internal static ReplyKeyboardMarkup? CreateReply(this ButtonsSend? buttonsSend)
        {
            if (buttonsSend is null) return null;
            return new ReplyKeyboardMarkup()
            {
                Keyboard = buttonsSend.Buttons.Select(x => x.Select(b =>
                {
                    return new KeyboardButton(b.Text)
                    {
                        Text = b.Text,
                        // TODO Parameters
                    };
                })),
                ResizeKeyboard = true
                // TODO Parameters
            };
        }
        internal static ReplyMarkup? GetReplyMarkup(this SendModel sendingClient) => (ReplyMarkup?)CreateInline(sendingClient.Inline) ?? CreateReply(sendingClient.Keyboard);

        internal static async ValueTask<FileTG> GetFile(this MediaSource media) => await FileTG.GetFileTg(media);
        internal static async ValueTask<FilesTG> GetFiles(this IEnumerable<MediaSource> medias) => await FilesTG.GetFilesTg(medias);

        internal static string GetFirstFileId(this Message message) =>
            message.Animation?.FileId ??
            message.Video?.FileId ??
            message.Photo?.FirstOrDefault()?.FileId ??
            message.Document?.FileId ??
            message.Audio?.FileId ??
            message.VideoNote?.FileId ??
            message.Sticker?.FileId ??
            throw new Exception("В сообщении нет информации о файле");
    }
}
