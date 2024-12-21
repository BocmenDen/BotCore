using OneBot.Extensions;
using OneBot.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OneBot.Tg
{
    public static class TgClientExtensions
    {

        public static Telegram.Bot.Types.Enums.ParseMode GetParseMode(this SendModel sendingClient)
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

        public static InlineKeyboardMarkup? CreateTgInline(this ButtonsSend? buttonsSend)
        {
            if (buttonsSend is null) return null;
            return new InlineKeyboardMarkup()
            {
                InlineKeyboard = buttonsSend.Buttons.Select(x => x.Select(b =>
                {
                    return new InlineKeyboardButton(b.Text)
                    {
                        CallbackData = b.Text,
                        Url = b.GetOrDefault<string>(nameof(InlineKeyboardButton.Url))
                    };
                }))
            };
        }

        public static ReplyKeyboardMarkup? CreateTgReply(this ButtonsSend? buttonsSend)
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

        internal static async Task<FileTG> GetFile(this MediaSource media)
        {
            if (media.TryGetParameter(TgClient.KeyMediaSourceFileId, out string? id)) return new(InputFile.FromFileId(id!));
            var stream = await media.GetStream();
            return new FileTG(InputFile.FromStream(stream, media.Name), () => stream.Dispose());
        }

        public class FileTG(InputFile file, Action? disponse = null) : IDisposable
        {
            public InputFile File { get; private set; } = file??throw new ArgumentNullException(nameof(file));
            private readonly Action? _disponse = disponse;

            public void Dispose()
            {
                _disponse?.Invoke();
                GC.SuppressFinalize(this);
            }

            public static implicit operator InputFile(FileTG fileTG) => fileTG.File;
        }
    }
}
