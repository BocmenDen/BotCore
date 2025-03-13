using BotCore.PageRouter.Interfaces;

namespace BotCore.PageRouter.Models
{
    public class StorageModel<T>(T value, IStorageProvider storageProvider)
        where T : new()
    {
        public readonly T? Value = value;

        public Task Save() => storageProvider.Save();
    }
}
