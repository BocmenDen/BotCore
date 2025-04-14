using BotCore.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BotCore.Tg
{
    public partial class TgClient<TUser, TDB>
    {
        private partial Task SendMessage(SendModel sendModel, Chat chatId)
        {
            var sendingMessageInfo = new SendingMessageInfo(sendModel);
            if (sendingMessageInfo.IsDelete) return DeleteOldMessage(chatId, sendingMessageInfo);
            if (sendingMessageInfo.IsLimitText) throw new Exception("В данный момент поддержка превышения лимита текста не поддерживается");
            if (sendingMessageInfo.IsKeyboard && sendingMessageInfo.IsInline) throw new Exception("В данный момент отправка клавиатуры и кнопок в сообщении одновременно не поддерживаются");
            if (sendingMessageInfo.IsMedia && sendingMessageInfo.IsKeyboard) throw new Exception("В данный момент отправка медиафайлов и клавиатуры одновременно не поддерживаются");
            if (!sendingMessageInfo.IsApplyEdit)
                return SendNewMessage(sendModel, chatId, sendingMessageInfo);
            return EditOldMessage(sendModel, chatId, sendingMessageInfo);
        }

        private Task DeleteOldMessage(Chat chatId, SendingMessageInfo sendingMessageInfo)
        {
            if (sendingMessageInfo.IsApplyDelete)
                return BotClient.DeleteMessages(chatId, sendingMessageInfo.LastMessage!.AllMessages);
            return Task.CompletedTask;
        }

        private async Task SendNewMessage(SendModel send, Chat chatId, SendingMessageInfo sendingMessageInfo)
        {
            await DeleteOldMessage(chatId, sendingMessageInfo);
            if (sendingMessageInfo.IsMedia)
                await SendNewMessageIfMedia(send, chatId, sendingMessageInfo);
            else
                await SendNewMessageIfNoMedia(send, chatId, sendingMessageInfo);
        }

        private async Task SendNewMessageIfNoMedia(SendModel send, Chat chatId, SendingMessageInfo sendingMessageInfo)
        {
            var message = await BotClient.SendMessage(chatId, send.Message!, replyMarkup: send.GetReplyMarkup(), parseMode: send.TgGetParseMode());
            send[TgClient.KeyMessagesToEdit] = new LastMessageInfo()
            {
                Messages = [message.Id],
                IsInline = sendingMessageInfo.IsInline
            };
        }

        private async Task SendNewMessageIfMedia(SendModel send, Chat chatId, SendingMessageInfo limitsInfo)
        {
            LastMessageInfo currentMessage;
            if (limitsInfo.IsMediaOne)
            {
                var media = send.Medias![0];
                var (message, idFile) = await SendNewOneMedia(send, chatId, media);
                media[TgClient.KeyMediaSourceFileId] = idFile;
                currentMessage = new LastMessageInfo()
                {
                    MessagesMediaGroup = [message.Id],
                    IsInline = limitsInfo.IsInline
                };
            }
            else
            {
                using var files = await send.Medias!.GetFiles();
                IEnumerable<IAlbumInputMedia> albomFiles = files;
                bool isInsertMessageToFile = false;
                if (!limitsInfo.IsInline && limitsInfo.IsTextMessage)
                {
                    var listFiles = albomFiles.ToList();
                    if (listFiles.Last() is InputMedia inputMedia)
                    {
                        inputMedia.Caption = send.Message;
                        inputMedia.ParseMode = send.TgGetParseMode();
                        isInsertMessageToFile = true;
                    }
                }
                var messages = await BotClient.SendMediaGroup(chatId, files);
                foreach (var (media, messageMedia) in send.Medias!.Zip(messages))
                {
                    var fileId = messageMedia.GetFirstFileId();
                    media[TgClient.KeyMediaSourceFileId] = fileId;
                }
                int? messageAdditional = null;
                if (!isInsertMessageToFile)
                    messageAdditional = (await BotClient.SendMessage(chatId, send.Message!, replyMarkup: send.GetReplyMarkup(), parseMode: send.TgGetParseMode())).Id;

                currentMessage = new LastMessageInfo()
                {
                    MessagesMediaGroup = [.. messages.Select(x => x.Id)],
                    IsInline = limitsInfo.IsInline,
                    Messages = messageAdditional == null ? null : [messageAdditional.Value]
                };
            }
            send[TgClient.KeyMessagesToEdit] = currentMessage;
            return;
        }

        private async Task<(Message message, string idFile)> SendNewOneMedia(SendModel send, Chat chatId, MediaSource media)
        {
            using var file = await media.GetFile();
            Message message;
            string idFile;
            switch (media.Type)
            {
                case "mp4":
                    message = await BotClient.SendVideo(chatId, file, caption: send.Message!, replyMarkup: send.GetReplyMarkup(), parseMode: send.TgGetParseMode());
                    idFile = message.Video!.FileId;
                    break;
                case "gif":
                    message = await BotClient.SendAnimation(chatId, file, caption: send.Message!, replyMarkup: send.GetReplyMarkup(), parseMode: send.TgGetParseMode());
                    idFile = message.Document!.FileId;
                    break;
                case "jpg":
                case "png":
                    message = await BotClient.SendPhoto(chatId, file, caption: send.Message!, replyMarkup: send.GetReplyMarkup(), parseMode: send.TgGetParseMode());
                    idFile = message.Photo![0].FileId;
                    break;
                default:
                    message = await BotClient.SendDocument(chatId, file, caption: send.Message!, replyMarkup: send.GetReplyMarkup(), parseMode: send.TgGetParseMode());
                    idFile = message.Document!.FileId;
                    break;
            }

            return (message, idFile);
        }

        private async Task EditOldMessage(SendModel send, Chat chatId, SendingMessageInfo limitsInfo)
        {
            int message;
            if (limitsInfo.IsMediaOne)
            {
                using var file = await send.Medias![0].GetFile();
                var inputMedia = (file.GetAlbumInputMedia() as InputMedia)!;
                inputMedia.Caption = send.Message;
                inputMedia.ParseMode = send.TgGetParseMode();
                message = (await BotClient.EditMessageMedia(chatId, limitsInfo.LastMessage!.MessagesMediaGroup![0], inputMedia, replyMarkup: send.Inline.CreateInline())).Id;
                send[TgClient.KeyMessagesToEdit] = new LastMessageInfo()
                {
                    MessagesMediaGroup = [message],
                    IsInline = limitsInfo.IsInline
                };
            }
            else
            {
                message = (await BotClient.EditMessageText(chatId, limitsInfo.LastMessage!.Messages![0], send.Message!, replyMarkup: send.Inline.CreateInline(), parseMode: send.TgGetParseMode())).Id;
                send[TgClient.KeyMessagesToEdit] = new LastMessageInfo()
                {
                    Messages = [message],
                    IsInline = limitsInfo.IsInline
                };
            }
        }
    }
}
