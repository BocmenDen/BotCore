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
            return null;
        }
        internal static InlineKeyboardMarkup? CreateInline(this ButtonsSend? buttonsSend) =>
            HelpGenerateButtons(buttonsSend, TgClient.KeyInlineKeyboardMarkupCache, TgClient.KeyInlineKeyboardMarkupSearchCache, (btn) =>
            {
                var textBtn = GetButtonText(btn);
                var hash = btn.Text.GetHashCode().ToString();
                return (hash, new InlineKeyboardButton(textBtn)
                {
                    CallbackData = hash,
                    Url = btn.GetOrDefault<string>(nameof(InlineKeyboardButton.Url))
                });
            }, (buttons) =>
            {
                return new InlineKeyboardMarkup()
                {
                    InlineKeyboard = buttons
                };
            });
        internal static ReplyKeyboardMarkup? CreateReply(this ButtonsSend? buttonsSend) =>
            HelpGenerateButtons(buttonsSend, TgClient.KeyReplyKeyboardMarkupCache, TgClient.KeyReplyKeyboardMarkupSearchCache, (btn) =>
            {
                var textBtn = GetButtonText(btn);
                return (textBtn, new KeyboardButton(textBtn));
            }, (buttons) =>
            {
                return new ReplyKeyboardMarkup()
                {
                    ResizeKeyboard = true,
                    Keyboard = buttons
                };
            });

        private static TOut? HelpGenerateButtons<TOut, TItem>(this ButtonsSend? buttonsSend, string cacheResultKey, string cacheSearchKey, Func<ButtonSend, (string hash, TItem)> generateBtn, Func<List<List<TItem>>, TOut> createResult) where TOut : class
        {
            if (buttonsSend is null) return null;
            if (buttonsSend.TryGetParameter(cacheResultKey, out TOut? result)) return result;
            List<List<TItem>> buttonsGenerate = [];
            int row = 0;
            Dictionary<string, ButtonSearch> cacheSearchButtons = [];
            foreach (var lineBtns in buttonsSend.Buttons)
            {
                List<TItem> lineGenerate = [];
                int column = 0;
                foreach (var btn in lineBtns)
                {
                    var (hash, btnGenerate) = generateBtn(btn);
                    lineGenerate.Add(btnGenerate);
                    cacheSearchButtons.Add(hash, new ButtonSearch(row, column, btn));
                    column++;
                }
                buttonsGenerate.Add(lineGenerate);
                row++;
            }

            result = createResult(buttonsGenerate);
            buttonsSend[cacheSearchKey] = cacheSearchButtons;
            buttonsSend[cacheResultKey] = result;
            return result;
        }

        private static string GetButtonText(ButtonSend button)
            => button.Text.Length <= TgClient.MaxLengthTextButton ? button.Text : button.Text[..TgClient.MaxLengthTextButton];

        internal static ReplyMarkup? GetReplyMarkup(this SendModel sendingClient) => (ReplyMarkup?)CreateInline(sendingClient.Inline) ?? CreateReply(sendingClient.Keyboard);

        internal static async ValueTask<FileTG> GetFile(this MediaSource media) => await FileTG.GetFileTg(media);
        internal static async ValueTask<FilesTG> GetFiles(this IEnumerable<MediaSource> medias) => await FilesTG.GetFilesTg(medias);

        internal static string GetFirstFileId(this Message message) =>
            message.Animation?.FileId ??
            message.Video?.FileId ??
            message.Photo?.LastOrDefault()?.FileId ??
            message.Document?.FileId ??
            message.Audio?.FileId ??
            message.VideoNote?.FileId ??
            message.Sticker?.FileId ??
            throw new Exception("В сообщении нет информации о файле");
    }
}
