using BotCore.Models;

namespace BotCore.Tg
{
    public static class TgClient
    {
        public const string KeyMessagesToEdit = "tg_messagesToEdit";
        public const string KeyParseMode = "tg_parseMode";
        public const string KeyMediaSourceFileId = "tg_fileId";
        internal const string KeyInlineKeyboardMarkupCache = "tg_inlineKeyboardMarkupCache";
        internal const string KeyInlineKeyboardMarkupSearchCache = "tg_inlineKeyboardMarkupSearchCache";
        internal const string KeyReplyKeyboardMarkupCache = "tg_replyKeyboardMarkupCache";
        internal const string KeyReplyKeyboardMarkupSearchCache = "tg_replyKeyboardMarkupSearchCache";

        internal const int MaxLengthTextButton = 64;

        internal static string GetButtonText(ButtonSend button) => button.Text.Length <= MaxLengthTextButton ? button.Text : button.Text[..MaxLengthTextButton];
        internal static string GetHashButtonIline(string textBtn) => textBtn.GetHashCode().ToString();
        internal static string GetHashButtonKeyboard(string textBtn) => textBtn;
    }
}