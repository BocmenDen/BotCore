using BotCore.PageRouter.Interfaces;

namespace BotCore.PageRouter.Models
{
    public class StorageModel<T>(T value, Func<T, Task> save)
        where T : new()
    {
        public readonly T Value = value ?? throw new Exception("Модель не может быть пустой");

        public Task Save() => save(Value);
    }
}
