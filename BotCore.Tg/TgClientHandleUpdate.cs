using BotCore.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BotCore.Tg
{
    public partial class TgClient<TUser, TDB>
    {
        public event Func<UpdateContext<TUser>, Task>? Update;

        private readonly ConcurrentDictionary<string, List<Update>> _mediaGroupCache = [];
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _mediaGroupTimeouts = [];
        private readonly TimeSpan MediaGroupDelay = TimeSpan.FromSeconds(1.5);

        private partial Task SendMessage(SendModel sendModel, Chat chatId);

        private partial async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (Update is null) return;
            if (update.Message?.MediaGroupId is not null)
            {
                HandleMediaGroup(update);
                return;
            }
            UpdateType flags = update.Type switch
            {
                Telegram.Bot.Types.Enums.UpdateType.CallbackQuery => UpdateType.Inline,
                _ => UpdateType.None
            };
            var entity = await GetOrCreateUser(update);
            if (entity == null) return;
            var (user, chatId) = entity.Value;
            var textMessage = update.Message?.Text ?? update.Message?.Caption;
            (var command, textMessage) = textMessage.ParseCommand();
            if (!string.IsNullOrWhiteSpace(command)) flags |= UpdateType.Command;
            if (!string.IsNullOrWhiteSpace(textMessage)) flags |= UpdateType.Message;
            var media = ParseMedia(update); if (media is not null) flags |= UpdateType.Media;

            var updateModel = new UpdateModel()
            {
                UpdateType = flags,
                Message = textMessage,
                Medias = media,
                Command = command,
                OriginalMessage = update
            };

            await Update(new UpdateContext<TUser>(this, user, updateModel, (sendModel) => SendMessage(sendModel, chatId)));
        }

        private void HandleMediaGroup(Update update)
        {
            var groupId = update.Message!.MediaGroupId!;
            if (_mediaGroupTimeouts.TryRemove(groupId, out var token))
            {
                token.Cancel();
            }
            List<Update> updateList;
            if (!_mediaGroupCache.TryGetValue(groupId, out updateList!))
            {
                updateList = [];
                _mediaGroupCache[groupId] = updateList;
            }
            updateList.Add(update);
            token = new CancellationTokenSource();
            _mediaGroupTimeouts[groupId] = token;
            _ = Task.Factory.StartNew(async () =>
            {
                var delayTask = Task.Delay(MediaGroupDelay);
                var cancelTask = Task.Delay(Timeout.Infinite, token.Token);
                var completed = await Task.WhenAny(delayTask, cancelTask);
                if (completed == cancelTask) return;
                if (_mediaGroupCache.TryRemove(groupId, out var updates))
                    await CombineMediaGroups(updates);
            });
        }

        private async Task CombineMediaGroups(List<Update> updates)
        {
            if (Update == null) return;
            var updateLast = updates.Last();
            var entity = await GetOrCreateUser(updateLast);
            if (entity == null) return;
            var (user, chatId) = entity.Value;
            List<MediaSource> medias = [];
            foreach (var item in updates)
            {
                var media = ParseMedia(item);
                if(media is not null)
                    medias.AddRange(media);
            }

            var updateModel = new UpdateModel()
            {
                UpdateType = UpdateType.Media,
                Medias = medias,
                OriginalMessage = updateLast
            };

            await Update(new UpdateContext<TUser>(this, user, updateModel, (sendModel) => SendMessage(sendModel, chatId)));
        }

        private async Task<(TUser, Chat)?> GetOrCreateUser(Update update)
        {
            var chatId = update.GetChatId(); if (chatId is null) return null;
            var (user, isNewUser) = await _database.GetOrCreate(chatId); if (isNewUser) _logger?.LogInformation("Добавлен новый пользователь [{userTg}]", user);
            return (user, chatId);
        }

        /// <summary>
        /// TODO извлечение более одного медиа
        /// </summary>
        internal List<MediaSource>? ParseMedia(Update update)
        {
            List<MediaSource> mediaSources = [];
            void addMedia(List<MediaSource> medias, string fileId, string? fileName, string? mimeType)
            {
                if (fileId == null) return;
                medias.Add(new MediaSource(async () =>
                {
                    string path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + fileName);
                    var streamWriter = File.Open(path, FileMode.OpenOrCreate);
                    var file = await BotClient.GetFile(fileId);
                    await BotClient.DownloadFile(file.FilePath!, streamWriter);
                    streamWriter.Position = 0;
                    return streamWriter;
                }, new() { { TgClient.KeyMediaSourceFileId, fileId } })
                {
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    Type = Path.GetExtension(fileName)?.Replace(".", string.Empty),
                    MimeType = mimeType,
                    Id = fileId
                });
            }
            if (update.Message?.Document != null)
                addMedia(mediaSources, update.Message.Document.FileId, update.Message.Document.FileName, update.Message.Document.MimeType);
            else if (update.Message?.Animation != null)
                addMedia(mediaSources, update.Message.Animation.FileId, update.Message.Animation.FileName, update.Message.Animation.MimeType);
            else if (update.Message?.Video != null)
                addMedia(mediaSources, update.Message.Video.FileId, update.Message.Video.FileName, update.Message.Video.MimeType);
            else if (update.Message?.Photo != null)
                addMedia(mediaSources, update.Message.Photo.Last().FileId, $"{update.Message.Photo.Last().FileId}.jpg", "image/jpeg");
            else if (update.Message?.Audio != null)
                addMedia(mediaSources, update.Message.Audio.FileId, update.Message.Audio.FileName, update.Message.Audio.MimeType);
            else if (update.Message?.Voice != null)
                addMedia(mediaSources, update.Message.Voice.FileId, $"{update.Message.Voice}.mp3", update.Message.Voice.MimeType);
            else if (update.Message?.VideoNote != null)
                addMedia(mediaSources, update.Message.VideoNote.FileId, $"{update.Message.VideoNote}.mp4", "video/mp4");
            else if (update.Message?.MediaGroupId != null)
            {
                // TODO сложная ломающая всё логика ТГ отправляет данные группы в виде разных сообщений
            }
            if (mediaSources.Count!=0)
                return mediaSources;
            return null;
        }
    }
}
