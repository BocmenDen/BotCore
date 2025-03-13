using BotCore.PageRouter.Models;

namespace BotCore.PageRouter.Interfaces
{
    public interface IBindStorageModel<T>
        where T : new()
    {
        public void BindStorageModel(StorageModel<T> model);
    }
}