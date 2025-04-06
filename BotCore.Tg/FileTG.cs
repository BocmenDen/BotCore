using BotCore.Models;
using Telegram.Bot.Types;

namespace BotCore.Tg
{
    public static partial class TgClientExtensions
    {
        internal class FileTG(MediaSource media) : IDisposable
        {
            public InputFile File = null!;
            private Action? _disponse;

            public static ValueTask<FileTG> GetFileTg(MediaSource media)
            {
                if (media.TryGetParameter(TgClient.KeyMediaSourceFileId, out string? id))
                    return ValueTask.FromResult(new FileTG(media) { File = InputFile.FromFileId(id!) });

                if (media.Uri is not null)
                    return ValueTask.FromResult(new FileTG(media) { File = InputFile.FromUri(media.Uri) });

                return new ValueTask<FileTG>(media.GetStream().ContinueWith((ts) =>
                {
                    var stream = ts.Result;
                    return new FileTG(media) { File = InputFile.FromStream(stream, media.Name), _disponse = stream.Dispose };
                }));
            }

            public void Dispose()
            {
                _disponse?.Invoke();
                GC.SuppressFinalize(this);
            }

            public static implicit operator InputFile(FileTG fileTG) => fileTG.File;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Преобразовать оператор switch в выражение", Justification = "Теряем комментарий")]
            public IAlbumInputMedia GetAlbumInputMedia()
            {
                switch (media.Type)
                {
                    case "mp4":
                    case "gif": // https://github.com/TelegramBots/Telegram.Bot/issues/1319
                        return new InputMediaVideo(File);
                    case "jpg":
                    case "png":
                        return new InputMediaPhoto(File);
                    default:
                        return new InputMediaDocument(File);
                }
            }
        }
    }
}
