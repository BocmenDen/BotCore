using BotCore.Models;
using System.Collections;
using Telegram.Bot.Types;

namespace BotCore.Tg
{
    public static partial class TgClientExtensions
    {
        internal class FilesTG : IDisposable, IEnumerable<IAlbumInputMedia>
        {
            public readonly IReadOnlyList<FileTG> Files;

            private FilesTG(List<FileTG> files) => Files = files;

            public static async Task<FilesTG> GetFilesTg(IEnumerable<MediaSource> medias)
            {
                List<FileTG> files = [];
                foreach (var media in medias)
                {
                    var file = await media.GetFile();
                    files.Add(file);
                }
                return new FilesTG(files);
            }

            public void Dispose()
            {
                foreach (var file in Files)
                    file.Dispose();
                GC.SuppressFinalize(this);
            }

            public IEnumerator<IAlbumInputMedia> GetEnumerator() => Files.Select(x => x.GetAlbumInputMedia()).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Files.Select(x => x.GetAlbumInputMedia()).GetEnumerator();
        }
    }
}
