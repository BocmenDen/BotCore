namespace BotCore.PageRouter.Interfaces
{
    public interface IStorageProvider
    {
        public Task Save<T>(T model);
    }
}