using BotCore.Extensions;
using BotCore.Models;

namespace BotCore.Tg
{
    public class SendingMessageInfo
    {
        public readonly static TimeSpan LimitTime = TimeSpan.FromHours(48);

        public bool IsDelete { get; private set; }
        public bool IsKeyboard { get; private set; }
        public bool IsInline { get; private set; }
        public bool IsLimitText { get; private set; }
        public bool IsMedia { get; private set; }
        public bool IsMediaOne { get; private set; }
        public bool IsMediaGroup { get; private set; }
        public bool IsTextMessage { get; private set; }

        public LastMessageInfo? LastMessage { get; private set; }
        public bool IsApplyEdit { get; private set; }
        public bool IsApplyDelete { get; private set; } = false;

        public SendingMessageInfo(SendModel sendModel)
        {
            IsDelete = sendModel.IsEmpty;
            IsKeyboard = sendModel.Keyboard is not null;
            IsInline = sendModel.Inline is not null;
            IsLimitText = (sendModel.Message?.Length ?? 0) > 4096;
            IsTextMessage = sendModel.Message is not null;
            IsMedia = sendModel.Medias is not null;
            IsMediaOne = sendModel.Medias?.Count == 1;
            IsMediaGroup = (sendModel.Medias?.Count ?? 0) > 1;
            LastMessage = sendModel.GetOrDefault<LastMessageInfo>(TgClient.KeyMessagesToEdit);
            IsApplyDelete = LastMessage is not null && (LastMessage.OldestMessage - DateTime.Now) < LimitTime;
            IsApplyEdit = IsApplyDelete && (!IsMediaGroup && LastMessage!.MessagesMediaGroup?.Length == sendModel.Medias?.Count) && !IsKeyboard;
        }
    }
}
